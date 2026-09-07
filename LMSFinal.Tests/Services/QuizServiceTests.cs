using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Application.Services;
using LMSFinal.Contracts.DTOs.Quizzes;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using LMSFinal.Tests.Common;
using Moq;

namespace LMSFinal.Tests.Services
{

    public class QuizServiceTests
    {
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<ILessonRepository> _lessons = new();
        private readonly Mock<IQuizAttemptRepository> _attempts = new();
        private readonly Mock<IEnrollmentRepository> _enrollments = new();
        private readonly Mock<ICertificateService> _certificates = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private readonly Guid _studentId = Guid.NewGuid();
        private readonly Guid _instructorId = Guid.NewGuid();

        private QuizService CreateSut() => new(
            _quizzes.Object, _lessons.Object, _attempts.Object, _enrollments.Object,
            _certificates.Object, _notifications.Object, _unitOfWork.Object);
        private Quiz GivenQuiz(int questionCount = 4, int passingScore = 60, bool studentEnrolled = true)
        {
            var course = TestData.CourseWithLessons(_instructorId, 1, out var lessons);
            var quiz = TestData.Quiz(lessons[0], questionCount, passingScore);

            _quizzes.Setup(r => r.GetWithDetailsAsync(quiz.Id, It.IsAny<CancellationToken>())).ReturnsAsync(quiz);
            _enrollments
                .Setup(r => r.IsEnrolledAsync(_studentId, course.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(studentEnrolled);

            return quiz;
        }

        private static SubmitQuizRequest AnswerFirst(Quiz quiz, int correctCount)
        {
            var answers = quiz.Questions
                .OrderBy(question => question.OrderIndex)
                .Select((question, index) => new SubmitQuizAnswerInput(
                    question.Id,
                    index < correctCount
                        ? question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList()
                        : question.Answers.Where(a => !a.IsCorrect).Take(1).Select(a => a.Id).ToList()))
                .ToList();

            return new SubmitQuizRequest { Answers = answers };
        }

        // ------------------------------------------------------------------
        // Подсчёт баллов
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(4, 4, 4, 100.0, true)]
        [InlineData(4, 3, 3, 75.0, true)]
        [InlineData(4, 2, 2, 50.0, false)]
        [InlineData(4, 0, 0, 0.0, false)]
        [InlineData(5, 3, 3, 60.0, true)]   
        [InlineData(3, 2, 2, 66.67, true)]  
        public async Task SubmitAsync_ScoresAnswersOnTheServer(
            int questionCount, int correctCount, int expectedScore, double expectedPercentage, bool expectedPassed)
        {
            var quiz = GivenQuiz(questionCount);

            var result = await CreateSut().SubmitAsync(_studentId, quiz.Id, AnswerFirst(quiz, correctCount));

            Assert.Equal(expectedScore, result.Score);
            Assert.Equal(expectedPercentage, result.Percentage);
            Assert.Equal(expectedPassed, result.Passed);
            Assert.Equal(questionCount, result.TotalQuestions);
            Assert.Equal(correctCount, result.CorrectAnswers);
        }

        [Fact]
        public async Task SubmitAsync_ForeignAnswerIdsAlone_EarnNoPoints()
        {
            var quiz = GivenQuiz(2);
            var questions = quiz.Questions.OrderBy(q => q.OrderIndex).ToList();

            var request = new SubmitQuizRequest
            {
                Answers = new[]
                {
                    new SubmitQuizAnswerInput(questions[0].Id, new[] { questions[1].Answers.First(a => a.IsCorrect).Id }),
                    new SubmitQuizAnswerInput(questions[1].Id, new[] { Guid.NewGuid() })
                }
            };

            var result = await CreateSut().SubmitAsync(_studentId, quiz.Id, request);

            Assert.Equal(0, result.Score);
            Assert.False(result.Passed);
        }

        [Fact]
        public async Task SubmitAsync_JunkIdsSentAlongsideACorrectAnswer_AreStrippedAndTheQuestionStillCounts()
        {
            var quiz = GivenQuiz(2);
            var questions = quiz.Questions.OrderBy(q => q.OrderIndex).ToList();

            var withJunk = new List<Guid>
            {
                questions[0].Answers.First(a => a.IsCorrect).Id,          
                questions[1].Answers.First(a => !a.IsCorrect).Id,        
                Guid.NewGuid()                                            
            };

            var request = new SubmitQuizRequest
            {
                Answers = new[]
                {
                    new SubmitQuizAnswerInput(questions[0].Id, withJunk),
                    new SubmitQuizAnswerInput(
                        questions[1].Id,
                        questions[1].Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList())
                }
            };

            var result = await CreateSut().SubmitAsync(_studentId, quiz.Id, request);

            Assert.Equal(2, result.Score);
            Assert.Equal(100.0, result.Percentage);
        }

        [Fact]
        public async Task SubmitAsync_MultipleChoice_RequiresTheExactSetOfCorrectAnswers()
        {
            var quiz = GivenQuiz(0);
            var question = TestData.MultipleChoiceQuestion(quiz, points: 2);
            var correct = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList();

            var sut = CreateSut();

            var exact = await sut.SubmitAsync(_studentId, quiz.Id,
                new SubmitQuizRequest { Answers = new[] { new SubmitQuizAnswerInput(question.Id, correct) } });

            var partial = await sut.SubmitAsync(_studentId, quiz.Id,
                new SubmitQuizRequest { Answers = new[] { new SubmitQuizAnswerInput(question.Id, correct.Take(1).ToList()) } });

            var withExtra = await sut.SubmitAsync(_studentId, quiz.Id,
                new SubmitQuizRequest
                {
                    Answers = new[]
                    {
                        new SubmitQuizAnswerInput(
                            question.Id,
                            correct.Append(question.Answers.First(a => !a.IsCorrect).Id).ToList())
                    }
                });

            Assert.Equal(2, exact.Score);
            Assert.Equal(0, partial.Score);   
            Assert.Equal(0, withExtra.Score);
        }

        [Fact]
        public async Task SubmitAsync_UsesQuestionPointsRatherThanQuestionCount()
        {
            var quiz = GivenQuiz(2, passingScore: 60);
            var heavy = TestData.MultipleChoiceQuestion(quiz, points: 2);
            var easy = quiz.Questions.First();

            var request = new SubmitQuizRequest
            {
                Answers = new[]
                {
                    new SubmitQuizAnswerInput(easy.Id, easy.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList()),
                    new SubmitQuizAnswerInput(heavy.Id, heavy.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToList())
                }
            };

            var result = await CreateSut().SubmitAsync(_studentId, quiz.Id, request);

            Assert.Equal(3, result.Score);
            Assert.Equal(75.0, result.Percentage);
        }

        [Fact]
        public async Task SubmitAsync_StoresEveryQuestionInTheAttemptEvenWhenUnanswered()
        {
            var quiz = GivenQuiz(3);

            QuizAttempt? saved = null;
            _attempts
                .Setup(r => r.AddAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()))
                .Callback<QuizAttempt, CancellationToken>((attempt, _) => saved = attempt)
                .Returns(Task.CompletedTask);

            await CreateSut().SubmitAsync(_studentId, quiz.Id, new SubmitQuizRequest { Answers = Array.Empty<SubmitQuizAnswerInput>() });

            Assert.NotNull(saved);
            Assert.Equal(3, saved!.AttemptAnswers.Count);
            Assert.All(saved.AttemptAnswers, answer => Assert.Empty(answer.SelectedAnswers));
            Assert.Equal(0, saved.Score);
        }

