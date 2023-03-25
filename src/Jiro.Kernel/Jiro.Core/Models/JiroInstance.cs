using Jiro.Core.Abstraction;

namespace Jiro.Core.Models
{
    public class JiroInstance : DbModel<int>
    {
        public bool IsConfigured { get; set; }
    }
}