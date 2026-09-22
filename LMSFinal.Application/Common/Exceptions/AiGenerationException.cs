using System;

namespace LMSFinal.Application.Common.Exceptions
{
    /// <summary>
    /// Внешний AI-сервис ответил, но результат нельзя использовать — например,
    /// DeepSeek иногда обрезает JSON на середине из-за лимита токенов. Отдельный
    /// тип нужен, чтобы GlobalExceptionMiddleware не путал это с "клиент прислал
    /// кривой JSON" (оба сценария иначе ловились бы одним и тем же JsonException).
    /// </summary>
    public class AiGenerationException : Exception
    {
        public AiGenerationException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
