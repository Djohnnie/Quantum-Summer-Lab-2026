# Quantum Summer Lab 2026 — Copilot instructions

Blazor Server app for the EUMaster4HPC Summer School "Quantum Summer Lab": teams
register, solve Microsoft Q# coding challenges, submit solutions that are verified
by running real Q# code, chat with an AI assistant ("Qubit Buddy"), and compete on
a leaderboard.

## Solution layout (.NET 10, `QuantumSummerLab.slnx`)

- `QuantumSummerLab.Web` — Blazor Server UI (MudBlazor), InteractiveServer render mode. The deployed app. Reaches the backend **only** through `IMediator`.
- `QuantumSummerLab.Application` — business logic as MediatR CQRS handlers. References Data.
- `QuantumSummerLab.Data` — EF Core (SQL Server) `QuantumSummerLabDbContext`, `Model/`, `Migrations/`.
- `QuantumSummerLab.Copilot` — "Qubit Buddy" AI agent over Azure OpenAI (`Microsoft.Agents.AI` / `Microsoft.Extensions.AI`).
- `QuantumSummerLab.Processor` — Azure Functions (isolated worker) HTTP endpoint that runs/verifies Q# solutions. References `QSharp.Community.QSharpBridge`.
- `QSharp.Community.QSharpBridge` — .NET wrapper over the native Rust `qsharp-bridge`. Prebuilt natives (`qsharp_bridge.dll`, `libqsharp_bridge.so`) are committed, so no Rust toolchain is needed to build.
- `QuantumSummerLab.Tools` — console app to migrate/clear the DB and seed challenges.

## Build / run

- Build the web app (matches CI): `dotnet build QuantumSummerLab.Web/QuantumSummerLab.Web.csproj -c Release`
- Run the web app: `dotnet run --project QuantumSummerLab.Web`
- Run the Functions app: `func start` in `QuantumSummerLab.Processor` (or run the project).
- There is **no test project** in this solution — do not assume `dotnet test` exists.

## CQRS conventions (the most important pattern)

Every use case is **one file** under `<Feature>/Commands/<Name>Command.cs` or
`<Feature>/Queries/<Name>Query.cs`, containing all of:

- the request (`...Command` / `...Query : IRequest<...Response>`),
- the `...Response` (and any nested DTOs),
- the handler (`...CommandHandler` / `...QueryHandler : IRequestHandler<TRequest, TResponse>`).

Do not split these into separate files. Handlers are auto-registered by MediatR
scanning the Application assembly (`AddApplicationServices`).

**DbContext access inside handlers** is deliberate and consistent: inject
`IServiceScopeFactory`, never the `DbContext` directly, then resolve a scoped
context:

```csharp
using var dbContext = _scopeFactory.CreateScope().ServiceProvider
    .GetRequiredService<QuantumSummerLabDbContext>();
```

## Dependency injection

Each project exposes an `AddXxxServices(...)` extension under `_di/`; compose them in
`Program.cs`. `AddApplicationServices` wires MediatR, `IPasswordHashHelper`,
`AddHttpClient()`, and `AddDataServices()`.

## Configuration (runtime keys, NOT in appsettings.json)

Read via `IConfiguration` from environment / App Service settings:
`CONNECTION_STRING`, `AZUREOPENAI_ENDPOINT`, `AZUREOPENAI_KEY`,
`AZUREOPENAI_DEPLOYMENT`, `QSHARP_HELPER_BASE_ADDRESS` (Functions base URL),
`MEDIATR_LICENSEKEY`.

## Data model & migrations

- Entities live in `QuantumSummerLab.Data/Model/` (`Challenge`, `Team`, `Score`, `Chat`).
- Convention: `Guid Id` is a **non-clustered** PK; `int SysId` is an identity with the **clustered** index; tables are UPPERCASE (`TEAMS`, `CHALLENGES`, `SCORES`, `CHATS`).
- Migrations are applied automatically at Web startup (`app.Services.MigrateDatabase()`) and from the Tools console app.
- `dotnet-ef` (9.0.7) is pinned as a local tool in `QuantumSummerLab.Web/.config/dotnet-tools.json`. Add migrations with `dotnet ef migrations add <Name> --project QuantumSummerLab.Data --startup-project QuantumSummerLab.Web`.

