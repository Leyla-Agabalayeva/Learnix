using LMSFinal.Contracts.DTOs.Auth;
using LMSFinal.Domain.Common;
using LMSFinal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{

    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IEnumerable<string> roles);
    }

}
