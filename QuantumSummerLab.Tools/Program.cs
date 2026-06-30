
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;
using QuantumSummerLab.Data.Model;
using QuantumSummerLab.Tools;
using Spectre.Console;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddApplicationServices(configuration);
serviceCollection.AddSingleton<IConfiguration>(configuration);
using var serviceProvider = serviceCollection.BuildServiceProvider();

while (true)
{
    if (!Console.IsOutputRedirected)
    {
        AnsiConsole.Clear();
    }

    RenderHeader(configuration);

    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to [aqua]do[/]?")
            .PageSize(6)
            .HighlightStyle(new Style(foreground: Color.Aqua))
            .AddChoices(
                MenuOptions.Migrate,
                MenuOptions.Clear,
                MenuOptions.AddChallenges,
                MenuOptions.Exit));

    if (choice == MenuOptions.Exit)
    {
        AnsiConsole.MarkupLine("[grey]Goodbye![/]");
        break;
    }

    AnsiConsole.WriteLine();

    try
    {
        switch (choice)
        {
            case MenuOptions.Migrate:
                await MigrateDatabaseAsync(serviceProvider);
                break;
            case MenuOptions.Clear:
                await ClearDatabaseAsync(serviceProvider);
                break;
            case MenuOptions.AddChallenges:
                await AddChallengesAsync(serviceProvider);
                break;
        }
    }
    catch (Exception exception)
    {
        AnsiConsole.MarkupLine("[red]Something went wrong while performing the operation:[/]");
        AnsiConsole.WriteException(exception, ExceptionFormats.ShortenEverything);
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Press any key to return to the menu...[/]");
    Console.ReadKey(true);
}

static void RenderHeader(IConfiguration configuration)
{
    AnsiConsole.Write(
        new FigletText("Quantum Lab")
            .Centered()
            .Color(Color.Aqua));

    AnsiConsole.Write(
        new Rule("[aqua]Quantum Summer Lab 2026 — Console Tools[/]")
            .RuleStyle("grey")
            .Centered());

    var connectionConfigured = !string.IsNullOrWhiteSpace(configuration.GetValue<string>("CONNECTION_STRING"));
    var connectionStatus = connectionConfigured ? "[green]configured[/]" : "[red]NOT SET[/]";
    AnsiConsole.MarkupLine($"[grey]Connection string:[/] {connectionStatus}");
    AnsiConsole.WriteLine();
}

static async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

    var pendingMigrations = Array.Empty<string>();

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("aqua"))
        .StartAsync("Checking for pending migrations...", async _ =>
        {
            pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
        });

    if (pendingMigrations.Length == 0)
    {
        AnsiConsole.MarkupLine("[green]✓[/] The database is already up to date.");
        return;
    }

    var pendingTable = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey)
        .AddColumn("[aqua]Pending migration[/]");

    foreach (var migration in pendingMigrations)
    {
        pendingTable.AddRow(Markup.Escape(migration));
    }

    AnsiConsole.Write(pendingTable);

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("aqua"))
        .StartAsync($"Applying {pendingMigrations.Length} migration(s)...", async _ =>
        {
            await dbContext.Database.MigrateAsync();
        });

    AnsiConsole.MarkupLine($"[green]✓[/] Applied [aqua]{pendingMigrations.Length}[/] migration(s).");
}

static async Task ClearDatabaseAsync(IServiceProvider serviceProvider)
{
    AnsiConsole.MarkupLine("[yellow]⚠[/]  This will permanently delete all [red]scores[/], [red]challenges[/] and [red]teams[/].");

    if (!AnsiConsole.Confirm("Are you sure you want to clear the database?", defaultValue: false))
    {
        AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
        return;
    }

    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

    var deleted = new List<(string Entity, int Count)>();

    await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("aqua"))
        .StartAsync("Clearing database...", async context =>
        {
            context.Status("Deleting scores...");
            deleted.Add(("Scores", await dbContext.Scores.ExecuteDeleteAsync()));

            context.Status("Deleting challenges...");
            deleted.Add(("Challenges", await dbContext.Challenges.ExecuteDeleteAsync()));

            context.Status("Deleting teams...");
            deleted.Add(("Teams", await dbContext.Teams.ExecuteDeleteAsync()));
        });

    var resultTable = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey)
        .AddColumn("[aqua]Entity[/]")
        .AddColumn(new TableColumn("[aqua]Rows deleted[/]").RightAligned());

    foreach (var (entity, count) in deleted)
    {
        resultTable.AddRow(entity, $"[yellow]{count}[/]");
    }

    AnsiConsole.Write(resultTable);
    AnsiConsole.MarkupLine("[green]✓[/] The database has been cleared.");
}

