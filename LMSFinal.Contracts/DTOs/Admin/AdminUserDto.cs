using System;
using System.Collections.Generic;

namespace LMSFinal.Contracts.DTOs.Admin
{
    public record AdminUserDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyList<string> Roles,
        bool IsLocked,
        DateTime CreatedAt,
        string? AvatarUrl);
}
