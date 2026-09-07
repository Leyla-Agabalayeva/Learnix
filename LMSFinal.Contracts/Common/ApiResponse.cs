using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.Common
{
    public class ApiResponse
    {
        public bool Success { get; init; }

        public string? Message { get; init; }

       
        public IDictionary<string, string[]>? Errors { get; init; }

        public static ApiResponse Fail(string message, IDictionary<string, string[]>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; init; }

        public static ApiResponse<T> Ok(T data, string? message = null) =>
            new() { Success = true, Message = message, Data = data };
    }

}
