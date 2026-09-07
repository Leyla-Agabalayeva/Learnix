using FluentValidation;
using LMSFinal.Contracts.DTOs.Auth;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен.")
                .EmailAddress().WithMessage("Некорректный формат email.")
                .MaximumLength(256);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен.")
                .MinimumLength(8).WithMessage("Пароль должен быть не короче 8 символов.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Имя обязательно.")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Фамилия обязательна.")
                .MaximumLength(100);

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Некорректная роль.")
                .Must(role => role == UserRole.Instructor || role == UserRole.Student)
                .WithMessage("Регистрация доступна только с ролью Instructor или Student.");
        }
    }

}
