using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Infrastructure.AI
{
    public class DeepSeekSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "deepseek-chat";
    }
}
