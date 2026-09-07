using LMSFinal.Contracts.DTOs.Lessons;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{

    public interface ILessonService
    {

        Task<IReadOnlyList<LessonDto>> GetByModuleIdAsync(Guid currentUserId, Guid moduleId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LessonLocalizedDto>> GetByModuleIdLocalizedAsync(Guid currentUserId, Guid moduleId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<LessonDto> GetByIdAsync(Guid currentUserId, Guid lessonId, CancellationToken cancellationToken = default);

        Task<LessonLocalizedDto> GetByIdLocalizedAsync(Guid currentUserId, Guid lessonId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<LessonDto> CreateAsync(Guid instructorId, Guid moduleId, CreateLessonRequest request, CancellationToken cancellationToken = default);

        Task<LessonDto> UpdateAsync(Guid instructorId, Guid lessonId, UpdateLessonRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid instructorId, Guid lessonId, CancellationToken cancellationToken = default);

        Task ReorderAsync(Guid instructorId, ReorderLessonsRequest request, CancellationToken cancellationToken = default);

        Task<LessonResourceDto> AddResourceAsync(
            Guid instructorId, Guid lessonId, LanguageCode languageCode,
            string fileName, string fileUrl, string fileType, CancellationToken cancellationToken = default);

        /// <returns>FileUrl удалённого материала — вызывающий код (контроллер) сам удаляет объект из хранилища.</returns>
        Task<string> RemoveResourceAsync(Guid instructorId, Guid lessonId, Guid resourceId, CancellationToken cancellationToken = default);
    }


}
