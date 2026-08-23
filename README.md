# Incident Assistant (v2 — RAG with Qdrant + Microsoft Agent Framework)

An AI-powered ops assistant that answers natural-language questions about
production incidents, now with real retrieval instead of just stuffing
everything into the prompt.

## What's new in v2
- **Qdrant** stores an embedding of each incident (via OpenAI's
  `text-embedding-3-small`), so retrieval scales past a handful of records.
- **Microsoft Agent Framework** wraps the chat model into an `AIAgent` with
  an incident-search tool. The agent decides for itself, per question,
  whether it needs to search before answering — this is the "agentic" part,
  vs. v1's fixed "always inject everything" approach.
- The `[VectorStoreKey]`/`[VectorStoreData]`/`[VectorStoreVector]`
  attributes (`Microsoft.Extensions.VectorData.Abstractions`) mean the
  `IncidentRecord` class isn't tied to Qdrant specifically — swapping to
  Azure AI Search or another store later is mostly a one-line change in
  `Program.cs`, not a rewrite.

## Prerequisites
1. .NET 8 SDK
2. Docker (to run Qdrant locally)
3. An OpenAI API key from [platform.openai.com](https://platform.openai.com/api-keys)

## Setup
```bash
# 1. Start Qdrant locally
docker compose up -d

# 2. Restore packages
dotnet restore

# 3. Set environment variables
export OPENAI_API_KEY="sk-..."
export OPENAI_CHAT_MODEL="gpt-4o-mini"          # optional, this is the default
export OPENAI_EMBEDDING_MODEL="text-embedding-3-small"  # optional, this is the default

# 4. Run it — this ingests incidents.json into Qdrant on startup, then
#    drops you into the Q&A loop
dotnet run
```

You can browse the Qdrant collection at `http://localhost:6333/dashboard`
while it's running — useful for actually *seeing* the vectors and payloads,
which is a good thing to be able to show in an interview.

⚠️ Keep your API key out of source control — the `export` above keeps it in
your shell session only.

## A note on package/API stability
This project uses two of the newest pieces of the .NET AI stack — the
Microsoft Agent Framework and `Microsoft.Extensions.VectorData` (including
its Qdrant connector, `Microsoft.SemanticKernel.Connectors.Qdrant`) — both
of which have been changing quickly through 2026 as they move toward GA.
The Qdrant connector in particular is still prerelease-only; if
`dotnet restore` complains about it, try:
```bash
dotnet add package Microsoft.SemanticKernel.Connectors.Qdrant --prerelease
```
The package names and version numbers here are a best effort as of when
this was written; if something doesn't resolve or compile on
`dotnet restore`, check the current samples at
[github.com/microsoft/agent-framework](https://github.com/microsoft/agent-framework)
— the overall pattern (chat client + tools → agent, vector store
abstractions → Qdrant connector) should still hold even if a method name
or package version has moved on. Debugging this kind of drift is itself
decent practice for working with a fast-moving ecosystem.

## Roadmap (matches the Saturday cloud/AI track)
- **v1:** simple "stuff it in the prompt" Q&A over a small JSON file.
- **v2 (this version):** real retrieval via Qdrant + an agentic tool-calling
  loop via Microsoft Agent Framework.
- **v3:** deploy to Azure (App Service or Container Apps) — Qdrant can run
  as a container alongside your app, or you can point at Qdrant Cloud.
  Wire up Application Insights for observability, and add a minimal Blazor
  front end so it's demoable in an interview instead of a console app.
- **v4 (optional stretch):** connect it to real log data instead of the
  sample JSON — e.g. ingest from your MicroserviceApp's logs.

## Notes for interview talking points
This project is deliberately designed to demonstrate three things at once:
.NET AI integration (`Microsoft.Extensions.AI`, later Semantic Kernel),
Azure hands-on deployment, and ops/reliability fluency (incident data,
severity, root cause) — the exact areas that came up in your recent
interview feedback.
