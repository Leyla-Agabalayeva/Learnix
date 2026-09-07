using AutoMapper;
using LMSFinal.Application.Common;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.Common;
using LMSFinal.Contracts.DTOs.Courses;
using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using LMSFinal.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LMSFinal.Application.Services
{


    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CourseService(
            ICourseRepository courseRepository,
            ICategoryRepository categoryRepository,
            IWishlistRepository wishlistRepository,
            IEnrollmentRepository enrollmentRepository,
            INotificationService notificationService,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _courseRepository = courseRepository;
            _categoryRepository = categoryRepository;
            _wishlistRepository = wishlistRepository;
            _enrollmentRepository = enrollmentRepository;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PagedResult<CourseSummaryDto>> SearchAsync(CourseSearchRequest request, CancellationToken cancellationToken = default)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 50);

            // Публичный каталог всегда показывает только Published — черновики и архив сюда не попадают.
            var (items, totalCount) = await _courseRepository.SearchAsync(
                request.SearchTerm, request.CategoryId, request.Level, request.MinPrice, request.MaxPrice,
                CourseStatus.Published, request.SortBy, page, pageSize, cancellationToken);

            return new PagedResult<CourseSummaryDto>
            {
                Items = _mapper.Map<IReadOnlyList<CourseSummaryDto>>(items),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<CourseDto> GetByIdAsync(Guid courseId, Guid? currentUserId, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetWithDetailsAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            var isOwner = currentUserId.HasValue && course.InstructorId == currentUserId.Value;

            // Записанный студент не должен терять доступ к курсу, который сам прошёл
            // или проходит, только из-за того, что преподаватель его потом заархивировал —
            // иначе "Перейти к курсу" в личном кабинете начинает бить в 404.
            var isEnrolled = currentUserId.HasValue
                && await _enrollmentRepository.IsEnrolledAsync(currentUserId.Value, courseId, cancellationToken);

            // Черновик/архив чужого курса не должен быть виден постороннему — отдаём 404,
            // а не 403, чтобы не подтверждать сам факт существования курса с таким Id.
            if (course.Status != CourseStatus.Published && !isOwner && !isEnrolled)
            {
                throw new NotFoundException("Course", courseId);
            }

            return _mapper.Map<CourseDto>(course);
        }

        /// <summary>
        /// Phase 14: та же логика видимости (Published-only для не-владельца), но на выходе —
        /// одно значение перевода вместо массива, с fallback AZ/EN через TranslationResolver.
        /// </summary>
        public async Task<PagedResult<CourseSummaryLocalizedDto>> SearchLocalizedAsync(
            CourseSearchRequest request, LanguageCode language, CancellationToken cancellationToken = default)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 50);

            var (items, totalCount) = await _courseRepository.SearchAsync(
                request.SearchTerm, request.CategoryId, request.Level, request.MinPrice, request.MaxPrice,
                CourseStatus.Published, request.SortBy, page, pageSize, cancellationToken);

            // Агрегаты для карточек берём одним запросом на всю страницу, а не по курсу:
            // иначе получили бы N+1 ровно там, где он больнее всего — в публичном каталоге.
            var stats = await _courseRepository.GetStatsAsync(
                items.Select(course => course.Id).ToList(), cancellationToken);

            return new PagedResult<CourseSummaryLocalizedDto>
            {
                Items = items.Select(c => MapSummaryLocalized(c, language, stats)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<CourseLocalizedDto> GetByIdLocalizedAsync(
            Guid courseId, Guid? currentUserId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            // Публичная страница курса показывает программу целиком, поэтому здесь
            // нужен курс вместе с модулями и уроками — в отличие от publish/update,
            // которым хватает GetWithDetailsAsync.
            var course = await _courseRepository.GetWithCurriculumAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            var isOwner = currentUserId.HasValue && course.InstructorId == currentUserId.Value;

            // Признак записи считает сервер: именно он переключает кнопку
            // «Записаться» на «Продолжить обучение». У гостя currentUserId нет,
            // поэтому лишнего запроса к базе не делаем.
            //
            // Считаем его ДО проверки видимости ниже: записанный студент не должен
            // терять доступ к курсу, который сам прошёл или проходит, только из-за
            // того, что преподаватель его потом заархивировал.
            var isEnrolled = currentUserId.HasValue
                && await _enrollmentRepository.IsEnrolledAsync(currentUserId.Value, courseId, cancellationToken);

            if (course.Status != CourseStatus.Published && !isOwner && !isEnrolled)
            {
                throw new NotFoundException("Course", courseId);
            }

            var stats = await _courseRepository.GetStatsAsync(new[] { courseId }, cancellationToken);

            return MapLocalized(course, language, stats.GetValueOrDefault(courseId) ?? EmptyStats, isEnrolled);
        }

        public async Task<IReadOnlyList<CourseSummaryDto>> GetMyCoursesAsync(Guid instructorId, CancellationToken cancellationToken = default)
        {
            var courses = await _courseRepository.GetByInstructorIdAsync(instructorId, cancellationToken);
            return _mapper.Map<IReadOnlyList<CourseSummaryDto>>(courses);
        }

        public async Task<IReadOnlyList<CourseSummaryLocalizedDto>> GetMyCoursesLocalizedAsync(
            Guid instructorId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            // Здесь, в отличие от каталога, возвращаются ВСЕ статусы: преподавателю
            // нужны и черновики, и архив — это его рабочий список.
            var courses = await _courseRepository.GetByInstructorIdAsync(instructorId, cancellationToken);

            var stats = await CourseSummaryMapper.LoadStatsAsync(_courseRepository, courses, cancellationToken);

            return courses
                .Select(course => CourseSummaryMapper.ToLocalized(course, language, stats.GetValueOrDefault(course.Id)))
                .ToList();
        }

        public async Task<CourseDto> CreateAsync(Guid instructorId, CreateCourseRequest request, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
                ?? throw new NotFoundException("Category", request.CategoryId);

            var course = new Course
            {
                InstructorId = instructorId,
                CategoryId = category.Id,
                ThumbnailUrl = request.ThumbnailUrl,
                Price = request.Price,
                DurationMinutes = request.DurationMinutes,
                Level = request.Level,
                Status = CourseStatus.Draft // новый курс всегда Draft — публикация отдельным вызовом
            };

            ApplyTranslations(course, request.Translations);

            await _courseRepository.AddAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _courseRepository.GetWithDetailsAsync(course.Id, cancellationToken)
                ?? throw new NotFoundException("Course", course.Id);

            return _mapper.Map<CourseDto>(created);
        }

        public async Task<CourseDto> UpdateAsync(Guid instructorId, Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetWithDetailsAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            EnsureOwnership(course, instructorId);

            var categoryExists = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
                ?? throw new NotFoundException("Category", request.CategoryId);

            course.CategoryId = categoryExists.Id;
            course.ThumbnailUrl = request.ThumbnailUrl;
            course.Price = request.Price;
            course.DurationMinutes = request.DurationMinutes;
            course.Level = request.Level;
            course.UpdatedAt = DateTime.UtcNow;

            // Полностью заменяем переводы — проще и надёжнее построчного сравнения старых/новых записей.
            // Course.Translations загружен через GetWithDetailsAsync (с отслеживанием изменений),
            // поэтому EF Core сам сформирует DELETE для убранных строк и INSERT для новых.
            //course.Translations.Clear();
            //ApplyTranslations(course, request.Translations);

            //_courseRepository.Update(course);
            //await _unitOfWork.SaveChangesAsync(cancellationToken);
            course.Translations.Clear();
            ApplyTranslations(course, request.Translations);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CourseDto>(course);
        }

        public async Task DeleteAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            EnsureOwnership(course, instructorId);

            // Курс со студентами удалять нельзя: вместе с ним исчезли бы прогресс,
            // результаты тестов и уже выданные сертификаты. На уровне базы это и так
            // запрещено (Restrict), но без явной проверки EF упирался бы в нарушение
            // внешнего ключа, и наружу уходила бы «Внутренняя ошибка сервера» —
            // вместо внятного объяснения, почему кнопка не сработала.
            var enrollments = await _enrollmentRepository.GetByCourseIdAsync(courseId, cancellationToken);

            if (enrollments.Count > 0)
            {
                throw new ConflictException(
                    "Нельзя удалить курс, на который записаны студенты. " +
                    "Снимите его с публикации или отправьте в архив.");
            }

            _courseRepository.Remove(course);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<CourseDto> PublishAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await GetOwnedCourseAsync(instructorId, courseId, cancellationToken);

            if (course.Translations.Count == 0 || course.Translations.Any(t => string.IsNullOrWhiteSpace(t.Title)))
            {
                throw new ConflictException("Нельзя опубликовать курс без названия хотя бы на одном языке.");
            }

            course.Status = CourseStatus.Published;
            course.UpdatedAt = DateTime.UtcNow;

            _courseRepository.Update(course);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Событие "course published" (раздел 18.5 ТЗ) — уведомляем только тех, кто добавил
            // этот курс в избранное (Wishlist), а не вообще всех студентов платформы: у них есть
            // явный сигнал интереса именно к этому курсу.
            await NotifyWishlistersAsync(course, cancellationToken);

            return _mapper.Map<CourseDto>(course);
        }

        public async Task<CourseDto> UnpublishAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await GetOwnedCourseAsync(instructorId, courseId, cancellationToken);

            if (course.Status != CourseStatus.Published)
            {
                throw new ConflictException("Можно снять с публикации только опубликованный курс.");
            }

            course.Status = CourseStatus.Draft;
            course.UpdatedAt = DateTime.UtcNow;

            _courseRepository.Update(course);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CourseDto>(course);
        }

        public async Task<CourseDto> ArchiveAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await GetOwnedCourseAsync(instructorId, courseId, cancellationToken);

            if (course.Status == CourseStatus.Archived)
            {
                throw new ConflictException("Курс уже в архиве.");
            }

            course.Status = CourseStatus.Archived;
            course.UpdatedAt = DateTime.UtcNow;

            _courseRepository.Update(course);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CourseDto>(course);
        }

        // --- helpers ---

        private async Task<Course> GetOwnedCourseAsync(Guid instructorId, Guid courseId, CancellationToken cancellationToken)
        {
            var course = await _courseRepository.GetWithDetailsAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            EnsureOwnership(course, instructorId);

            return course;
        }

        /// <summary>Ownership check — ключевое правило раздела 52 ТЗ: инструктор не может
        /// управлять чужим курсом. Бросает 403, а не 404, т.к. на этом этапе instructorId
        /// уже прошёл аутентификацию и точно существует — просто это не его курс.</summary>
        private async Task NotifyWishlistersAsync(Course course, CancellationToken cancellationToken)
        {
            var wishlistedBy = await _wishlistRepository.GetByCourseIdAsync(course.Id, cancellationToken);
            if (wishlistedBy.Count == 0)
            {
                return;
            }

            var courseTitle = course.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.EN)?.Title
                ?? course.Translations.FirstOrDefault()?.Title
                ?? "Course";

            foreach (var wishlistItem in wishlistedBy)
            {
                await _notificationService.NotifyAsync(
                    wishlistItem.StudentId,
                    "New course published",
                    $"\"{courseTitle}\" from your wishlist is now available!",
                    NotificationType.CoursePublished,
                    cancellationToken);
            }
        }

        private static void EnsureOwnership(Course course, Guid instructorId)
        {
            if (course.InstructorId != instructorId)
            {
                throw new ForbiddenAccessException("Вы можете управлять только своими курсами.");
            }
        }

        private static void ApplyTranslations(Course course, IReadOnlyList<CourseTranslationInput> translations)
        {
            foreach (var input in translations)
            {
                course.Translations.Add(new CourseTranslation
                {
                    Id = Guid.Empty,
                    LanguageCode = input.LanguageCode,
                    Title = input.Title,
                    ShortDescription = input.ShortDescription,
                    Description = input.Description,
                    WhatYouWillLearn = JsonSerializer.Serialize(input.WhatYouWillLearn ?? new List<string>())
                });
            }
        }

        private static CourseLocalizedDto MapLocalized(
            Course course, LanguageCode language, CourseStats stats, bool isEnrolled)
        {
            var translation = TranslationResolver.Resolve(course.Translations, language, t => t.LanguageCode)
                ?? throw new ConflictException($"У курса '{course.Id}' нет ни одного перевода.");

            var categoryName = TranslationResolver
                .Resolve(course.Category.Translations, language, t => t.LanguageCode)?.Name
                ?? course.Category.Slug;

            return new CourseLocalizedDto(
                course.Id,
                course.InstructorId,
                $"{course.Instructor.FirstName} {course.Instructor.LastName}",
                course.CategoryId,
                course.Category.Slug,
                course.ThumbnailUrl,
                course.Price,
                course.DurationMinutes,
                course.Level.ToString(),
                course.Status.ToString(),
                translation.LanguageCode.ToString(),
                translation.Title,
                translation.ShortDescription,
                translation.Description,
                JsonListHelper.DeserializeStringList(translation.WhatYouWillLearn),
                course.CreatedAt,
                course.UpdatedAt,
                categoryName,
                stats.AverageRating,
                stats.ReviewCount,
                stats.EnrollmentCount,
                stats.LessonCount,
                MapCurriculum(course, language),
                isEnrolled);
        }


        private static IReadOnlyList<CourseCurriculumModuleDto> MapCurriculum(Course course, LanguageCode language)
        {
            return course.Modules
                .OrderBy(module => module.OrderIndex)
                .Select(module =>
                {
                    var lessons = module.Lessons
                        .Where(lesson => lesson.IsPublished)
                        .OrderBy(lesson => lesson.OrderIndex)
                        .Select(lesson => new CourseCurriculumLessonDto(
                            lesson.Id,
                            lesson.OrderIndex,
                            TranslationResolver.Resolve(lesson.Translations, language, t => t.LanguageCode)?.Title ?? "—",
                            lesson.DurationMinutes,
                            lesson.Quiz is not null))
                        .ToList();

                    return new CourseCurriculumModuleDto(
                        module.Id,
                        module.OrderIndex,
                        TranslationResolver.Resolve(module.Translations, language, t => t.LanguageCode)?.Title ?? "—",
                        lessons.Count,
                        lessons.Sum(lesson => lesson.DurationMinutes),
                        lessons);
                })

                .Where(module => module.Lessons.Count > 0)
                .ToList();
        }


        private static CourseSummaryLocalizedDto MapSummaryLocalized(
            Course course, LanguageCode language, IReadOnlyDictionary<Guid, CourseStats> stats) =>
            CourseSummaryMapper.ToLocalized(course, language, stats.GetValueOrDefault(course.Id));

        private static readonly CourseStats EmptyStats = CourseSummaryMapper.EmptyStats;
    }

}
