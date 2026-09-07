using LMSFinal.Contracts.DTOs.Quizzes;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface IQuizService
    {

        Task<QuizDto> GetByIdAsync(Guid currentUserId, Guid quizId, CancellationToken cancellationToken = default);

        Task<QuizLocalizedDto> GetByIdLocalizedAsync(
            Guid currentUserId, Guid quizId, LanguageCode language, CancellationToken cancellationToken = default);

        Task<QuizDto> CreateAsync(Guid instructorId, CreateQuizRequest request, CancellationToken cancellationToken = default);

        Task<QuizDto> UpdateAsync(Guid instructorId, Guid quizId, UpdateQuizRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid instructorId, Guid quizId, CancellationToken cancellationToken = default);

        Task<QuizResultDto> SubmitAsync(Guid studentId, Guid quizId, SubmitQuizRequest request, CancellationToken cancellationToken = default);


        Task<IReadOnlyList<QuizResultDto>> GetResultsAsync(Guid currentUserId, Guid quizId, CancellationToken cancellationToken = default);
    }

}
