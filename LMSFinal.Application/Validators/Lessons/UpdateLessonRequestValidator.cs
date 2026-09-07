using FluentValidation;
using LMSFinal.Contracts.DTOs.Lessons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Lessons
{
    public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
    {
        public UpdateLessonRequestValidator()
        {
            RuleFor(x => x.DurationMinutes)
                .GreaterThanOrEqualTo(0).WithMessage("Длительность урока не может быть отрицательной.");

            RuleFor(x => x.VideoUrl)
                .MaximumLength(500);

            RuleFor(x => x.Translations)
                .NotEmpty().WithMessage("Нужен хотя бы один перевод урока.");

            RuleForEach(x => x.Translations).ChildRules(t =>
            {
                t.RuleFor(x => x.Title)
                    .NotEmpty().WithMessage("Название урока обязательно.")
                    .MaximumLength(300);

                t.RuleFor(x => x.Content)
                    .NotEmpty().WithMessage("Содержимое урока обязательно.");
            });
        }
    }

}
