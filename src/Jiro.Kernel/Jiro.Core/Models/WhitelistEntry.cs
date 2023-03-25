using Jiro.Core.Abstraction;

namespace Jiro.Core.Models
{
    public class WhiteListEntry : DbModel<string>
    {
        public override string Id { get; set; } = default!;
        public DateTime AddedDate { get; set; }
        public string UserId { get; set; } = default!;
        public virtual AppUser User { get; set; } = default!;
    }
}