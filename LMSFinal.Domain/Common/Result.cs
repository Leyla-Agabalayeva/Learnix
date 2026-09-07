using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Domain.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string? Error { get; }

        protected Result(bool isSuccess, string? error)
        {
            if (isSuccess && error is not null)
                throw new InvalidOperationException("Успешный результат не может содержать ошибку.");

            if (!isSuccess && error is null)
                throw new InvalidOperationException("Неуспешный результат должен содержать ошибку.");

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);

        public static Result Failure(string error) => new(false, error);
    }

}
