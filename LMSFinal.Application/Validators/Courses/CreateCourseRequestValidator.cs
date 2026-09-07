using FluentValidation;
using LMSFinal.Contracts.DTOs.Courses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Courses
{
    public class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
    {
        public CreateCourseRequestValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Категория обязательна.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Цена не может быть отрицательной.");

            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0).WithMessage("Длительность курса должна быть больше 0.");

            RuleFor(x => x.Level)
                .IsInEnum().WithMessage("Некорректный уровень курса.");

            RuleFor(x => x.Translations)
                .NotEmpty().WithMessage("Нужен хотя бы один перевод курса (AZ/EN/RU).");

            RuleForEach(x => x.Translations).ChildRules(translation =>
            {
                translation.RuleFor(t => t.Title)
                    .NotEmpty().WithMessage("Название курса обязательно.")
                    .MaximumLength(300);

                translation.RuleFor(t => t.ShortDescription)
                    .NotEmpty().WithMessage("Краткое описание обязательно.")
                    .MaximumLength(500);

                translation.RuleFor(t => t.Description)
                    .NotEmpty().WithMessage("Полное описание обязательно.");
            });
        }
    }

}
