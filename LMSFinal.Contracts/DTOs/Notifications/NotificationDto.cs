using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Notifications
{
    public record NotificationDto(
        Guid Id,
        string Title,
        string Message,
        string Type,
        bool IsRead,
        DateTime CreatedAt);

}
