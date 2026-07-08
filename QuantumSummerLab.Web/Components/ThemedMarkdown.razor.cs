using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace QuantumSummerLab.Web.Components;

public partial class ThemedMarkdown
{
    [Parameter]
    public string Value { get; set; } = string.Empty;

    [CascadingParameter(Name = "IsDarkMode")]
    private bool IsDarkMode { get; set; }

    private MudMarkdownStyling _styling = new();

    protected override void OnParametersSet()
    {
        var styling = new MudMarkdownStyling();
        styling.CodeBlock.Theme = IsDarkMode ? CodeBlockTheme.GithubDark : CodeBlockTheme.Github;
        _styling = styling;
    }
}
