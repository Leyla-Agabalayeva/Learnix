using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LMSFinal.Persistence.Seed
{
    
    public sealed class SeedCatalog
    {
        private const string CatalogResourceSuffix = ".Seed.Data.catalog.json";
        private const string CoursesResourceMarker = ".Seed.Data.courses.";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private SeedCatalog(SeedCatalogFile catalog, IReadOnlyList<CourseSpec> courses)
        {
            Categories = catalog.Categories;
            Instructors = catalog.Instructors;
            Students = catalog.Students;
            Courses = courses;
        }

        public IReadOnlyList<CategorySpec> Categories { get; }
        public IReadOnlyList<UserSpec> Instructors { get; }
        public IReadOnlyList<UserSpec> Students { get; }
        public IReadOnlyList<CourseSpec> Courses { get; }

        public static SeedCatalog Load()
        {
            var assembly = typeof(SeedCatalog).Assembly;
            var resourceNames = assembly.GetManifestResourceNames();

            var catalogResource = resourceNames.FirstOrDefault(name => name.EndsWith(CatalogResourceSuffix, StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    "Не найден встроенный ресурс Seed/Data/catalog.json. " +
                    "Проверьте, что в LMSFinal.Persistence.csproj есть <EmbeddedResource Include=\"Seed\\Data\\**\\*.json\" />.");

            var catalog = Deserialize<SeedCatalogFile>(assembly, catalogResource);

            var courses = resourceNames
                .Where(name => name.Contains(CoursesResourceMarker, StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .Select(name => Deserialize<CourseSpec>(assembly, name))
                .ToList();

            if (courses.Count == 0)
            {
                throw new InvalidOperationException(
                    "В Seed/Data/courses не найдено ни одного JSON-файла с курсом.");
            }

            Validate(catalog, courses);

            return new SeedCatalog(catalog, courses);
        }

        private static T Deserialize<T>(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Не удалось открыть встроенный ресурс '{resourceName}'.");

            try
            {
                return JsonSerializer.Deserialize<T>(stream, JsonOptions)
                    ?? throw new InvalidOperationException($"Ресурс '{resourceName}' содержит пустой JSON.");
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Ошибка разбора JSON в ресурсе '{resourceName}': {exception.Message}", exception);
            }
        }

       
        private static void Validate(SeedCatalogFile catalog, IReadOnlyList<CourseSpec> courses)
        {
            var categorySlugs = catalog.Categories.Select(c => c.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var instructorEmails = catalog.Instructors.Select(i => i.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var problems = new List<string>();

            foreach (var course in courses)
            {
                var title = course.Title.En;

                if (!categorySlugs.Contains(course.CategorySlug))
                {
                    problems.Add($"Курс '{title}': категория '{course.CategorySlug}' отсутствует в catalog.json.");
                }

                if (!instructorEmails.Contains(course.InstructorEmail))
                {
                    problems.Add($"Курс '{title}': инструктор '{course.InstructorEmail}' отсутствует в catalog.json.");
                }

                if (course.Modules.Count == 0)
                {
                    problems.Add($"Курс '{title}': нет ни одного модуля.");
                }

                foreach (var quiz in course.Modules.SelectMany(m => m.Lessons).Where(l => l.Quiz is not null).Select(l => l.Quiz!))
                {
                    foreach (var question in quiz.Questions)
                    {
                        var correctCount = question.Answers.Count(a => a.IsCorrect);

             
                        if (correctCount == 0)
                        {
                            problems.Add($"Квиз '{quiz.Title}': у вопроса «{question.Text}» нет правильного ответа.");
                        }

                        if (question.Answers.Count(a => !a.IsCorrect) == 0)
                        {
                            problems.Add($"Квиз '{quiz.Title}': у вопроса «{question.Text}» нет ни одного неправильного варианта.");
                        }

                        if (question.Type == Domain.Enums.QuestionType.SingleChoice && correctCount > 1)
                        {
                            problems.Add($"Квиз '{quiz.Title}': вопрос «{question.Text}» помечен как SingleChoice, но правильных ответов {correctCount}.");
                        }
                    }
                }
            }

            if (problems.Count > 0)
            {
                throw new InvalidOperationException(
                    "Демо-контент не прошёл проверку:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
            }
        }
    }
}
