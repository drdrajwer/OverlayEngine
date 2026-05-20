namespace OverlayEngine.Core.Models;

public sealed class NoteItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; } = false;
}
