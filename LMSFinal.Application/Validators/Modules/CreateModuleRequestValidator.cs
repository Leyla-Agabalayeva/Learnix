using FluentValidation;
using LMSFinal.Contracts.DTOs.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Modules
{
    public class CreateModuleRequestValidator : AbstractValidator<CreateModuleRequest>
    {
        public CreateModuleRequestValidator()
        {
            RuleFor(x => x.Translations)
                .NotEmpty().WithMessage("Нужен хотя бы один перевод модуля.");

            RuleForEach(x => x.Translations).ChildRules(t =>
            {
                t.RuleFor(x => x.Title)
                    .NotEmpty().WithMessage("Название модуля обязательно.")
                    .MaximumLength(300);
            });
        }
    }

}
