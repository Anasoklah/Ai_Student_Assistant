# AGENTS.md

This file provides high-level context for OpenCode sessions to avoid common mistakes and ramp up quickly.

## Architecture
- **Backend**: C# .NET 10.0, located in `src/backend/SyrianStudyBot/`.
- **AI Service**: Python, located in `src/ai-service/`.
- **Database**: PostgreSQL with `pgvector` (via Docker).

## Key Commands
- **Database**: `docker-compose up -d` to start the PostgreSQL service.
- **Backend**: Standard .NET commands (`dotnet build`, `dotnet run`) work from `src/backend/SyrianStudyBot/`.

## Operational Notes
- Ensure Docker is running before working on features dependent on the database.
- Backend project dependencies are managed via NuGet (`SyrianStudyBot.csproj`).
