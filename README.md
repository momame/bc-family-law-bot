# ⚖️ BC Family Law Forms Assistant

A **RAG-powered chatbot** that helps users understand British Columbia family law court forms, filing workflows, deadlines, and form relationships.

Built with **.NET 8 + Semantic Kernel + GitHub Models (free tier)** — no Azure subscription required.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Semantic Kernel](https://img.shields.io/badge/Semantic_Kernel-1.32-blue)
![GitHub Models](https://img.shields.io/badge/GitHub_Models-Free-green)
![License](https://img.shields.io/badge/License-MIT-yellow)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    React Chat UI (SPA)                      │
│              Dark theme · Real-time responses               │
└──────────────────────┬──────────────────────────────────────┘
                       │ POST /api/chat
┌──────────────────────▼──────────────────────────────────────┐
│                  .NET 8 Web API                             │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐ │
│  │  Semantic     │  │  RAG Chat    │  │  Document         │ │
│  │  Kernel       │──│  Service     │──│  Ingestion        │ │
│  │  (Orchestr.)  │  │  (Pipeline)  │  │  Service          │ │
│  └──────┬───────┘  └──────────────┘  └─────────┬─────────┘ │
│         │                                       │           │
│  ┌──────▼───────┐                    ┌──────────▼────────┐  │
│  │  GitHub       │                    │  In-Memory        │  │
│  │  Models API   │                    │  Vector Store     │  │
│  │  (GPT-4o-mini)│                    │  (SK InMemory)    │  │
│  └──────────────┘                    └───────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### How RAG Works Here

1. **Startup**: Markdown files in `Data/forms/` are chunked, embedded via GitHub Models' `text-embedding-3-small`, and stored in an in-memory vector database
2. **User asks a question**: The question is embedded using the same model
3. **Semantic search**: The 5 most relevant document chunks are retrieved by cosine similarity
4. **Augmented prompt**: Retrieved chunks + the question are sent to GPT-4o-mini via Semantic Kernel
5. **Grounded answer**: The LLM answers based ONLY on the retrieved documents — no hallucination

### Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| Frontend | React 18 (CDN) | Chat UI with dark theme |
| Backend | .NET 8 Minimal API | REST endpoints |
| Orchestration | Microsoft Semantic Kernel | RAG pipeline, LLM integration |
| LLM | GPT-4o-mini via GitHub Models | Chat completion |
| Embeddings | text-embedding-3-small via GitHub Models | Document & query vectorization |
| Vector Store | Semantic Kernel InMemory | Cosine similarity search |
| Knowledge Base | Markdown files | BC family law forms documentation |

---

## 🚀 Quick Start (Windows)

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- A [GitHub account](https://github.com) (free)
- A GitHub Personal Access Token (PAT)

### Step 1: Get a GitHub PAT

1. Go to [github.com/settings/tokens](https://github.com/settings/tokens)
2. Click **"Generate new token (classic)"**
3. Give it a name like `bc-law-bot`
4. **No scopes needed** — leave all checkboxes unchecked (GitHub Models uses the default `models:read` permission)
5. Click **Generate token**
6. **Copy the token** — you'll need it in Step 3

### Step 2: Clone and Navigate

```bash
git clone https://github.com/YOUR_USERNAME/bc-family-law-bot.git
cd bc-family-law-bot
```

### Step 3: Configure Your Token

Open `appsettings.json` and replace `YOUR_GITHUB_PAT_HERE`:

```json
{
  "GitHubModels": {
    "Token": "ghp_your_actual_token_here"
  }
}
```

**OR** set it as an environment variable (recommended for security):

```powershell
$env:GITHUB_TOKEN = "ghp_your_actual_token_here"
```

### Step 4: Run

```bash
dotnet run
```

You'll see:
```
Found 5 markdown files to ingest.
Created 28 chunks. Generating embeddings...
Ingested 28 chunks into vector store.
=== Documents ingested and ready ===
BC Family Law Bot running at http://localhost:5000
```

### Step 5: Open

Navigate to **http://localhost:5000** in your browser.

---

## 📁 Project Structure

```
BCFamilyLawBot/
├── Data/forms/                    # Knowledge base (markdown files)
│   ├── 01-starting-a-case.md      # F1, F3, F4, F5 forms
│   ├── 02-applications-responses.md # F6, F8, F9, F10 forms
│   ├── 03-financial-forms.md      # F30, F31, F20 forms
│   ├── 04-consent-divorce-trial.md # F52, F35, divorce process
│   └── 05-provincial-court.md     # Provincial Court forms, comparison
├── Models/
│   └── DocumentChunk.cs           # Vector store record model
├── Services/
│   ├── DocumentIngestionService.cs # Chunking, embedding, vector store
│   └── RagChatService.cs          # RAG pipeline orchestration
├── wwwroot/
│   └── index.html                 # React chat UI (single-file SPA)
├── Program.cs                     # API setup, DI, startup ingestion
├── appsettings.json               # Configuration
└── BCFamilyLawBot.csproj          # .NET project file
```

---

## 💡 Example Questions

- "What form do I need to start a family law case in BC Supreme Court?"
- "How do I reply to a family claim? What forms do I need?"
- "What is Form F8 and when do I need it?"
- "What financial forms do I need for a divorce with children?"
- "Walk me through the consent order workflow"
- "What's the difference between Provincial and Supreme Court forms?"
- "Can I transfer my case from Provincial Court to Supreme Court?"
- "What happens if I don't file Form F4 in time?"
- "What documents do I need to attach to Form F30?"

---

## 🔧 Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `GitHubModels:Token` | (required) | Your GitHub PAT |
| `GitHubModels:ChatModel` | `gpt-4o-mini` | LLM for chat completion |
| `GitHubModels:EmbeddingModel` | `text-embedding-3-small` | Model for text embeddings |
| `GitHubModels:Endpoint` | `https://models.inference.ai.azure.com` | GitHub Models API endpoint |
| `Urls` | `http://localhost:5000` | Server URL |

---

## 🔄 Extending the Knowledge Base

To add more forms or update information:

1. Add or edit `.md` files in `Data/forms/`
2. Use clear headings and structured content
3. Include form numbers, purposes, when to use, filing requirements, and workflows
4. Restart the application — documents are re-ingested on startup

---

## 🏗️ Production Upgrade Path

This project uses **GitHub Models (free)** for development. To move to production Azure:

| Component | Current (Free) | Production (Azure) |
|-----------|---------------|-------------------|
| LLM | GitHub Models GPT-4o-mini | Azure OpenAI GPT-4o |
| Embeddings | GitHub Models text-embedding-3-small | Azure OpenAI embeddings |
| Vector Store | In-Memory (SK) | Azure AI Search |
| Auth | None | Azure AD |
| Hosting | localhost | Azure App Service |

The architecture is identical — only the endpoint configuration changes.

---

## ⚠️ Disclaimer

This tool provides **general information** about BC family law forms. It is **not legal advice**. For specific legal matters, consult a qualified lawyer or visit the [BC Government Family Forms](https://www2.gov.bc.ca/gov/content/justice/courthouse-services/documents-forms-records/court-forms/prov-family-forms) page.

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

## 👨‍💻 Author

**Matt Mehrpak** — Senior Full Stack Developer
- 8+ years .NET/C#, Angular/TypeScript, Azure Cloud
- Microsoft Certified: AZ-104 (Azure Administrator), AI-102 (AI Engineer)
- [LinkedIn](https://linkedin.com/in/mehr)
