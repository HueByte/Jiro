namespace Jiro.Core.DTO;

public class ChangePasswordDTO
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}