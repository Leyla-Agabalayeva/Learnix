using LMSFinal.Application.Common.Exceptions;
using LMSFinal.Application.Interfaces;
using LMSFinal.Contracts.DTOs.Quizzes;
using LMSFinal.Domain.Enums;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LMSFinal.Infrastructure.AI
{
    public class DeepSeekQuizGenerationService : IQuizGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly DeepSeekSettings _settings;

        public DeepSeekQuizGenerationService(HttpClient httpClient, IOptions<DeepSeekSettings> settings)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.deepseek.com/");
            _settings = settings.Value;
        }

        // DeepSeek изредка отдаёт структурно битый JSON (лишняя/пропущенная скобка
        // где-то в середине) — не из-за нехватки токенов, а просто самой модели
        // иногда "не везёт" на конкретной генерации. Повторный запрос почти всегда
        // проходит с первого раза, так что один автоматический retry прячет эту
        // нестабильность от преподавателя вместо того, чтобы показывать ему ошибку
        // и заставлять жать кнопку самому.
        private const int MaxAttempts = 2;

        public async Task<IReadOnlyList<QuestionInput>> GenerateAsync(
            string lessonContent, int questionCount, CancellationToken cancellationToken = default)
        {
            AiGenerationException? lastFailure = null;

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    return await GenerateOnceAsync(lessonContent, questionCount, cancellationToken);
                }
                catch (AiGenerationException ex)
                {
                    lastFailure = ex;
                }
            }

            throw lastFailure!;
        }

        private async Task<IReadOnlyList<QuestionInput>> GenerateOnceAsync(
            string lessonContent, int questionCount, CancellationToken cancellationToken)
        {
            var prompt = BuildPrompt(lessonContent, questionCount);

            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var body = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = "You generate quiz questions and reply with JSON only, no markdown." },
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                // 5 вопросов × 3 языка × 4 варианта ответа — это ощутимо больше текста, чем
                // модель кладёт в дефолтный бюджет токенов. Без явного max_tokens DeepSeek
                // изредка обрывал JSON на середине, и он падал на парсинге ниже.
                max_tokens = 4096
            };

            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new AiGenerationException("Не удалось связаться с AI-сервисом. Попробуйте ещё раз.", ex);
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var text = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                ?? "{\"questions\":[]}";

            GeneratedWrapper wrapper;
            try
            {
                wrapper = JsonSerializer.Deserialize<GeneratedWrapper>(text,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new GeneratedWrapper(new List<GeneratedQuestion>());
            }
            catch (JsonException ex)
            {
                // Сюда попадаем, когда сам ответ AI не распарсился — это внешний сбой,
                // а не то, что студент/преподаватель прислал кривые данные. Дальше эта
                // ошибка идёт как AiGenerationException, а не «проверьте форму».
                throw new AiGenerationException("AI вернул некорректный ответ. Попробуйте сгенерировать ещё раз.", ex);
            }

            return wrapper.Questions.Select(q => new QuestionInput(
                ToTranslations(q.Translations, (lang, value) => new QuestionTranslationInput(lang, value)),
                QuestionType.SingleChoice,
                1,
                q.Answers.Select(a => new AnswerInput(
                    ToTranslations(a.Translations, (lang, value) => new AnswerTranslationInput(lang, value)),
                    a.IsCorrect)).ToList()
            )).ToList();
        }

        /// <summary>
        /// DeepSeek возвращает переводы как словарь "az"/"en"/"ru" → текст. Пропущенный
        /// язык (модель иногда экономит на одном из трёх) подстраховываем текстом любого
        /// другого — иначе TranslationResolver на фронтенде получил бы дыру в переводах.
        /// </summary>
        private static IReadOnlyList<T> ToTranslations<T>(Dictionary<string, string> translations, Func<LanguageCode, string, T> factory)
        {
            var fallback = translations.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

            return new[] { LanguageCode.AZ, LanguageCode.EN, LanguageCode.RU }
                .Select(lang => factory(lang, GetOrFallback(translations, lang, fallback)))
                .ToList();
        }

        private static string GetOrFallback(Dictionary<string, string> translations, LanguageCode language, string fallback)
        {
            var key = language.ToString().ToLowerInvariant();
            return translations.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
        }

        private const string JsonShapeHint =
            "{\"questions\": [{\"translations\": {\"az\": \"...\", \"en\": \"...\", \"ru\": \"...\"}, " +
            "\"answers\": [{\"translations\": {\"az\": \"...\", \"en\": \"...\", \"ru\": \"...\"}, \"isCorrect\": true}, ...]}]}";

        private static string BuildPrompt(string lessonContent, int questionCount) =>
            $"""
            You are creating a quiz for an online course lesson. Base every question strictly on the
            lesson content below — do not invent an unrelated topic even if the content looks short
            or incomplete. Generate exactly {questionCount} multiple-choice questions. Each question
            needs its text translated into all three languages: Azerbaijani (az), English (en), and
            Russian (ru) — a native-quality translation, not a literal word-for-word one. Each question
            has exactly 4 answer options (each also translated into az/en/ru), exactly one of which is correct.

            Reply with a JSON object of this exact shape:
            {JsonShapeHint}

            Lesson content:
            {lessonContent}
            """;

        private record GeneratedWrapper(List<GeneratedQuestion> Questions);
        private record GeneratedQuestion(Dictionary<string, string> Translations, List<GeneratedAnswer> Answers);
        private record GeneratedAnswer(Dictionary<string, string> Translations, bool IsCorrect);
    }
}
