using System;
using System.Collections.Generic;

namespace LMSFinal.Application.Validators.Auth
{
    /// <summary>
    /// Общие константы профессионального профиля преподавателя. Вынесены отдельно, чтобы
    /// регистрация и редактирование профиля проверяли одни и те же значения одинаково.
    /// </summary>
    internal static class InstructorProfileRules
    {
        public const int MinBioLength = 30;
        public const int MaxYearsOfExperience = 60;

        public static readonly IReadOnlyCollection<string> EducationLevels =
            new[] { "HighSchool", "Bachelor", "Master", "Doctorate", "Other" };

        public static bool BeAValidWebUrl(string? value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
