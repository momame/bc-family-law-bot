namespace BCFamilyLawBot.Models;

public class DocumentChunk
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
}