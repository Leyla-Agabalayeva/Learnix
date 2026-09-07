using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Contracts.DTOs.Modules
{

    public record ReorderModulesRequest
    {
        public IReadOnlyList<ModuleOrderItem> Items { get; init; } = Array.Empty<ModuleOrderItem>();
    }
}
