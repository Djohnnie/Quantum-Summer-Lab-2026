using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Helpers;
using QuantumSummerLab.Data.DependencyInjection;
using System.Reflection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var licenseKey = configuration.GetValue<string>("MEDIATR_LICENSEKEY");

        var executingAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(c =>
        {
            c.LicenseKey = licenseKey;
            c.RegisterServicesFromAssembly(executingAssembly);
        });

        services.AddScoped<IPasswordHashHelper, PasswordHashHelper>();
        services.AddHttpClient();
        services.AddDataServices();

        return services;
    }
}