using AutoMapper;
using LMSFinal.Application.Common;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Lessons;
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

    public class LessonService : ILessonService
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LessonService(
            ILessonRepository lessonRepository,
            IModuleRepository moduleRepository,
            IEnrollmentRepository enrollmentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _lessonRepository = lessonRepository;
            _moduleRepository = moduleRepository;
            _enrollmentRepository = enrollmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        public async Task<IReadOnlyList<LessonDto>> GetByModuleIdAsync(Guid currentUserId, Guid moduleId, CancellationToken cancellationToken = default)
        {
            var module = await _moduleRepository.GetWithCourseAsync(moduleId, cancellationToken)
                ?? throw new NotFoundException("Module", moduleId);

            var isOwner = module.Course.InstructorId == currentUserId;

            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, module.CourseId, cancellationToken);
                if (!isEnrolled)
                {
                    throw new ForbiddenAccessException("Чтобы открыть уроки, запишитесь на курс.");
                }
            }

            var lessons = await _lessonRepository.GetByModuleIdAsync(moduleId, cancellationToken);

            if (!isOwner)
            {
                lessons = lessons.Where(l => l.IsPublished).ToList();
            }

            return _mapper.Map<IReadOnlyList<LessonDto>>(lessons);
        }

        public async Task<IReadOnlyList<LessonLocalizedDto>> GetByModuleIdLocalizedAsync(
            Guid currentUserId, Guid moduleId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            var module = await _moduleRepository.GetWithCourseAsync(moduleId, cancellationToken)
                ?? throw new NotFoundException("Module", moduleId);

            var isOwner = module.Course.InstructorId == currentUserId;

            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, module.CourseId, cancellationToken);
                if (!isEnrolled)
                {
                    throw new ForbiddenAccessException("Чтобы открыть уроки, запишитесь на курс.");
                }
            }

            var lessons = await _lessonRepository.GetByModuleIdAsync(moduleId, cancellationToken);

            if (!isOwner)
            {
                lessons = lessons.Where(l => l.IsPublished).ToList();
            }

            return lessons.Select(l => MapLocalized(l, language)).ToList();
        }

        public async Task<LessonDto> GetByIdAsync(Guid currentUserId, Guid lessonId, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(lessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", lessonId);

            var isOwner = lesson.Module.Course.InstructorId == currentUserId;

            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, lesson.Module.CourseId, cancellationToken);
                if (!isEnrolled || !lesson.IsPublished)
                {
                    throw new ForbiddenAccessException("Чтобы открыть урок, запишитесь на курс.");
                }
            }

            return _mapper.Map<LessonDto>(lesson);
        }


        public async Task<LessonLocalizedDto> GetByIdLocalizedAsync(
            Guid currentUserId, Guid lessonId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(lessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", lessonId);

            var isOwner = lesson.Module.Course.InstructorId == currentUserId;

            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, lesson.Module.CourseId, cancellationToken);
                if (!isEnrolled || !lesson.IsPublished)
                {
                    throw new ForbiddenAccessException("Чтобы открыть урок, запишитесь на курс.");
                }
            }

            return MapLocalized(lesson, language);
        }

        public async Task<LessonDto> CreateAsync(Guid instructorId, Guid moduleId, CreateLessonRequest request, CancellationToken cancellationToken = default)
        {
            var module = await _moduleRepository.GetWithCourseAsync(moduleId, cancellationToken)
                ?? throw new NotFoundException("Module", moduleId);

            EnsureCourseOwnership(module.Course, instructorId);

            var nextOrderIndex = await _lessonRepository.GetNextOrderIndexAsync(moduleId, cancellationToken);

            var lesson = new Lesson
            {
                ModuleId = moduleId,
                VideoUrl = request.VideoUrl,
                DurationMinutes = request.DurationMinutes,
                OrderIndex = nextOrderIndex,
                IsPublished = request.IsPublished
            };

            ApplyTranslations(lesson, request.Translations);

            await _lessonRepository.AddAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<LessonDto>(lesson);
        }

        public async Task<LessonDto> UpdateAsync(Guid instructorId, Guid lessonId, UpdateLessonRequest request, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(lessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", lessonId);

            EnsureCourseOwnership(lesson.Module.Course, instructorId);

            lesson.VideoUrl = request.VideoUrl;
            lesson.DurationMinutes = request.DurationMinutes;
            lesson.IsPublished = request.IsPublished;
            lesson.UpdatedAt = DateTime.UtcNow;
            lesson.Translations.Clear();
            ApplyTranslations(lesson, request.Translations);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<LessonDto>(lesson);
        }

        public async Task DeleteAsync(Guid instructorId, Guid lessonId, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(lessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", lessonId);

            EnsureCourseOwnership(lesson.Module.Course, instructorId);

            _lessonRepository.Remove(lesson);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ReorderAsync(Guid instructorId, ReorderLessonsRequest request, CancellationToken cancellationToken = default)
        {
            var ids = request.Items.Select(i => i.LessonId).ToList();
            var lessons = await _lessonRepository.GetByIdsWithModuleAndCourseAsync(ids, cancellationToken);

            if (lessons.Count != ids.Count)
            {
                throw new NotFoundException("Один или несколько уроков не найдены.");
            }

            var distinctModuleIds = lessons.Select(l => l.ModuleId).Distinct().Count();
            if (distinctModuleIds > 1)
            {
                throw new ConflictException("Нельзя одновременно менять порядок уроков из разных модулей.");
            }

            foreach (var lesson in lessons)
            {
                EnsureCourseOwnership(lesson.Module.Course, instructorId);
            }

            var orderMap = request.Items.ToDictionary(i => i.LessonId, i => i.OrderIndex);

            foreach (var lesson in lessons)
            {
                lesson.OrderIndex = orderMap[lesson.Id];
                _lessonRepository.Update(lesson);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<LessonResourceDto> AddResourceAsync(
            Guid instructorId, Guid lessonId, LanguageCode languageCode,
            string fileName, string fileUrl, string fileType, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(lessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", lessonId);

            EnsureCourseOwnership(lesson.Module.Course, instructorId);

            var resource = new LessonResource
            {
                Id = Guid.Empty,
                LessonId = lessonId,
                LanguageCode = languageCode,
                FileName = fileName,
                FileUrl = fileUrl,
                FileType = fileType
            };

            lesson.Resources.Add(resource);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new LessonResourceDto(resource.Id, resource.FileName, resource.FileUrl, resource.FileType);
        }

        public async Task<string> RemoveResourceAsync(
            Guid instructorId, Guid lessonId, Guid resourceId, CancellationToken cancellationToken = default)
        {
            var lesson = await _lessonRepository.GetWithModuleAndCourseAsync(lessonId, cancellationToken)
                ?? throw new NotFoundException("Lesson", lessonId);

            EnsureCourseOwnership(lesson.Module.Course, instructorId);

            var resource = lesson.Resources.FirstOrDefault(r => r.Id == resourceId)
                ?? throw new NotFoundException("LessonResource", resourceId);

            lesson.Resources.Remove(resource);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return resource.FileUrl;
        }

        // --- helpers ---

        private static void EnsureCourseOwnership(Course course, Guid instructorId)
        {
            if (course.InstructorId != instructorId)
            {
                throw new ForbiddenAccessException("Вы можете управлять только уроками своих курсов.");
            }
        }

        private static void ApplyTranslations(Lesson lesson, IReadOnlyList<LessonTranslationInput> translations)
        {
            foreach (var input in translations)
            {
                lesson.Translations.Add(new LessonTranslation
                {
                    Id = Guid.Empty,
                    LanguageCode = input.LanguageCode,
                    Title = input.Title,
                    Description = input.Description,
                    Content = input.Content
                });
            }
        }

        private static LessonLocalizedDto MapLocalized(Lesson lesson, LanguageCode language)
        {
            var translation = TranslationResolver.Resolve(lesson.Translations, language, t => t.LanguageCode)
                ?? throw new ConflictException($"У урока '{lesson.Id}' нет ни одного перевода.");

            return new LessonLocalizedDto(
                lesson.Id,
                lesson.ModuleId,
                lesson.VideoUrl,
                lesson.DurationMinutes,
                lesson.OrderIndex,
                lesson.IsPublished,
                translation.LanguageCode.ToString(),
                translation.Title,
                translation.Description,
                translation.Content,
                lesson.Quiz?.Id,

                lesson.Resources
                    .Where(r => r.LanguageCode == translation.LanguageCode)
                    .OrderBy(r => r.FileName)
                    .Select(r => new LessonResourceDto(r.Id, r.FileName, r.FileUrl, r.FileType))
                    .ToList());
        }
    }

}
