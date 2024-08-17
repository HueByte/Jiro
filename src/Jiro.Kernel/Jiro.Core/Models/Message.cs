using System.ComponentModel.DataAnnotations.Schema;
using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

public class Message : DbModel<string>
{
    public string Role { get; set; } = default!;
    public string Content { get; set; } = default!;

    [ForeignKey("ChatSessionId")]
    public string ChatSessionId { get; set; } = default!;
    public ChatSession ChatSession { get; set; } = default!;
}