        [Fact]
        public async Task SubmitAsync_PersistsScoreConsistentWithTheStoredSelections()
        {
            var quiz = GivenQuiz(4);

            QuizAttempt? saved = null;
            _attempts
                .Setup(r => r.AddAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()))
                .Callback<QuizAttempt, CancellationToken>((attempt, _) => saved = attempt)
                .Returns(Task.CompletedTask);

            await CreateSut().SubmitAsync(_studentId, quiz.Id, AnswerFirst(quiz, 3));

            Assert.NotNull(saved);

            var correctAnswerIds = quiz.Questions
                .SelectMany(q => q.Answers.Where(a => a.IsCorrect))
                .Select(a => a.Id)
                .ToHashSet();

            var recomputed = saved!.AttemptAnswers.Count(answer =>
                answer.SelectedAnswers.Count > 0 &&
                answer.SelectedAnswers.All(selection => correctAnswerIds.Contains(selection.AnswerId)));

            Assert.Equal(recomputed, saved.Score);
        }

        // ------------------------------------------------------------------
        // Доступ
        // ------------------------------------------------------------------

        [Fact]
        public async Task SubmitAsync_StudentIsNotEnrolled_ThrowsForbidden()
        {
            var quiz = GivenQuiz(studentEnrolled: false);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().SubmitAsync(_studentId, quiz.Id, AnswerFirst(quiz, 4)));

            _attempts.Verify(r => r.AddAsync(It.IsAny<QuizAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_Student_DoesNotSeeWhichAnswersAreCorrect()
        {
            var quiz = GivenQuiz(3);

            var result = await CreateSut().GetByIdAsync(_studentId, quiz.Id);

            Assert.All(result.Questions.SelectMany(q => q.Answers), answer => Assert.Null(answer.IsCorrect));
        }

        [Fact]
        public async Task GetByIdAsync_OwningInstructor_SeesCorrectAnswerFlags()
        {
            var quiz = GivenQuiz(3);

            var result = await CreateSut().GetByIdAsync(_instructorId, quiz.Id);

            Assert.All(result.Questions.SelectMany(q => q.Answers), answer => Assert.NotNull(answer.IsCorrect));
            Assert.Contains(result.Questions.SelectMany(q => q.Answers), answer => answer.IsCorrect == true);
        }

        [Fact]
        public async Task GetByIdAsync_StrangerWhoIsNotEnrolled_ThrowsForbidden()
        {
            var quiz = GivenQuiz(3, studentEnrolled: false);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => CreateSut().GetByIdAsync(_studentId, quiz.Id));
        }

        [Fact]
        public async Task SubmitAsync_TriggersCertificateCheck()
        {
            var quiz = GivenQuiz(2);
            var courseId = quiz.Lesson.Module.CourseId;

            await CreateSut().SubmitAsync(_studentId, quiz.Id, AnswerFirst(quiz, 2));

            _certificates.Verify(c => c.IssueIfEligibleAsync(_studentId, courseId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
