using Microsoft.AspNetCore.Components;

namespace QuantumSummerLab.Web.Components;

public partial class ExploreCard
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string Link { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string GetLink()
    {
        if (String.IsNullOrEmpty(Link))
        {
            return $"/components/{Title.Replace(" ", "").ToLowerInvariant()}";
        }
        else
        {
            return Link;
        }
    }
}
