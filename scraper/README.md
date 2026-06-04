# CSE Analyser — Scraper

.NET 10 console app that runs the nightly price pipeline against the CSE API and writes results to PostgreSQL via Npgsql.

## Git hooks

A pre-commit hook enforces formatting on both the scraper (dotnet format) and the frontend (prettier + eslint). Run this once after cloning:

```bash
git config core.hooksPath .githooks
```

Each check only runs when files in its area are staged, so a scraper-only commit won't trigger the frontend check and vice versa.

If a scraper commit is rejected, fix formatting and retry:

```bash
cd scraper && dotnet format
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Local Supabase running (`npx supabase start` from `frontend/`)

## Local setup

### 1. Create your local connection string file

Copy the template and fill in your connection string. This file is git-ignored — never commit it.

```
scraper/CseAnalyser.Scraper/appsettings.Development.json
```

```json
{
  "ConnectionStrings": {
    "Scraper": "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres"
  }
}
```

Find your local Supabase connection details with:

```bash
cd frontend && npx supabase status
```

### 2. Run the scraper

```bash
cd scraper
dotnet run --project CseAnalyser.Scraper
```

Or open the solution in Visual Studio and run `CseAnalyser.Scraper`.

## Running tests

### Unit tests only

```bash
cd scraper
dotnet test --filter "Category!=Integration"
```

### Integration tests

Integration tests hit a real PostgreSQL database. They use a separate `scraper_test` database to avoid touching your real data.

**One-time setup — create the `scraper_test` database:**

```sql
-- Connect to your local Supabase PostgreSQL (port 54322) and run:
CREATE DATABASE scraper_test;
```

Then apply all migrations to it:

```bash
cd frontend
npx supabase migration up --db-url "postgresql://postgres:postgres@127.0.0.1:54322/scraper_test"
```

**Create your test connection string file** (git-ignored — never commit it):

```
scraper/CseAnalyser.Scraper.Tests/appsettings.Test.json
```

```json
{
  "ConnectionStrings": {
    "ScraperTest": "Host=127.0.0.1;Port=54322;Database=scraper_test;Username=postgres;Password=postgres"
  }
}
```

**Run all tests including integration:**

```bash
cd scraper
dotnet test
```

### All checks (format + tests)

```bash
cd scraper
dotnet format --verify-no-changes && dotnet test
```

## Logging

The scraper uses `Microsoft.Extensions.Logging` with the Console provider. The default level is `Information` — phase milestones and errors are always visible; per-stock success lines are at `Debug`.

### Enable verbose output

Set the environment variable before running:

```bash
# Windows PowerShell
$env:LOGGING__LOGLEVEL__DEFAULT = "Debug"
dotnet run --project CseAnalyser.Scraper

# bash
LOGGING__LOGLEVEL__DEFAULT=Debug dotnet run --project CseAnalyser.Scraper
```

### Enable logging in integration tests

Integration tests use `NullLogger.Instance` by default — no output. If you need to see log output while debugging a failing test, swap `NullLogger.Instance` for a real logger in the test constructor:

```csharp
using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddConsole());
_phase = new TechnicalIndicatorsPhase(_db, loggerFactory.CreateLogger("TechnicalIndicators"));
```

Remember to revert before committing — `NullLogger.Instance` is the right default for CI.
