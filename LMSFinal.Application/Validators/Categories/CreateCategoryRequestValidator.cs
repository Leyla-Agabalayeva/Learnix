using FluentValidation;
using LMSFinal.Contracts.DTOs.Categories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Categories
{
    public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
    {
        public CreateCategoryRequestValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug обязателен.")
                .MaximumLength(150)
                .Matches("^[a-z0-9-]+$").WithMessage("Slug может содержать только строчные латинские буквы, цифры и дефис.");

            RuleFor(x => x.Translations)
                .NotEmpty().WithMessage("Нужен хотя бы один перевод категории.");

            RuleForEach(x => x.Translations).ChildRules(translation =>
            {
                translation.RuleFor(t => t.Name)
                    .NotEmpty().WithMessage("Название категории обязательно.")
                    .MaximumLength(150);
            });
        }
    }

}