## Auth (team-based, no ASP.NET Identity)

- Passwords are hashed with BCrypt via `IPasswordHashHelper`.
- Login/Register return an `AuthenticationToken { TeamId, TeamName, IsAdmin }` stored client-side in `ProtectedLocalStorage` under key `"authToken"`.
- Admin/management commands and queries take a `RequestingTeamId` and re-verify **server-side** that the team is `IsAdmin && IsApproved && !IsArchived`; UI additionally gates on `IsAdmin` from the token. See `GetTeamManagementOverviewQuery`, `ArchiveTeamCommand`, `ApproveTeamCommand`, `SetTeamAdminStatusCommand`.

## HTTP calls

Outbound HTTP from handlers must use injected `IHttpClientFactory.CreateClient()` —
never `new HttpClient()`.

## Q# challenge verification flow

1. Web `VerifyChallengeSolutionCommand` loads the `Challenge`, builds a `QSharpRequest`, and POSTs to the Functions endpoint `api/QSharpVerificationFunction` at `QSHARP_HELPER_BASE_ADDRESS`.
2. The Processor runs the Q# via `QSharpBridge` and returns `QSharpFeedback`, which is persisted as a `Score`.
3. Challenge code fields (`VerificationTemplate`, `SolutionTemplate`, `Solution`, `ExpectedOutput`, `ExpectedStates`) are **Base64-encoded** in transit/storage — use `ToBase64String()` / `FromBase64String()` (`Base64Extensions`). The verification template contains a `<<SOLVE>>` placeholder where the team's solution is injected.

## Challenges

- `QuantumSummerLab.Tools/Challenges.cs` is **auto-generated** — do not hand-edit it. It's seeded into the DB by running the Tools console app (option 3 "Add challenges"). Names are `0, A1..A3, B1..B3, C1..C3, D1..D3` (`Level` 0–4).
- `_challenges/<id>/` is the source of truth: a standalone, runnable Q# project (`qsharp.json` + `src/Main.qs`). Each `Main.qs` has a `CHALLENGE METADATA` comment block at the top (Title, Description, Tldr, etc.) and marks the reference solution with `// ===SOLVE-START===` / `// ===SOLVE-END===`. `SolutionTemplate` and `VerificationTemplate` are derived from the code, not stored.
- After editing anything under `_challenges/`, run `python _challenges/generate_challenges.py` to regenerate `Challenges.cs` (`--check` verifies without writing).

## Qubit Buddy (AI assistant)

- Agent name/instructions are built in `CopilotHelper.BuildInstructions`; callable tools are defined in `CopilotFunctions` via `AIFunctionFactory.Create(...)`.
- The agent must **never output markdown**: responses use `[BR]` for line breaks and `●` for bullets (the UI renders plain text). Preserve this when editing instructions or adding tools.
- Chat history auto-summarizes ("reduce") once a conversation exceeds 10 active messages.

## Web UI

- MudBlazor components; pages reach the backend only via `IMediator`.
- Scoped `NavigationHelper` / `DrawerHelper` are event-based state holders used to trigger cross-component re-renders (`.Update()` raises an event that pages subscribe to).
- Browser downloads use JS interop `window.downloadFileFromStream` (`wwwroot/js/download.js`, included in `App.razor`) over a `DotNetStreamReference`.

## Deployment

GitHub Actions `.github/workflows/deploy-web.yml` builds and publishes **only** the
Web app to Azure App Service `quantumsummerlab2025` on push to `main` (path-filtered
to Web/Application/Copilot/Data). The Functions Processor is deployed separately.
