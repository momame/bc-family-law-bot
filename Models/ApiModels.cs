namespace BCFamilyLawBot.Models;

public record ChatRequest(string Message, List<HistoryItem>? History);
public record HistoryItem(string Role, string Content);
public record ChatResponse(string Answer, List<SourceInfo> Sources);
public record SourceInfo(string Name, string Preview);
