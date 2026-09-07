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
        }

        private static bool BeAValidUrl(string? value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            || (value is not null && value.StartsWith('/'));
    }
}
