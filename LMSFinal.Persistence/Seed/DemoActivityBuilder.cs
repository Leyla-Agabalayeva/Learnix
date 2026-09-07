using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;

namespace LMSFinal.Persistence.Seed
{
    /// <summary>Всё, что сгенерировал <see cref="DemoActivityBuilder"/>, одним пакетом.</summary>
    public sealed class DemoActivity
    {
        public List<Enrollment> Enrollments { get; } = new();
        public List<LessonProgress> LessonProgresses { get; } = new();
        public List<QuizAttempt> QuizAttempts { get; } = new();
        public List<CourseReview> Reviews { get; } = new();
        public List<Certificate> Certificates { get; } = new();
        public List<Wishlist> WishlistItems { get; } = new();
        public List<Notification> Notifications { get; } = new();
    }

    public sealed class DemoActivityBuilder
    {
        private const int RandomSeed = 20260826;

        private readonly Random _rng = new(RandomSeed);
        private readonly DateTime _now = DateTime.UtcNow;

        private static readonly int[] PrimaryProfileByCourse = { 0, 3, 1, 2, 5 };

        public DemoActivity Build(
            IReadOnlyList<Course> courses,
            IReadOnlyList<UserSpec> studentSpecs,
            IReadOnlyDictionary<string, ApplicationUser> studentsByEmail,
            int existingCertificateCount)
        {
            var activity = new DemoActivity();

            var students = studentSpecs
                .Where(spec => studentsByEmail.ContainsKey(spec.Email))
                .OrderByDescending(spec => spec.IsPrimaryDemo)
                .ThenBy(spec => spec.Email, StringComparer.Ordinal)
                .Select(spec => studentsByEmail[spec.Email])
                .ToList();

            if (students.Count == 0)
            {
                return activity;
            }

            var primaryStudent = studentSpecs.Any(spec => spec.IsPrimaryDemo) ? students[0] : null;
            var certificateSequence = existingCertificateCount;

            var openCourses = courses.Where(course => course.Status != CourseStatus.Draft).ToList();

            for (var courseIndex = 0; courseIndex < openCourses.Count; courseIndex++)
            {
                SeedCourseActivity(openCourses[courseIndex], courseIndex, students, primaryStudent, activity, ref certificateSequence);
            }

            SeedWishlist(courses, students, activity);

            return activity;
        }

