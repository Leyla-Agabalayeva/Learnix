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

            // Преподаватель заполняет профессиональный профиль сразу при регистрации —
            // студентов эти поля не касаются, поэтому правила действуют только для Instructor.
            When(x => x.Role == UserRole.Instructor, () =>
            {
                RuleFor(x => x.ProfessionalTitle)
                    .NotEmpty().WithMessage("Укажите вашу должность или профессию.");

                RuleFor(x => x.Specialization)
                    .NotEmpty().WithMessage("Укажите область, в которой вы преподаёте.");

                RuleFor(x => x.YearsOfExperience)
                    .NotNull().WithMessage("Укажите стаж работы.")
                    .InclusiveBetween(0, InstructorProfileRules.MaxYearsOfExperience)
                    .WithMessage($"Стаж должен быть от 0 до {InstructorProfileRules.MaxYearsOfExperience} лет.");

                RuleFor(x => x.Bio)
                    .NotEmpty().WithMessage("Расскажите о себе — студенты выбирают курсы по преподавателю.")
                    .MinimumLength(InstructorProfileRules.MinBioLength)
                    .WithMessage($"Расскажите о себе подробнее — минимум {InstructorProfileRules.MinBioLength} символов.");

                RuleFor(x => x.EducationLevel)
                    .Must(level => string.IsNullOrWhiteSpace(level) || InstructorProfileRules.EducationLevels.Contains(level))
                    .WithMessage("Некорректный уровень образования.");

                RuleFor(x => x.ProfileUrl)
                    .Must(InstructorProfileRules.BeAValidWebUrl)
                    .When(x => !string.IsNullOrWhiteSpace(x.ProfileUrl))
                    .WithMessage("Укажите корректную ссылку (http:// или https://).");
            });
        }
    }

}
