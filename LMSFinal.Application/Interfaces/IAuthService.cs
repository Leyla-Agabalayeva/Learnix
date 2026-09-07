using LMSFinal.Contracts.DTOs.Auth;
using LMSFinal.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<Result<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result<CurrentUserResponse>> UpdateProfileAsync(
            Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);


        Task<Result<CurrentUserResponse>> UpdateAvatarAsync(
            Guid userId, string avatarUrl, CancellationToken cancellationToken = default);


        Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);


        Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    }

}
