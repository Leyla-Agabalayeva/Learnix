using FluentValidation;
using LMSFinal.Contracts.DTOs.Quizzes;
using LMSFinal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Validators.Quizzes
{
    public class CreateQuizRequestValidator : AbstractValidator<CreateQuizRequest>
    {
        public CreateQuizRequestValidator()
        {
            RuleFor(x => x.LessonId)
                .NotEmpty().WithMessage("LessonId обязателен.");

            RuleFor(x => x.Translations)
                .NotEmpty().WithMessage("Нужен хотя бы один перевод квиза.");

            RuleForEach(x => x.Translations).ChildRules(translation =>
            {
                translation.RuleFor(t => t.Title)
                    .NotEmpty().WithMessage("Название квиза обязательно.")
                    .MaximumLength(300);
            });

            RuleFor(x => x.PassingScore)
                .InclusiveBetween(0, 100).WithMessage("Проходной балл должен быть от 0 до 100.");

            RuleFor(x => x.TimeLimitMinutes)
                .GreaterThan(0).WithMessage("Лимит времени должен быть больше 0.")
                .When(x => x.TimeLimitMinutes.HasValue);

            RuleFor(x => x.Questions)
                .NotEmpty().WithMessage("В квизе должен быть хотя бы один вопрос.");

            RuleForEach(x => x.Questions).ChildRules(question =>
            {
                question.RuleFor(q => q.Translations)
                    .NotEmpty().WithMessage("Нужен хотя бы один перевод вопроса.");

                question.RuleForEach(q => q.Translations).ChildRules(translation =>
                {
                    translation.RuleFor(t => t.QuestionText)
                        .NotEmpty().WithMessage("Текст вопроса обязателен.");
                });

                question.RuleFor(q => q.Points)
                    .GreaterThan(0).WithMessage("Баллы за вопрос должны быть больше 0.");

                question.RuleFor(q => q.Answers)
                    .Must(answers => answers.Count >= 2)
                    .WithMessage("У вопроса должно быть минимум 2 варианта ответа.");

                question.RuleForEach(q => q.Answers).ChildRules(answer =>
                {
                    answer.RuleFor(a => a.Translations)
                        .NotEmpty().WithMessage("Нужен хотя бы один перевод варианта ответа.");

                    answer.RuleForEach(a => a.Translations).ChildRules(translation =>
                    {
                        translation.RuleFor(t => t.AnswerText)
                            .NotEmpty().WithMessage("Текст варианта ответа обязателен.");
                    });
                });

                question.RuleFor(q => q)
                    .Must(HaveValidCorrectAnswers)
                    .WithMessage("Single Choice требует ровно один правильный ответ, Multiple Choice — хотя бы один.");
            });
        }



        private static bool HaveValidCorrectAnswers(QuestionInput question)
        {
            var correctCount = question.Answers.Count(a => a.IsCorrect);

            return question.QuestionType switch
            {
                QuestionType.SingleChoice => correctCount == 1,
                QuestionType.MultipleChoice => correctCount >= 1,
                _ => false
            };
        }
    }

}
