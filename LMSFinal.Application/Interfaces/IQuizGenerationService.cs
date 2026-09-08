using LMSFinal.Contracts.DTOs.Quizzes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface IQuizGenerationService
    {
        /// <summary>Каждый вопрос и ответ возвращается сразу с переводами на AZ/EN/RU.</summary>
        Task<IReadOnlyList<QuestionInput>> GenerateAsync(
            string lessonContent, int questionCount, CancellationToken cancellationToken = default);
    }
}