static async Task AddChallengesAsync(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

    var challenges = Challenges.All;
    var results = new List<(Challenge Challenge, ChallengeAction Action)>();

    await AnsiConsole.Progress()
        .AutoClear(false)
        .Columns(
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn(Spinner.Known.Dots))
        .StartAsync(async context =>
        {
            var task = context.AddTask("[aqua]Seeding challenges[/]", maxValue: challenges.Count);

            foreach (var challenge in challenges)
            {
                task.Description = $"[aqua]Seeding[/] challenge [white]{Markup.Escape(challenge.Name)}[/]";
                var action = await ProcessChallengeAsync(dbContext, challenge);
                results.Add((challenge, action));
                task.Increment(1);
            }

            task.Description = "[aqua]Seeding challenges[/]";
        });

    var summaryTable = new Table()
        .Border(TableBorder.Rounded)
        .BorderColor(Color.Grey)
        .Title("[aqua]Challenge seeding results[/]")
        .AddColumn("[aqua]Name[/]")
        .AddColumn(new TableColumn("[aqua]Level[/]").Centered())
        .AddColumn("[aqua]Title[/]")
        .AddColumn("[aqua]Action[/]");

    foreach (var (challenge, action) in results)
    {
        var actionMarkup = action == ChallengeAction.Added ? "[green]Added[/]" : "[yellow]Updated[/]";
        summaryTable.AddRow(
            Markup.Escape(challenge.Name),
            challenge.Level.ToString(),
            Markup.Escape(challenge.Title),
            actionMarkup);
    }

    AnsiConsole.Write(summaryTable);

    var added = results.Count(result => result.Action == ChallengeAction.Added);
    var updated = results.Count(result => result.Action == ChallengeAction.Updated);
    AnsiConsole.MarkupLine($"[green]✓[/] Done — [green]{added} added[/], [yellow]{updated} updated[/], [aqua]{results.Count} total[/].");
}

static async Task<ChallengeAction> ProcessChallengeAsync(QuantumSummerLabDbContext dbContext, Challenge challenge)
{
    if (await dbContext.Challenges.AnyAsync(x => x.Name == challenge.Name))
    {
        await dbContext.Challenges.Where(x => x.Name == challenge.Name).ExecuteUpdateAsync(setters =>
            setters.SetProperty(p => p.Title, challenge.Title)
                   .SetProperty(p => p.Description, challenge.Description)
                   .SetProperty(p => p.Tldr, challenge.Tldr)
                   .SetProperty(p => p.SolutionTemplate, challenge.SolutionTemplate)
                   .SetProperty(p => p.ExampleDescription, challenge.ExampleDescription)
                   .SetProperty(p => p.ExampleCode, challenge.ExampleCode)
                   .SetProperty(p => p.VerificationTemplate, challenge.VerificationTemplate)
                   .SetProperty(p => p.ExpectedOutput, challenge.ExpectedOutput)
                   .SetProperty(p => p.ExpectedStates, challenge.ExpectedStates)
                   .SetProperty(p => p.CopilotInstructions, challenge.CopilotInstructions)
                   .SetProperty(p => p.Level, challenge.Level)
        );

        return ChallengeAction.Updated;
    }

    dbContext.Challenges.Add(challenge);
    await dbContext.SaveChangesAsync();

    return ChallengeAction.Added;
}

enum ChallengeAction
{
    Added,
    Updated
}

static class MenuOptions
{
    public const string Migrate = "Migrate database";
    public const string Clear = "Clear database";
    public const string AddChallenges = "Add / update challenges";
    public const string Exit = "Exit";
}