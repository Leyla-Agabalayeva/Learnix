using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Auth
{
   
    public record CurrentUserResponse(
     Guid UserId,
     string Email,
     string FirstName,
     string LastName,
     IReadOnlyList<string> Roles,
     string? Bio,
     string? AvatarUrl,
     DateTime CreatedAt);
}
