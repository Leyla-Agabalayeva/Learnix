using AutoMapper;
using LMSFinal.Application.Common;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Modules;
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
    public class ModuleService : IModuleService
    {
        private readonly IModuleRepository _moduleRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ModuleService(
            IModuleRepository moduleRepository,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _moduleRepository = moduleRepository;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Phase 9: открыто не только владельцу-инструктору, но и записанному студенту —
        /// это и было обещано в README Phase 6 ("появится вместе с Lesson Progress").
        /// </summary>
        public async Task<IReadOnlyList<ModuleDto>> GetByCourseIdAsync(Guid currentUserId, Guid courseId, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            var isOwner = course.InstructorId == currentUserId;

            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, courseId, cancellationToken);
                if (!isEnrolled)
                {
                    throw new ForbiddenAccessException("Чтобы открыть программу курса, запишитесь на курс.");
                }
            }

            var modules = await _moduleRepository.GetByCourseIdAsync(courseId, cancellationToken);
            return _mapper.Map<IReadOnlyList<ModuleDto>>(modules);
        }

        /// <summary>Phase 14: та же ownership/enrollment-проверка, локализованный вывод.</summary>
        public async Task<IReadOnlyList<ModuleLocalizedDto>> GetByCourseIdLocalizedAsync(
            Guid currentUserId, Guid courseId, LanguageCode language, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            var isOwner = course.InstructorId == currentUserId;

            if (!isOwner)
            {
                var isEnrolled = await _enrollmentRepository.IsEnrolledAsync(currentUserId, courseId, cancellationToken);
                if (!isEnrolled)
                {
                    throw new ForbiddenAccessException("Чтобы открыть программу курса, запишитесь на курс.");
                }
            }

            var modules = await _moduleRepository.GetByCourseIdAsync(courseId, cancellationToken);
            return modules.Select(m => MapLocalized(m, language)).ToList();
        }

        public async Task<ModuleDto> CreateAsync(Guid instructorId, Guid courseId, CreateModuleRequest request, CancellationToken cancellationToken = default)
        {
            var course = await _courseRepository.GetByIdAsync(courseId, cancellationToken)
                ?? throw new NotFoundException("Course", courseId);

            EnsureCourseOwnership(course, instructorId);

            var nextOrderIndex = await _moduleRepository.GetNextOrderIndexAsync(courseId, cancellationToken);

            var module = new Module
            {
                CourseId = courseId,
                OrderIndex = nextOrderIndex
            };

            ApplyTranslations(module, request.Translations);

            await _moduleRepository.AddAsync(module, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ModuleDto>(module);
        }

        public async Task<ModuleDto> UpdateAsync(Guid instructorId, Guid moduleId, UpdateModuleRequest request, CancellationToken cancellationToken = default)
        {
            var module = await _moduleRepository.GetWithCourseAsync(moduleId, cancellationToken)
                ?? throw new NotFoundException("Module", moduleId);

            EnsureCourseOwnership(module.Course, instructorId);
            module.Translations.Clear();
            ApplyTranslations(module, request.Translations);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<ModuleDto>(module);
        }

        public async Task DeleteAsync(Guid instructorId, Guid moduleId, CancellationToken cancellationToken = default)
        {
            var module = await _moduleRepository.GetWithCourseAsync(moduleId, cancellationToken)
                ?? throw new NotFoundException("Module", moduleId);

            EnsureCourseOwnership(module.Course, instructorId);

            _moduleRepository.Remove(module);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task ReorderAsync(Guid instructorId, ReorderModulesRequest request, CancellationToken cancellationToken = default)
        {
            var ids = request.Items.Select(i => i.ModuleId).ToList();
            var modules = await _moduleRepository.GetByIdsWithCourseAsync(ids, cancellationToken);

            if (modules.Count != ids.Count)
            {
                throw new NotFoundException("Один или несколько модулей не найдены.");
            }

            var distinctCourseIds = modules.Select(m => m.CourseId).Distinct().Count();
            if (distinctCourseIds > 1)
            {
                throw new ConflictException("Нельзя одновременно менять порядок модулей из разных курсов.");
            }

            foreach (var module in modules)
            {
                EnsureCourseOwnership(module.Course, instructorId);
            }

            var orderMap = request.Items.ToDictionary(i => i.ModuleId, i => i.OrderIndex);

            foreach (var module in modules)
            {
                module.OrderIndex = orderMap[module.Id];
                _moduleRepository.Update(module);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // --- helpers ---

        private static void EnsureCourseOwnership(Course course, Guid instructorId)
        {
            if (course.InstructorId != instructorId)
            {
                throw new ForbiddenAccessException("Вы можете управлять только модулями своих курсов.");
            }
        }

        private static void ApplyTranslations(Module module, IReadOnlyList<ModuleTranslationInput> translations)
        {
            foreach (var input in translations)
            {
                module.Translations.Add(new ModuleTranslation
                {
                    Id = Guid.Empty,
                    LanguageCode = input.LanguageCode,
                    Title = input.Title,
                    Description = input.Description
                });
            }
        }

        private static ModuleLocalizedDto MapLocalized(Module module, LanguageCode language)
        {
            var translation = TranslationResolver.Resolve(module.Translations, language, t => t.LanguageCode)
                ?? throw new ConflictException($"У модуля '{module.Id}' нет ни одного перевода.");

            return new ModuleLocalizedDto(
                module.Id,
                module.CourseId,
                module.OrderIndex,
                translation.LanguageCode.ToString(),
                translation.Title,
                translation.Description);
        }
    }


}
