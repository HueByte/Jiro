namespace Jiro.Core.Base.Results;

public class ImageResult : ICommandResult
{
    public string? Note { get; set; }
    public string? ImageUrl { get; set; }
    public string? Image { get; set; }

    public ImageResult(string? image = null, string? imageUrl = null, string? note = null)
    {
        Image = image;
        ImageUrl = imageUrl;
        Note = note;
    }

    public static ImageResult Create(string? image = null, string? imageUrl = null, string? note = null)
    {
        return new ImageResult(image, imageUrl, note);
    }
}