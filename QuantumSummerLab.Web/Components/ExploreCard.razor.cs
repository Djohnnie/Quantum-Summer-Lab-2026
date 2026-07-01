using Microsoft.AspNetCore.Components;

namespace QuantumSummerLab.Web.Components;

public partial class ExploreCard
{
    [Parameter] public string Title { get; set; }
    [Parameter] public string Link { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }

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
