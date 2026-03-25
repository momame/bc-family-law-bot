using BCFamilyLawBot.Models;
using BCFamilyLawBot.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// CORS for React dev server
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Read config
var githubToken = builder.Configuration["GitHubModels:Token"]
    ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
    ?? throw new InvalidOperationException(
        "Set GitHubModels:Token in appsettings.json or GITHUB_TOKEN env var.");

var chatModel = builder.Configuration["GitHubModels:ChatModel"] ?? "gpt-4o-mini";
var embeddingModel = builder.Configuration["GitHubModels:EmbeddingModel"] ?? "text-embedding-3-small";
var endpoint = builder.Configuration["GitHubModels:Endpoint"]
    ?? "https://models.inference.ai.azure.com";

// Semantic Kernel — chat via GitHub Models (OpenAI-compatible)
builder.Services.AddSingleton<Kernel>(sp =>
{
    var kb = Kernel.CreateBuilder();
    kb.AddOpenAIChatCompletion(
        modelId: chatModel,
        apiKey: githubToken,
        endpoint: new Uri(endpoint));
    return kb.Build();
});

// Document ingestion (singleton — lives for app lifetime)
builder.Services.AddSingleton<DocumentIngestionService>(sp =>
    new DocumentIngestionService(
        githubToken, embeddingModel, endpoint,
        Path.Combine(builder.Environment.ContentRootPath, "Data", "forms")));

// Chat service (transient — stateless, history comes from frontend)
builder.Services.AddTransient<RagChatService>();

var app = builder.Build();

// Ingest documents on startup
using (var scope = app.Services.CreateScope())
{
    var ingestion = scope.ServiceProvider.GetRequiredService<DocumentIngestionService>();
    await ingestion.IngestAsync();
    Console.WriteLine("=== Documents ingested and ready ===");
}

app.UseCors();

// Serve React SPA from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Chat endpoint
app.MapPost("/api/chat", async (ChatRequest req, RagChatService chat) =>
{
    try
    {
        var (answer, sources) = await chat.AskAsync(req.Message, req.History);
        return Results.Ok(new ChatResponse(answer, sources));
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
});

// Health check
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

// SPA fallback — unmatched routes serve index.html
app.MapFallbackToFile("index.html");

Console.WriteLine("BC Family Law Bot running at http://localhost:5000");
app.Run();
