using LMSFinal.Application.Common;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Quizzes;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Services
{


    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly IQuizAttemptRepository _quizAttemptRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICertificateService _certificateService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public QuizService(
            IQuizRepository quizRepository,
            ILessonRepository lessonRepository,
            IQuizAttemptRepository quizAttemptRepository,
            IEnrollmentRepository enrollmentRepository,
            ICertificateService certificateService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _quizRepository = quizRepository;
            _lessonRepository = lessonRepository;
            _quizAttemptRepository = quizAttemptRepository;
            _enrollmentRepository = enrollmentRepository;
            _certificateService = certificateService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task<QuizDto> GetByIdAsync(Guid currentUserId, Guid quizId, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetWithDetailsAsync(quizId, cancellationToken)
                ?? throw new NotFoundException("Quiz", quizId);

            var isOwner = quiz.Lesson.Module.Course.InstructorId == currentUserId;

            // Phase 15 (Security review): раньше этой проверки не было — любой аутентифицированный
            // пользователь (даже не записанный на курс) мог получить текст вопросов и вариантов
            // ответа по одному только QuizId. IsCorrect и так был скрыт (см. MapQuiz), но сам
            // контент квиза утекал в обход Enrollment — то же правило, что уже действует для
            // Lesson/Module (Phase 9), просто забыли применить сюда при рефакторинге Phase 8.
            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, quiz.Lesson.Module.CourseId, cancellationToken);
                if (!isEnrolled)
                {
                    throw new ForbiddenAccessException("Чтобы открыть квиз, запишитесь на курс.");
                }
            }

            return MapQuiz(quiz, revealCorrectAnswers: isOwner);
        }

        public async Task<QuizLocalizedDto> GetByIdLocalizedAsync(
            Guid currentUserId, Guid quizId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetWithDetailsAsync(quizId, cancellationToken)
                ?? throw new NotFoundException("Quiz", quizId);

            var isOwner = quiz.Lesson.Module.Course.InstructorId == currentUserId;

            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, quiz.Lesson.Module.CourseId, cancellationToken);
                if (!isEnrolled)
                {
                    throw new ForbiddenAccessException("Чтобы открыть квиз, запишитесь на курс.");
                }
            }

            return MapQuizLocalized(quiz, language, revealCorrectAnswers: isOwner);
        }

        public async Task<QuizDto> CreateAsync(Guid instructorId, CreateQuizRequest request, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(request.LessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", request.LessonId);

            EnsureCourseOwnership(lesson.Module.Course, instructorId);

            var existingQuiz = await _quizRepository.GetByLessonIdAsync(request.LessonId, cancellationToken);
            if (existingQuiz is not null)
            {
                throw new ConflictException("У этого урока уже есть квиз — отредактируйте существующий вместо создания нового.");
            }

            var quiz = new Quiz
            {
                LessonId = request.LessonId,
                PassingScore = request.PassingScore,
                TimeLimitMinutes = request.TimeLimitMinutes
            };

            ApplyQuizTranslations(quiz, request.Translations);
            ApplyQuestions(quiz, request.Questions);

            await _quizRepository.AddAsync(quiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapQuiz(quiz, revealCorrectAnswers: true);
        }

        public async Task<QuizDto> UpdateAsync(Guid instructorId, Guid quizId, UpdateQuizRequest request, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetWithDetailsAsync(quizId, cancellationToken)
                ?? throw new NotFoundException("Quiz", quizId);

            EnsureCourseOwnership(quiz.Lesson.Module.Course, instructorId);

            if (await _quizAttemptRepository.HasAttemptsAsync(quizId, cancellationToken))
            {
                throw new ConflictException(
                    "Нельзя менять вопросы квиза, который уже проходили студенты. Создайте новый квиз вместо этого.");
            }

            quiz.PassingScore = request.PassingScore;
            quiz.TimeLimitMinutes = request.TimeLimitMinutes;
            quiz.UpdatedAt = DateTime.UtcNow;

            quiz.Translations.Clear();
            ApplyQuizTranslations(quiz, request.Translations);

            quiz.Questions.Clear();
            ApplyQuestions(quiz, request.Questions);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapQuiz(quiz, revealCorrectAnswers: true);
        }

        public async Task DeleteAsync(Guid instructorId, Guid quizId, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetWithDetailsAsync(quizId, cancellationToken)
                ?? throw new NotFoundException("Quiz", quizId);

            EnsureCourseOwnership(quiz.Lesson.Module.Course, instructorId);

            if (await _quizAttemptRepository.HasAttemptsAsync(quizId, cancellationToken))
            {
                throw new ConflictException("Нельзя удалить квиз, который уже проходили студенты.");
            }

            _quizRepository.Remove(quiz);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<QuizResultDto> SubmitAsync(Guid studentId, Guid quizId, SubmitQuizRequest request, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetWithDetailsAsync(quizId, cancellationToken)
                ?? throw new NotFoundException("Quiz", quizId);

            //  закрываем TODO из Phase 8 — теперь реально проверяем Enrollment, а не только роль.
            var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(studentId, quiz.Lesson.Module.CourseId, cancellationToken);
            if (!isEnrolled)
            {
                throw new ForbiddenAccessException("Чтобы проходить квиз, нужно быть записанным на курс.");
            }

            var attempt = new QuizAttempt
            {
                StudentId = studentId,
                QuizId = quizId,
                StartedAt = DateTime.UtcNow
            };

            var totalPoints = quiz.Questions.Sum(q => q.Points);
            var earnedPoints = 0;
            var correctQuestionsCount = 0;

            foreach (var question in quiz.Questions)
            {
                var submitted = request.Answers.FirstOrDefault(a => a.QuestionId == question.Id);


                var validAnswerIds = question.Answers.Select(a => a.Id).ToHashSet();
                var selectedIds = (submitted?.SelectedAnswerIds ?? Array.Empty<Guid>())
                    .Where(id => validAnswerIds.Contains(id))
                    .ToHashSet();

                var correctIds = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToHashSet();

                var isCorrect = selectedIds.SetEquals(correctIds);

                if (isCorrect)
                {
                    earnedPoints += question.Points;
                    correctQuestionsCount++;
                }

                var attemptAnswer = new QuizAttemptAnswer
                {
                    QuizAttemptId = attempt.Id,
                    QuestionId = question.Id
                };

                foreach (var answerId in selectedIds)
                {
                    attemptAnswer.SelectedAnswers.Add(new QuizAttemptAnswerSelection
                    {
                        QuizAttemptAnswerId = attemptAnswer.Id,
                        AnswerId = answerId
                    });
                }

                attempt.AttemptAnswers.Add(attemptAnswer);
            }

            var percentage = totalPoints == 0 ? 0 : Math.Round(earnedPoints * 100.0 / totalPoints, 2);
            var passed = percentage >= quiz.PassingScore;

            attempt.Score = earnedPoints;
            attempt.Percentage = percentage;
            attempt.Passed = passed;
            attempt.CompletedAt = DateTime.UtcNow;

            await _quizAttemptRepository.AddAsync(attempt, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var quizTitle = TranslationResolver.Resolve(quiz.Translations, LanguageCode.EN, t => t.LanguageCode)?.Title
                ?? quiz.Id.ToString();

     
            await _notificationService.NotifyAsync(
                studentId,
                "Quiz result available",
                $"You scored {percentage}% on \"{quizTitle}\" — {(passed ? "Passed" : "Not passed")}.",
                NotificationType.QuizResultAvailable,
                cancellationToken);


            await _certificateService.IssueIfEligibleAsync(studentId, quiz.Lesson.Module.CourseId, cancellationToken);

            return new QuizResultDto(
                attempt.Id,
                StudentName: null,
                attempt.Score,
                attempt.Percentage,
                attempt.Passed,
                TotalQuestions: quiz.Questions.Count,
                CorrectAnswers: correctQuestionsCount,
                attempt.CompletedAt);
        }

        public async Task<IReadOnlyList<QuizResultDto>> GetResultsAsync(Guid currentUserId, Guid quizId, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetWithDetailsAsync(quizId, cancellationToken)
                ?? throw new NotFoundException("Quiz", quizId);

            var isOwner = quiz.Lesson.Module.Course.InstructorId == currentUserId;

            if (isOwner)
            {
                var allAttempts = await _quizAttemptRepository.GetByQuizIdAsync(quizId, cancellationToken);
                return allAttempts.Select(a => new QuizResultDto(
                    a.Id,
                    $"{a.Student.FirstName} {a.Student.LastName}",
                    a.Score, a.Percentage, a.Passed,
                    TotalQuestions: null, CorrectAnswers: null, a.CompletedAt)).ToList();
            }

            // Не владелец курса — видит только свои попытки по этому квизу.
            var myAttempts = await _quizAttemptRepository.GetByStudentIdAsync(currentUserId, cancellationToken);
            return myAttempts.Where(a => a.QuizId == quizId).Select(a => new QuizResultDto(
                a.Id, StudentName: null,
                a.Score, a.Percentage, a.Passed,
                TotalQuestions: null, CorrectAnswers: null, a.CompletedAt)).ToList();
        }

        // --- helpers ---

        private static void EnsureCourseOwnership(Course course, Guid instructorId)
        {
            if (course.InstructorId != instructorId)
            {
                throw new ForbiddenAccessException("Вы можете управлять только квизами своих курсов.");
            }
        }

        private static void ApplyQuizTranslations(Quiz quiz, IReadOnlyList<QuizTranslationInput> translations)
        {
            foreach (var input in translations)
            {
                quiz.Translations.Add(new QuizTranslation
                {
                    
                    Id = Guid.Empty,
                    LanguageCode = input.LanguageCode,
                    Title = input.Title,
                    Description = input.Description
                });
            }
        }

        private static void ApplyQuestions(Quiz quiz, IReadOnlyList<QuestionInput> questions)
        {
            var orderIndex = 0;

            foreach (var questionInput in questions)
            {
                var question = new Question
                {
                    Id = Guid.Empty,
                    QuizId = quiz.Id,
                    QuestionType = questionInput.QuestionType,
                    Points = questionInput.Points,
                    OrderIndex = orderIndex++
                };

                foreach (var translationInput in questionInput.Translations)
                {
                    question.Translations.Add(new QuestionTranslation
                    {
                        Id = Guid.Empty,
                        LanguageCode = translationInput.LanguageCode,
                        QuestionText = translationInput.QuestionText
                    });
                }

                foreach (var answerInput in questionInput.Answers)
                {
                    var answer = new Answer
                    {
                        Id = Guid.Empty,
                        IsCorrect = answerInput.IsCorrect
                    };

                    foreach (var translationInput in answerInput.Translations)
                    {
                        answer.Translations.Add(new AnswerTranslation
                        {
                            Id = Guid.Empty,
                            LanguageCode = translationInput.LanguageCode,
                            AnswerText = translationInput.AnswerText
                        });
                    }

                    question.Answers.Add(answer);
                }

                quiz.Questions.Add(question);
            }
        }

    
        private static QuizDto MapQuiz(Quiz quiz, bool revealCorrectAnswers)
        {
            var questions = quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new QuestionDto(
                    q.Id,
                    q.QuestionType.ToString(),
                    q.OrderIndex,
                    q.Points,
                    q.Translations.Select(t => new QuestionTranslationDto(t.LanguageCode.ToString(), t.QuestionText)).ToList(),
                    q.Answers.Select(a => new AnswerDto(
                        a.Id,
                        revealCorrectAnswers ? a.IsCorrect : null,
                        a.Translations.Select(t => new AnswerTranslationDto(t.LanguageCode.ToString(), t.AnswerText)).ToList()))
                        .ToList()))
                .ToList();

            var quizTranslations = quiz.Translations
                .Select(t => new QuizTranslationDto(t.LanguageCode.ToString(), t.Title, t.Description))
                .ToList();

            return new QuizDto(quiz.Id, quiz.LessonId, quiz.PassingScore, quiz.TimeLimitMinutes, quizTranslations, questions);
        }


        private static QuizLocalizedDto MapQuizLocalized(Quiz quiz, LanguageCode language, bool revealCorrectAnswers)
        {
            var quizTranslation = TranslationResolver.Resolve(quiz.Translations, language, t => t.LanguageCode)
                ?? throw new ConflictException($"У квиза '{quiz.Id}' нет ни одного перевода.");

            var questions = quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q =>
                {
                    var questionTranslation = TranslationResolver.Resolve(q.Translations, language, t => t.LanguageCode)
                        ?? throw new ConflictException($"У вопроса '{q.Id}' нет ни одного перевода.");

                    var answers = q.Answers.Select(a =>
                    {
                        var answerTranslation = TranslationResolver.Resolve(a.Translations, language, t => t.LanguageCode)
                            ?? throw new ConflictException($"У варианта ответа '{a.Id}' нет ни одного перевода.");

                        return new AnswerLocalizedDto(a.Id, answerTranslation.AnswerText, revealCorrectAnswers ? a.IsCorrect : null);
                    }).ToList();

                    return new QuestionLocalizedDto(
                        q.Id, questionTranslation.QuestionText, q.QuestionType.ToString(), q.OrderIndex, q.Points, answers);
                })
                .ToList();

            return new QuizLocalizedDto(
                quiz.Id, quiz.LessonId, quizTranslation.Title, quizTranslation.Description,
                quiz.PassingScore, quiz.TimeLimitMinutes, questions);
        }
    }

}
