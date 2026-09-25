using FluentValidation;
using LMSFinal.Contracts.DTOs.Auth;

namespace LMSFinal.Application.Validators.Auth
{

    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(request => request.FirstName)
                .NotEmpty().WithMessage("Имя обязательно.")
                .MaximumLength(100).WithMessage("Имя не длиннее 100 символов.");

            RuleFor(request => request.LastName)
                .NotEmpty().WithMessage("Фамилия обязательна.")
                .MaximumLength(100).WithMessage("Фамилия не длиннее 100 символов.");

            RuleFor(request => request.Bio)
                .MaximumLength(1000).WithMessage("Биография не длиннее 1000 символов.");

            RuleFor(request => request.AvatarUrl)
                .MaximumLength(500).WithMessage("Ссылка не длиннее 500 символов.")

                .Must(BeAValidUrl).When(request => !string.IsNullOrWhiteSpace(request.AvatarUrl))
                .WithMessage("Укажите корректную ссылку на изображение.");

            // Поля преподавателя необязательны при редактировании (студент их не присылает),
            // но если заполнены — проверяются так же, как при регистрации.
            RuleFor(request => request.YearsOfExperience)
                .InclusiveBetween(0, InstructorProfileRules.MaxYearsOfExperience)
                .When(request => request.YearsOfExperience.HasValue)
                .WithMessage($"Стаж должен быть от 0 до {InstructorProfileRules.MaxYearsOfExperience} лет.");

            RuleFor(request => request.EducationLevel)
                .Must(level => InstructorProfileRules.EducationLevels.Contains(level!))
                .When(request => !string.IsNullOrWhiteSpace(request.EducationLevel))
                .WithMessage("Некорректный уровень образования.");

            RuleFor(request => request.ProfileUrl)
                .Must(InstructorProfileRules.BeAValidWebUrl)
                .When(request => !string.IsNullOrWhiteSpace(request.ProfileUrl))
                .WithMessage("Укажите корректную ссылку (http:// или https://).");
        }

        private static bool BeAValidUrl(string? value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            || (value is not null && value.StartsWith('/'));
    }
}
