using FluentValidation;
using LMSFinal.Contracts.DTOs.Auth;

namespace LMSFinal.Application.Validators.Auth
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен.")
                .EmailAddress().WithMessage("Некорректный формат email.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Ссылка для сброса пароля повреждена — запросите новую.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Пароль обязателен.")
                .MinimumLength(8).WithMessage("Пароль должен быть не короче 8 символов.");
        }
    }
}
