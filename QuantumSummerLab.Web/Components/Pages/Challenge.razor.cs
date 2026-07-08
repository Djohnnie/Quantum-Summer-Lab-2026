using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QuantumSummerLab.Application.Challenges.Queries;
using QuantumSummerLab.Application.Scores.Commands;
using QuantumSummerLab.Application.Scores.Queries;
using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Copilot;
using QuantumSummerLab.Copilot.Extensions;
using QuantumSummerLab.Web.Helpers;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Challenge
{
    private bool? _lastLoggedIn;

    [Parameter]
    public string ChallengeName { get; set; } = string.Empty;

    private bool IsLoggedIn { get; set; }
    private string TeamName { get; set; } = string.Empty;
    private Guid TeamId { get; set; }
    private bool IsLoading { get; set; }
    private bool IsAvailable { get; set; }
    private bool IsSubmitting { get; set; }
    private List<YourSubmission> YourSubmissions { get; set; } = new List<YourSubmission>();
    private List<VerificationFeedback> VerificationFeedback { get; set; } = new List<VerificationFeedback>();

    private string Title { get; set; } = string.Empty;
    private string[] Description { get; set; } = Array.Empty<string>();
    private string Tldr { get; set; } = string.Empty;
    private string SolutionTemplate { get; set; } = string.Empty;
    private string ChallengeSolutionTemplate { get; set; } = string.Empty;
    private string[] ExampleDescription { get; set; } = Array.Empty<string>();
    private string ExampleCode { get; set; } = string.Empty;
    private string CopilotInstructions { get; set; } = string.Empty;
    private string FeedbackMessage { get; set; } = string.Empty;
    private string? Tips { get; set; }
    private bool? IsValid { get; set; }

    private string Solution { get; set; } = string.Empty;

    private StandaloneCodeEditor _editor = null!;
    private bool _editorReady;
    private bool? _appliedDarkMode;
    private string? _loadedChallengeName;

    [CascadingParameter(Name = "IsDarkMode")]
    private bool IsDarkMode { get; set; }

    private bool IsSuccess { get; set; }
    private int NumberOfAttempts { get; set; }
    private int TeamsCompleted { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedChallengeName != ChallengeName)
        {
            _loadedChallengeName = ChallengeName;
            IsLoading = true;
            // Rendering the loading state unmounts the editor; the one created for
            // the new challenge re-arms this flag through OnEditorInit.
            _editorReady = false;
            YourSubmissions = new List<YourSubmission>();

            var challenge = await Mediator.Send(new GetChallengeByNameQuery { ChallengeName = ChallengeName });
            IsAvailable = challenge.IsAvailable;
            if (IsAvailable)
            {
                Title = challenge.Title;
                Description = challenge.Description.Split("[BR]");
                Tldr = challenge.Tldr;
                ChallengeSolutionTemplate = challenge.SolutionTemplate;
                SolutionTemplate = $"```js{Environment.NewLine}{ChallengeSolutionTemplate}{Environment.NewLine}```";
                Solution = ChallengeSolutionTemplate;
                ExampleDescription = challenge.ExampleDescription.Split("[BR]", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                ExampleCode = $"```js{Environment.NewLine}{challenge.ExampleCode}{Environment.NewLine}```";
                FeedbackMessage = "You have not yet submitted a solution";
                VerificationFeedback = new List<VerificationFeedback>();
                Tips = null;
                CopilotInstructions = challenge.CopilotInstructions;
                await LoadScore();
                await InitializeEditorSolution();
                await LoadStatistics();
            }

            IsLoading = false;
        }

        await ApplyEditorThemeAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await ApplyEditorThemeAsync();

        var authToken = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");

        if (_lastLoggedIn == null || _lastLoggedIn != authToken.Success)
        {
            IsLoggedIn = authToken.Success;
            TeamName = authToken.Success ? authToken.Value!.TeamName : string.Empty;
            TeamId = authToken.Success ? authToken.Value!.TeamId : Guid.Empty;
            _lastLoggedIn = authToken.Success;

            await LoadScore();
            await InitializeEditorSolution();
            await LoadStatistics();
            StateHasChanged();
        }
    }

    protected async Task Submit()
    {
        IsSubmitting = true;
        StateHasChanged();

        if (_editorReady)
        {
            try
            {
                Solution = await _editor.GetValue();
            }
            catch (JSException)
            {
                _editorReady = false;
            }
        }

        var response = await Mediator.Send(new VerifyChallengeSolutionCommand
        {
            ChallengeName = ChallengeName,
            RequestingTeamId = TeamId,
            Solution = Solution,
            Timestamp = DateTime.UtcNow
        });

        IsValid = response.IsValid;
        FeedbackMessage = response.FeedbackMessage;
        VerificationFeedback = response.Feedback;
        Tips = response.Tips;

        await LoadScore();
        await LoadStatistics();

        IsSubmitting = false;

        NavigationHelper.Update();
        StateHasChanged();
    }

    protected async Task Clear()
    {
        Solution = string.Empty;
        IsValid = null;
        FeedbackMessage = "You have not yet submitted a solution";
        VerificationFeedback = new List<VerificationFeedback>();
        Tips = null;
        await TrySetEditorValueAsync(string.Empty);
        StateHasChanged();
    }

    protected async Task CopyLastSubmission()
    {
        var lastSubmission = YourSubmissions?.FirstOrDefault();
        if (lastSubmission != null)
        {
            await CopyCode(lastSubmission.Code);
        }
    }

    protected async Task CopyCode(string code)
    {
        Solution = code ?? string.Empty;
        await TrySetEditorValueAsync(Solution);
        StateHasChanged();
    }

    protected void Copilot()
    {
        DrawerHelper.Popout(ChallengeName, CopilotInstructions);
    }

    private async Task LoadScore()
    {
        if (string.IsNullOrEmpty(TeamName))
        {
            YourSubmissions = new List<YourSubmission>();
            IsSuccess = false;
            NumberOfAttempts = 0;
            return;
        }

        var score = await Mediator.Send(new GetYourScoreQuery { ChallengeName = ChallengeName, TeamName = TeamName });
        IsSuccess = score.IsSuccess;
        NumberOfAttempts = score.TotalAttempts;

        var submissions = await Mediator.Send(new GetYourSubmissionsQuery { ChallengeName = ChallengeName, TeamName = TeamName });
        YourSubmissions = submissions.YourSubmissions;
    }

    private async Task InitializeEditorSolution()
    {
        var initialSolution = ChallengeSolutionTemplate ?? string.Empty;

        if (YourSubmissions != null && YourSubmissions.Any())
        {
            var lastSubmission = YourSubmissions.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(lastSubmission?.Code))
            {
                initialSolution = lastSubmission.Code;
            }
        }

        Solution = initialSolution;
        await TrySetEditorValueAsync(Solution);

        StateHasChanged();
    }

    private async Task LoadStatistics()
    {
        var statistics = await Mediator.Send(new GetChallengeStatisticsQuery
        {
            ChallengeName = ChallengeName,
            RequestingTeamId = TeamId
        });
        TeamsCompleted = statistics.TeamsCompleted;
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "qsharp",
            Theme = IsDarkMode ? "vs-dark" : "vs",
            Value = Solution ?? string.Empty,
            FontSize = 14,
            Minimap = new EditorMinimapOptions { Enabled = false },
            ScrollBeyondLastLine = false,
            TabSize = 4,
            InsertSpaces = true,
        };
    }

    private async Task OnEditorInit()
    {
        _editorReady = true;

        if (!string.IsNullOrEmpty(Solution))
        {
            await TrySetEditorValueAsync(Solution);
        }

        // Force a render so OnAfterRenderAsync applies the theme once the editor
        // is painted. Applying the theme here (pre-paint) leaves the first editor
        // of the session showing Monaco's default light theme until re-navigation.
        StateHasChanged();
    }

    // A re-render can unmount the editor (challenge switch, login-state change,
    // IsSuccess flipping) while a JS call to it is still in flight; BlazorMonaco
    // then throws because its JS-side registry no longer holds the instance.
    // Losing the call is harmless: the replacement editor applies Solution in
    // OnEditorInit.
    private async Task TrySetEditorValueAsync(string value)
    {
        if (!_editorReady)
        {
            return;
        }

        try
        {
            await _editor.SetValue(value);
        }
        catch (JSException)
        {
            _editorReady = false;
        }
    }

    private async Task ApplyEditorThemeAsync()
    {
        if (_editorReady && _appliedDarkMode != IsDarkMode)
        {
            _appliedDarkMode = IsDarkMode;
            await BlazorMonaco.Editor.Global.SetTheme(JS, IsDarkMode ? "vs-dark" : "vs");
        }
    }
}
