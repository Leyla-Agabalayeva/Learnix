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

        public async Task<IReadOnlyList<QuestionInput>> GenerateAsync(
            string lessonContent, int questionCount, CancellationToken cancellationToken = default)
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
                response_format = new { type = "json_object" }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var text = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                ?? "{\"questions\":[]}";

            var wrapper = JsonSerializer.Deserialize<GeneratedWrapper>(text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new GeneratedWrapper(new List<GeneratedQuestion>());

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
