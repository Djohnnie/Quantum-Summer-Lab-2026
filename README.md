# Quantum-Summer-Lab-2026

EUMaster4HPC Summer School 2026 - Quantum Summer Lab

## Overview

An interactive learning platform for the EUMaster4HPC Summer School. Teams register,
work through a series of Microsoft Q# quantum-computing challenges, and submit their
solutions. Each submission is verified by **running the team's Q# code against a real
simulator**, scored, and ranked on a live leaderboard. An AI assistant — *Qubit Buddy* —
is on hand to give hints (never full solutions) along the way.

## What it does

- **Teams & accounts** — teams register, log in (BCrypt-hashed passwords), and admins approve/manage them.
- **Challenges** — a graded set of Q# challenges (`0`, `A1–A3`, `B1–B3`, `C1–C3`, `D1–D3`).
- **Solution verification** — submitted Q# is injected into a verification template and executed via a native Q# bridge in an Azure Functions endpoint.
- **Qubit Buddy** — an Azure OpenAI assistant that helps with quantum concepts and Q#, giving incremental hints only.
- **Leaderboard & reports** — scores, completion times, and per-team statistics.

## Project structure

| Project | Purpose |
| --- | --- |
| `QuantumSummerLab.Web` | Blazor Server UI (MudBlazor). The deployed app. |
| `QuantumSummerLab.Application` | Business logic as MediatR (CQRS) commands & queries. |
| `QuantumSummerLab.Data` | EF Core (SQL Server) DbContext, models, and migrations. |
| `QuantumSummerLab.Copilot` | The *Qubit Buddy* AI agent over Azure OpenAI. |
| `QuantumSummerLab.Processor` | Azure Functions endpoint that runs and verifies Q# solutions. |
| `QSharp.Community.QSharpBridge` | .NET wrapper over the native Rust `qsharp-bridge` simulator. |
| `QuantumSummerLab.Tools` | Console tool to migrate/clear the database and seed challenges. |

## Tech stack

.NET 10 · Blazor Server · MudBlazor · MediatR · Entity Framework Core (SQL Server) ·
Azure OpenAI · Azure Functions (isolated worker) · Microsoft Q#.

## Getting started

The web app is the entry point and applies database migrations on startup:

```bash
dotnet run --project QuantumSummerLab.Web
```

Runtime configuration (connection string, Azure OpenAI, and the Q# verification
endpoint) is supplied via environment variables / App Service settings — see
[`.github/copilot-instructions.md`](.github/copilot-instructions.md) for the full list
of keys and architecture details.
