using FluentValidation;
using LMSFinal.Contracts.DTOs.Reviews;

namespace LMSFinal.Application.Validators.Reviews
{
    public class ReplyToReviewRequestValidator : AbstractValidator<ReplyToReviewRequest>
    {
        public ReplyToReviewRequestValidator()
        {
            RuleFor(x => x.Reply)
                .NotEmpty().WithMessage("Ответ не может быть пустым.")
                .MaximumLength(2000);
        }
    }
}
