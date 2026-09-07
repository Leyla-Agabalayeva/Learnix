using FluentValidation;
using LMSFinal.Contracts.DTOs.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Reviews
{
    public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
    {
        public UpdateReviewRequestValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Оценка должна быть от 1 до 5.");

            RuleFor(x => x.Comment)
                .MaximumLength(2000);
        }
    }
}
