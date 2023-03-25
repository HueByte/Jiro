using Jiro.Core.Abstraction;

namespace Jiro.Core.Models
{
    public class JiroInstance : DbModel<int>
    {
        public string InstanceName { get; set; } = default!;
        public bool IsConfigured { get; set; }
        public bool IsActive { get; set; }
    }
}