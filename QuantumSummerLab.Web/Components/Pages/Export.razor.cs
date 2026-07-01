using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using QuantumSummerLab.Application.Export.Queries;
using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Application.Teams.Queries;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Export
{
    private bool IsLoading { get; set; } = true;
    private bool IsLoggedIn { get; set; }
    private bool IsAdmin { get; set; }
    private bool IsExporting { get; set; }
    private AuthenticationToken? AuthToken { get; set; }
    private GetExportStatisticsResponse Statistics { get; set; } = new GetExportStatisticsResponse();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var authToken = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");
        IsLoggedIn = authToken.Success;
        AuthToken = authToken.Success ? authToken.Value : null;
        IsAdmin = authToken.Success && authToken.Value.IsAdmin;

        if (IsAdmin && authToken.Value is not null)
        {
            var overview = await Mediator.Send(new GetTeamManagementOverviewQuery
            {
                RequestingTeamId = authToken.Value.TeamId
            });

            if (!overview.IsAuthorized)
            {
                IsAdmin = false;
            }
            else
            {
                Statistics = await Mediator.Send(new GetExportStatisticsQuery
                {
                    RequestingTeamId = authToken.Value.TeamId
                });
            }
        }

        IsLoading = false;
        StateHasChanged();
    }

    private async Task ExportData()
    {
        if (AuthToken is null)
        {
            return;
        }

        IsExporting = true;
        StateHasChanged();

        try
        {
            var data = await Mediator.Send(new ExportDataQuery
            {
                RequestingTeamId = AuthToken.TeamId
            });

            if (!data.IsAuthorized)
            {
                IsAdmin = false;
                return;
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"quantum-summer-lab-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

            using var stream = new MemoryStream(bytes);
            using var streamReference = new DotNetStreamReference(stream);

            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamReference);
        }
        finally
        {
            IsExporting = false;
            StateHasChanged();
        }
    }
}
