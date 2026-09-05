# Incident Assistant (v2 — RAG with Qdrant)

An AI-powered ops assistant that answers natural-language questions about
production incidents, now with real retrieval instead of just stuffing
everything into the prompt.

## What's new in v2
- **Qdrant** stores an embedding of each incident (via OpenAI's
  `text-embedding-3-small`), so retrieval scales past a handful of records.
- **Agentic tool-calling** via `Microsoft.Extensions.AI`'s built-in
  `.UseFunctionInvocation()` — the model decides for itself, per question,
  whether it needs to search Qdrant before answering. (This intentionally
  does *not* use the Microsoft Agent Framework — that package is still
  pre-GA and its API has been shifting release to release, so this sticks
  to the more stable, well-documented part of the stack to maximize the
  odds this actually builds cleanly for you. Once Agent Framework settles
  down, swapping it in later is a reasonable v2.5 step — see the note at
  the bottom.)
- The `[VectorStoreKey]`/`[VectorStoreData]`/`[VectorStoreVector]`
  attributes (`Microsoft.Extensions.VectorData.Abstractions`) mean the
  `IncidentRecord` class isn't tied to Qdrant specifically — swapping to
  Azure AI Search or another store later is mostly a one-line change in
  `Program.cs`, not a rewrite.

## Prerequisites
1. .NET 10 SDK (`dotnet --version` should show `10.x`)
2. Docker (to run Qdrant locally)
3. An OpenAI API key from [platform.openai.com](https://platform.openai.com/api-keys)

## Setup
```bash
# 1. Start Qdrant locally
docker compose up -d

# 2. Restore packages (the Qdrant connector is prerelease-only right now)
dotnet restore
# if that fails on the Qdrant connector specifically:
dotnet add package Microsoft.SemanticKernel.Connectors.Qdrant --prerelease

# 3. Set environment variables
export OPENAI_API_KEY="sk-..."
export OPENAI_CHAT_MODEL="gpt-4o-mini"          # optional, this is the default
export OPENAI_EMBEDDING_MODEL="text-embedding-3-small"  # optional, this is the default

# 4. Build, then run — running ingests incidents.json into Qdrant on
#    startup, then drops you into the Q&A loop
dotnet build
dotnet run
```

You can browse the Qdrant collection at `http://localhost:6333/dashboard`
while it's running — useful for actually *seeing* the vectors and payloads,
which is a good thing to be able to show in an interview.

⚠️ Keep your API key out of source control — the `export` above keeps it in
your shell session only.

## A note on package/API stability
`Microsoft.Extensions.VectorData` (and its Qdrant connector,
`Microsoft.SemanticKernel.Connectors.Qdrant`) has been changing quickly
through 2026 as it moves toward GA, and the Qdrant connector specifically
is still prerelease-only as of now. If `dotnet restore` or `dotnet build`
fails on either package, check current versions at
[nuget.org](https://www.nuget.org/packages/Microsoft.SemanticKernel.Connectors.Qdrant)
and bump the version in the `.csproj` accordingly — the underlying pattern
(vector store abstractions → Qdrant connector) should hold even if a
specific method name has moved on.

I couldn't actually compile this myself before handing it to you (no .NET
SDK/NuGet access in the environment I write code in) — treat this as a
strong, carefully-considered starting point rather than a guaranteed clean
build. If you hit an error that isn't covered above, share the exact
message and I can help debug it.

## Adding Microsoft Agent Framework back in (optional, once it's more stable)
The current version gets the same agentic behavior without it. If you
specifically want Agent Framework on your résumé for this project later,
the swap is: replace `.UseFunctionInvocation()` + the manual conversation
loop with `chatClient.CreateAIAgent(instructions, tools)` and
`agent.RunAsync(...)` — check current samples at
[github.com/microsoft/agent-framework](https://github.com/microsoft/agent-framework)
for the exact API shape at that time, since it's likely to have moved.

## Roadmap (matches the Saturday cloud/AI track)
- **v1:** simple "stuff it in the prompt" Q&A over a small JSON file.
- **v2 (this version):** real retrieval via Qdrant + agentic tool-calling.
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
