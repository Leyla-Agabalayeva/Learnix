using Microsoft.OpenApi.Models;

namespace LMSFinal.WebApi.Swagger
{
    public static class SwaggerTags
    {
        private static readonly (string Name, string Description)[] Definitions =
        {
            ("Auth",          "F1. Регистрация, вход, выход, текущий пользователь. Выдача JWT и проверка ролей."),
            ("Categories",    "Справочник категорий курсов. Категория — отдельная сущность БД со своими переводами, а не строка в курсе."),
            ("Courses",       "F2. Курсы: публичный каталог с поиском, фильтрами и пагинацией; CRUD для инструктора; publish / unpublish / archive."),
            ("Modules",       "F2. Модули курса и их порядок (OrderIndex) — источник данных для drag & drop в Course Builder."),
            ("Lessons",       "F2. Уроки внутри модуля: текст, видео, порядок, reorder."),
            ("Enrollments",   "F3. Запись студента на курс, список своих курсов, список студентов курса для инструктора."),
            ("Progress",      "F5. Отметка урока пройденным, процент прохождения курса, продолжение с места остановки."),
            ("Quizzes",       "F4. Конструктор тестов и сдача теста. Баллы считает СЕРВЕР — результат с фронтенда не принимается."),
            ("GradeBook",     "F6. Сводная ведомость студента: результаты всех тестов, средний балл, процент завершения курсов."),
            ("Reviews",       "F7. Рейтинг и отзывы. Оставить отзыв может только записанный на курс студент, один отзыв на курс."),
            ("Certificates",  "F8. Сертификаты: автоматическая выдача при 100% прохождения, свои сертификаты, скачивание PDF и публичная проверка по номеру."),
            ("Wishlist",      "Избранные курсы студента."),
            ("Notifications", "Уведомления пользователя (курс опубликован, результат теста, выдан сертификат)."),
            ("Analytics",     "Аналитика инструктора: сводка для дашборда и детальная статистика по конкретному курсу.")
        };
        public static void Apply(OpenApiDocument document)
        {
            var known = Definitions.Select(definition => new OpenApiTag
            {
                Name = definition.Name,
                Description = definition.Description
            });

            var knownNames = Definitions.Select(definition => definition.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unknown = document.Paths
                .SelectMany(path => path.Value.Operations.Values)
                .SelectMany(operation => operation.Tags ?? new List<OpenApiTag>())
                .Select(tag => tag.Name)
                .Where(name => !string.IsNullOrEmpty(name) && !knownNames.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(name => new OpenApiTag { Name = name });

            document.Tags = known.Concat(unknown).ToList();
        }
    }
}
