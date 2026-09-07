using FluentValidation;
using LMSFinal.Contracts.DTOs.Quizzes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Quizzes
{

    public class SubmitQuizRequestValidator : AbstractValidator<SubmitQuizRequest>
    {
        public SubmitQuizRequestValidator()
        {
            // Пустой Answers допустим (студент мог не ответить ни на один вопрос — просто 0 баллов),
            // но дубли QuestionId — явная ошибка клиента, отсекаем на входе.
            RuleFor(x => x.Answers)
                .Must(answers => answers.Select(a => a.QuestionId).Distinct().Count() == answers.Count)
                .WithMessage("В ответе есть дублирующиеся QuestionId.")
                .When(x => x.Answers.Count > 0);
        }
    }

}
