using FluentValidation;
using LMSFinal.Contracts.DTOs.Lessons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Lessons
{
    public class ReorderLessonsRequestValidator : AbstractValidator<ReorderLessonsRequest>
    {
        public ReorderLessonsRequestValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Список уроков для переупорядочивания пуст.");

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.LessonId).Distinct().Count() == items.Count)
                .WithMessage("В списке есть повторяющиеся LessonId.")
                .When(x => x.Items.Count > 0);

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.OrderIndex).GreaterThanOrEqualTo(0);
            });
        }
    }

}
