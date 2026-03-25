using BCFamilyLawBot.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace BCFamilyLawBot.Services;

public class RagChatService
{
    private readonly Kernel _kernel;
    private readonly DocumentIngestionService _ingestion;

    private const string SystemPrompt = """
        You are a helpful assistant specializing in British Columbia family law court forms.
        You help people understand which forms to use, how to fill them out, the filing
        workflow, deadlines, and how forms relate to each other.

        RULES:
        - ONLY answer based on the provided context documents below.
        - If the answer is not in the context, say "I don't have information about that
          in my knowledge base. Please check www2.gov.bc.ca for the latest forms and guides."
        - Always mention specific form numbers when relevant (e.g., Form F1, Form F4).
        - When explaining workflows, list the steps in order.
        - Be clear about which court level each form belongs to (Supreme Court vs Provincial Court).
        - Remind users you are an informational tool, not a substitute for a lawyer.
        - Use plain language. Avoid legalese when possible.
        - Format responses with clear structure. Use **bold** for form names and key terms.
        - Keep responses focused and concise — aim for 3-5 paragraphs max.
        """;

    public RagChatService(Kernel kernel, DocumentIngestionService ingestion)
    {
        _kernel = kernel;
        _ingestion = ingestion;
    }

    public async Task<(string Answer, List<SourceInfo> Sources)> AskAsync(
        string question, List<HistoryItem>? history)
    {
        // Step 1: Retrieve relevant chunks via semantic search
        var chunks = await _ingestion.SearchAsync(question, topK: 5);

        var context = string.Join("\n\n---\n\n",
            chunks.Select(c => $"[Source: {c.Source}]\n{c.Text}"));

        // Step 2: Build chat history from prior exchanges
        var chatHistory = new ChatHistory(SystemPrompt);

        if (history != null)
        {
            foreach (var item in history.TakeLast(12))
            {
                if (item.Role == "user")
                    chatHistory.AddUserMessage(item.Content);
                else
                    chatHistory.AddAssistantMessage(item.Content);
            }
        }

        // Step 3: Add augmented user message (retrieved context + question)
        chatHistory.AddUserMessage($"""
            CONTEXT DOCUMENTS:
            {context}

            USER QUESTION:
            {question}

            Answer based ONLY on the context documents above.
            Reference specific form numbers. If unsure, say so.
            """);

        // Step 4: Call LLM via Semantic Kernel
        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chatService.GetChatMessageContentAsync(chatHistory);
        var answer = response.Content ?? "Sorry, I couldn't generate a response.";

        // Step 5: Build source references for the UI
        var sources = chunks
            .Select(c => new SourceInfo(
                c.FormName,
                c.Text.Length > 120 ? c.Text[..120] + "..." : c.Text))
            .GroupBy(s => s.Name)
            .Select(g => g.First())
            .ToList();

        return (answer, sources);
    }
}
