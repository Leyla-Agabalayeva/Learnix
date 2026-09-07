using FluentValidation;
using LMSFinal.Contracts.DTOs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Modules
{

    public class ReorderModulesRequestValidator : AbstractValidator<ReorderModulesRequest>
    {
        public ReorderModulesRequestValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Список модулей для переупорядочивания пуст.");

            RuleFor(x => x.Items)
                .Must(items => items.Select(i => i.ModuleId).Distinct().Count() == items.Count)
                .WithMessage("В списке есть повторяющиеся ModuleId.")
                .When(x => x.Items.Count > 0);

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.OrderIndex).GreaterThanOrEqualTo(0);
            });
        }
    }

}