        private void SeedCourseActivity(
            Course course,
            int courseIndex,
            IReadOnlyList<ApplicationUser> students,
            ApplicationUser? primaryStudent,
            DemoActivity activity,
            ref int certificateSequence)
        {
            var lessons = course.Modules
                .OrderBy(module => module.OrderIndex)
                .SelectMany(module => module.Lessons.OrderBy(lesson => lesson.OrderIndex))
                .Where(lesson => lesson.IsPublished)
                .ToList();

            if (lessons.Count == 0)
            {
                return;
            }

    
            var minimumEnrolled = Math.Min(3, students.Count);
            var enrolledCount = _rng.Next(minimumEnrolled, students.Count + 1);

            List<ApplicationUser> enrolledStudents;

            if (primaryStudent is not null && courseIndex < PrimaryProfileByCourse.Length)
            {

                var position = PrimaryProfileByCourse[courseIndex];
                enrolledCount = Math.Clamp(Math.Max(enrolledCount, position + 1), 1, students.Count);
                position = Math.Min(position, enrolledCount - 1);

                enrolledStudents = students
                    .Where(student => !ReferenceEquals(student, primaryStudent))
                    .OrderBy(_ => _rng.Next())
                    .Take(enrolledCount - 1)
                    .ToList();

                enrolledStudents.Insert(position, primaryStudent);
            }
            else
            {
                enrolledStudents = students
                    .OrderBy(_ => _rng.Next())
                    .Take(enrolledCount)
                    .ToList();
            }

            for (var index = 0; index < enrolledStudents.Count; index++)
            {
                var student = enrolledStudents[index];
                var completedLessons = CompletedLessonCount(index, lessons.Count);
                var enrolledAt = _now.AddDays(-_rng.Next(25, 90));

                var enrollment = BuildEnrollment(course, student, lessons.Count, completedLessons, enrolledAt);
                activity.Enrollments.Add(enrollment);

                var lastActivityAt = enrolledAt;

                for (var lessonIndex = 0; lessonIndex < lessons.Count; lessonIndex++)
                {
                    var lesson = lessons[lessonIndex];
                    var isCompleted = lessonIndex < completedLessons;
                    var isCurrent = lessonIndex == completedLessons;

                    if (!isCompleted && !isCurrent)
                    {
                        continue; 
                    }

                    lastActivityAt = lastActivityAt.AddHours(_rng.Next(6, 40));
                    if (lastActivityAt > _now)
                    {
                        lastActivityAt = _now.AddMinutes(-_rng.Next(10, 600));
                    }

                    activity.LessonProgresses.Add(new LessonProgress
                    {
                        StudentId = student.Id,
                        LessonId = lesson.Id,
                        IsCompleted = isCompleted,
                        CompletedAt = isCompleted ? lastActivityAt : null,
                        LastPosition = isCompleted ? 0 : _rng.Next(40, Math.Max(60, lesson.DurationMinutes * 60 - 30)),
                        LastAccessedAt = lastActivityAt,
                        CreatedAt = lastActivityAt
                    });

                    if (isCompleted && lesson.Quiz is not null)
                    {
                        AddQuizAttempts(lesson.Quiz, student, lastActivityAt, activity);
                    }
                }

                if (enrollment.Status == EnrollmentStatus.Completed)
                {
                    var completedAt = enrollment.CompletedAt ?? lastActivityAt;

                    certificateSequence++;
                    activity.Certificates.Add(new Certificate
                    {
                        CertificateNumber = $"LMS-{completedAt.Year}-{certificateSequence:D6}",
                        StudentId = student.Id,
                        CourseId = course.Id,
                        IssuedAt = completedAt,
                        CompletionDate = completedAt,
                        CreatedAt = completedAt
                    });

                    AddNotification(activity, student, "Course completed 🎉",
                        "You've completed all lessons in this course. Check if you're eligible for a certificate!",
                        NotificationType.CourseCompleted, completedAt);

                    AddNotification(activity, student, "Certificate issued",
                        $"Your certificate ({activity.Certificates[^1].CertificateNumber}) is ready to download.",
                        NotificationType.CertificateIssued, completedAt.AddMinutes(1));
                }

                TryAddReview(course, student, completedLessons, lessons.Count, lastActivityAt, activity);
            }
        }
        private int CompletedLessonCount(int studentIndex, int totalLessons)
        {
            var ratio = studentIndex switch
            {
                0 => 1.00,                        
                1 => 0.80,
                2 => 0.50,
                3 => 1.00,                       
                4 => 0.30,
                _ => 0.15                       
            };

            var completed = (int)Math.Round(totalLessons * ratio, MidpointRounding.AwayFromZero);

            return Math.Clamp(completed, 1, totalLessons);
        }

        private Enrollment BuildEnrollment(
            Course course, ApplicationUser student, int totalLessons, int completedLessons, DateTime enrolledAt)
        {
            var isCompleted = completedLessons == totalLessons;
            var percentage = Math.Round(completedLessons * 100.0 / totalLessons, 2);

            var completedAt = isCompleted ? enrolledAt.AddDays(_rng.Next(7, 22)) : (DateTime?)null;

            if (completedAt > _now)
            {
                completedAt = _now.AddDays(-1);
            }

            return new Enrollment
            {
                StudentId = student.Id,
                CourseId = course.Id,
                EnrolledAt = enrolledAt,
                ProgressPercentage = percentage,
                Status = isCompleted ? EnrollmentStatus.Completed : EnrollmentStatus.Active,
                CompletedAt = completedAt,
                CreatedAt = enrolledAt
            };
        }

