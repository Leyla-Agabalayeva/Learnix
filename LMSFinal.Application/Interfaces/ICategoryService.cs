using LMSFinal.Contracts.DTOs.Categories;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

     
        Task<IReadOnlyList<CategoryLocalizedDto>> GetAllLocalizedAsync(LanguageCode language, CancellationToken cancellationToken = default);

        Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

        Task<CategoryDto> UpdateAsync(Guid categoryId, CreateCategoryRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
    }


}
