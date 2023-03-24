namespace Jiro.Core.Models
{
    public class WhiteListEntry
    {
        public string Id { get; set; } = default!;
        public DateTime AddedDate { get; set; }
        public string UserId { get; set; } = default!;
        public virtual AppUser User { get; set; } = default!;
    }
}