        // ------------------------------------------------------------------
        // Квизы
        // ------------------------------------------------------------------

        private void AddQuizAttempts(Quiz quiz, ApplicationUser student, DateTime takenAt, DemoActivity activity)
        {
            if (quiz.Questions.Count == 0)
            {
                return;
            }
            if (_rng.NextDouble() < 0.3)
            {
                activity.QuizAttempts.Add(BuildAttempt(quiz, student, takenAt.AddMinutes(-90), shouldPass: false));
            }

            var attempt = BuildAttempt(quiz, student, takenAt, shouldPass: true);
            activity.QuizAttempts.Add(attempt);

            if (_rng.NextDouble() < 0.4)
            {
                var quizTitle = quiz.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.EN)?.Title
                    ?? quiz.Translations.FirstOrDefault()?.Title
                    ?? quiz.Id.ToString();

                AddNotification(activity, student, "Quiz result available",
                    $"You scored {attempt.Percentage}% on \"{quizTitle}\" — {(attempt.Passed ? "Passed" : "Not passed")}.",
                    NotificationType.QuizResultAvailable, attempt.CompletedAt ?? takenAt);
            }
        }

        private QuizAttempt BuildAttempt(Quiz quiz, ApplicationUser student, DateTime takenAt, bool shouldPass)
        {
            var questions = quiz.Questions.OrderBy(question => question.OrderIndex).ToList();
            var totalPoints = questions.Sum(question => question.Points);
            var shuffled = questions.OrderBy(_ => _rng.Next()).ToList();

            var cumulativePoints = new int[shuffled.Count + 1];
            for (var i = 0; i < shuffled.Count; i++)
            {
                cumulativePoints[i + 1] = cumulativePoints[i] + shuffled[i].Points;
            }

            var correctCount = ChooseCorrectCount(cumulativePoints, totalPoints, quiz.PassingScore, shouldPass);
            var correctlyAnswered = shuffled.Take(correctCount).Select(question => question.Id).ToHashSet();

            var startedAt = takenAt.AddMinutes(-_rng.Next(4, 15));
            var attempt = new QuizAttempt
            {
                StudentId = student.Id,
                QuizId = quiz.Id,
                StartedAt = startedAt,
                CompletedAt = takenAt,
                CreatedAt = startedAt
            };

            foreach (var question in questions)
            {
                var attemptAnswer = new QuizAttemptAnswer
                {
                    QuizAttemptId = attempt.Id,
                    QuestionId = question.Id,
                    CreatedAt = takenAt
                };

                var selectedAnswers = correctlyAnswered.Contains(question.Id)
                    ? question.Answers.Where(answer => answer.IsCorrect)
                    : question.Answers.Where(answer => !answer.IsCorrect).Take(1);

                foreach (var answer in selectedAnswers)
                {
                    attemptAnswer.SelectedAnswers.Add(new QuizAttemptAnswerSelection
                    {
                        QuizAttemptAnswerId = attemptAnswer.Id,
                        AnswerId = answer.Id,
                        CreatedAt = takenAt
                    });
                }

                attempt.AttemptAnswers.Add(attemptAnswer);
            }

            var earnedPoints = cumulativePoints[correctCount];
            var percentage = totalPoints == 0 ? 0 : Math.Round(earnedPoints * 100.0 / totalPoints, 2);

            attempt.Score = earnedPoints;
            attempt.Percentage = percentage;
            attempt.Passed = percentage >= quiz.PassingScore;

            return attempt;
        }
        private int ChooseCorrectCount(int[] cumulativePoints, int totalPoints, int passingScore, bool shouldPass)
        {
            var questionCount = cumulativePoints.Length - 1;

            bool Passes(int count) =>
                totalPoints > 0 && Math.Round(cumulativePoints[count] * 100.0 / totalPoints, 2) >= passingScore;

            if (shouldPass)
            {
                var minimum = Enumerable.Range(0, questionCount + 1).FirstOrDefault(Passes, questionCount);
                return _rng.Next(minimum, questionCount + 1);
            }

            var maximumFailing = Enumerable.Range(0, questionCount + 1).Where(count => !Passes(count)).DefaultIfEmpty(0).Max();

            return _rng.Next(0, maximumFailing + 1);
        }

