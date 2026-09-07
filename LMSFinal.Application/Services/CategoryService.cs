using AutoMapper;
using LMSFinal.Application.Common;
using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Categories;
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
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(
            ICategoryRepository categoryRepository, ICourseRepository courseRepository,
            IUnitOfWork unitOfWork, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _courseRepository = courseRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _categoryRepository.GetAllWithTranslationsAsync(cancellationToken);
            return _mapper.Map<IReadOnlyList<CategoryDto>>(categories);
        }

        /// <summary>локализованный список — один язык с fallback AZ/EN вместо массива переводов.</summary>
        public async Task<IReadOnlyList<CategoryLocalizedDto>> GetAllLocalizedAsync(LanguageCode language, CancellationToken cancellationToken = default)
        {
            var categories = await _categoryRepository.GetAllWithTranslationsAsync(cancellationToken);
            return categories.Select(c => MapLocalized(c, language)).ToList();
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var existing = await _categoryRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (existing is not null)
            {
                throw new ConflictException($"Категория со slug '{request.Slug}' уже существует.");
            }

            var category = new Category
            {
                Slug = request.Slug,
                IconUrl = request.IconUrl
            };

            foreach (var t in request.Translations)
            {
                category.Translations.Add(new CategoryTranslation
                {
                    LanguageCode = t.LanguageCode,
                    Name = t.Name,
                    Description = t.Description
                });
            }

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task<CategoryDto> UpdateAsync(Guid categoryId, CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdWithTranslationsAsync(categoryId, cancellationToken)
                ?? throw new NotFoundException("Category", categoryId);

            var slugTaken = await _categoryRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (slugTaken is not null && slugTaken.Id != categoryId)
            {
                throw new ConflictException($"Категория со slug '{request.Slug}' уже существует.");
            }

            category.Slug = request.Slug;
            category.IconUrl = request.IconUrl;
            category.Translations.Clear();

            foreach (var t in request.Translations)
            {
                category.Translations.Add(new CategoryTranslation
                {
                    Id = Guid.Empty,
                    LanguageCode = t.LanguageCode,
                    Name = t.Name,
                    Description = t.Description
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdWithTranslationsAsync(categoryId, cancellationToken)
                ?? throw new NotFoundException("Category", categoryId);

            var (courses, _) = await _courseRepository.SearchAsync(
                searchTerm: null, categoryId: categoryId, level: null, minPrice: null, maxPrice: null,
                status: null, CourseSortBy.Newest, page: 1, pageSize: 1, cancellationToken);

            if (courses.Count > 0)
            {
                throw new ConflictException("Нельзя удалить категорию, в которой есть курсы.");
            }

            _categoryRepository.Remove(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static CategoryLocalizedDto MapLocalized(Category category, LanguageCode language)
        {
            var translation = TranslationResolver.Resolve(category.Translations, language, t => t.LanguageCode)
                ?? throw new ConflictException($"У категории '{category.Id}' нет ни одного перевода.");

            return new CategoryLocalizedDto(
                category.Id,
                category.Slug,
                category.IconUrl,
                translation.LanguageCode.ToString(),
                translation.Name,
                translation.Description);
        }
    }

}
