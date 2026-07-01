using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Helpers;

namespace QuantumSummerLab.Copilot.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotServices(this IServiceCollection services)
    {
        services.AddScoped<ICopilotHelper, CopilotHelper>();
        services.AddScoped<IErrorSummarizer, CopilotHelper>();

        return services;
    }
}