        // ------------------------------------------------------------------
        // Отзывы, избранное, уведомления
        // ------------------------------------------------------------------

        private static readonly string[] PositiveComments =
        {
            "Clear explanations and well-structured modules. The practical examples made everything click.",
            "One of the best courses I've taken here. The quizzes actually test understanding, not memorization.",
            "Great pacing. I could follow along and build the examples myself without getting lost.",
            "The instructor explains the 'why', not just the 'how'. That made a real difference for me.",
            "Excellent course. I applied what I learned at work the same week."
        };

        private static readonly string[] NeutralComments =
        {
            "Solid content overall, though a few lessons could use more examples.",
            "Good course for beginners. I'd like a bit more depth in the later modules.",
            "Useful material, but some videos felt a little rushed.",
            "Learned a lot, though I had to look up a few concepts elsewhere."
        };

        private void TryAddReview(
            Course course, ApplicationUser student, int completedLessons, int totalLessons,
            DateTime reviewedAt, DemoActivity activity)
        {
            var progressRatio = completedLessons / (double)totalLessons;
            if (progressRatio < 0.4 || _rng.NextDouble() > 0.7)
            {
                return;
            }

            var isPositive = progressRatio >= 0.8 || _rng.NextDouble() < 0.6;

            var rating = isPositive
                ? (_rng.NextDouble() < 0.65 ? 5 : 4)
                : (_rng.NextDouble() < 0.7 ? 4 : 3);

            var comment = isPositive
                ? PositiveComments[_rng.Next(PositiveComments.Length)]
                : NeutralComments[_rng.Next(NeutralComments.Length)];

            activity.Reviews.Add(new CourseReview
            {
                CourseId = course.Id,
                StudentId = student.Id,
                Rating = rating,
                Comment = comment,
                CreatedAt = reviewedAt
            });
        }

        private void SeedWishlist(IReadOnlyList<Course> courses, IReadOnlyList<ApplicationUser> students, DemoActivity activity)
        {
            var publishedCourses = courses.Where(course => course.Status == CourseStatus.Published).ToList();

            if (publishedCourses.Count == 0)
            {
                return;
            }

            var enrolledPairs = activity.Enrollments
                .Select(enrollment => (enrollment.StudentId, enrollment.CourseId))
                .ToHashSet();

            foreach (var student in students)
            {
                var candidates = publishedCourses
                    .Where(course => !enrolledPairs.Contains((student.Id, course.Id)))
                    .OrderBy(_ => _rng.Next())
                    .Take(_rng.Next(1, 4))
                    .ToList();

                foreach (var course in candidates)
                {
                    var addedAt = _now.AddDays(-_rng.Next(1, 40));

                    activity.WishlistItems.Add(new Wishlist
                    {
                        StudentId = student.Id,
                        CourseId = course.Id,
                        AddedAt = addedAt,
                        CreatedAt = addedAt
                    });
                }
            }
        }

        private void AddNotification(
            DemoActivity activity, ApplicationUser user, string title, string message,
            NotificationType type, DateTime createdAt)
        {
            if (createdAt > _now)
            {
                createdAt = _now.AddMinutes(-_rng.Next(5, 120));
            }

            activity.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = title,
                Message = message,
                Type = type,
                IsRead = (_now - createdAt).TotalDays > 7 || _rng.NextDouble() < 0.4,
                CreatedAt = createdAt
            });
        }
    }
}
