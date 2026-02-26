using System.Reflection;
using BackBase.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackBase.Application;

public static class DependencyInjection
{
    private const string InfrastructureAssemblyName = "Infrastructure";
    private const string InfrastructureClassName = "BackBase.Infrastructure.DependencyInjection";
    private const string AddInfrastructureMethodName = "AddInfrastructure";
    private const string InitializeInfrastructureMethodName = "InitializeInfrastructureAsync";

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationAssembly = typeof(ValidationBehavior<,>).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        LoadAndRegisterInfrastructure(services, configuration);

        return services;
    }

    public static async Task InitializeApplicationAsync(this IServiceProvider serviceProvider)
    {
        await InvokeInfrastructureInitializationAsync(serviceProvider).ConfigureAwait(false);
    }

    private static void LoadAndRegisterInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var infrastructureAssembly = Assembly.Load(InfrastructureAssemblyName);
        var diType = infrastructureAssembly.GetType(InfrastructureClassName)
            ?? throw new InvalidOperationException(
                $"Could not find type '{InfrastructureClassName}' in assembly '{InfrastructureAssemblyName}'.");

        var addMethod = diType.GetMethod(AddInfrastructureMethodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Could not find method '{AddInfrastructureMethodName}' on type '{InfrastructureClassName}'.");

        addMethod.Invoke(null, [services, configuration]);
    }

    private static async Task InvokeInfrastructureInitializationAsync(IServiceProvider serviceProvider)
    {
        var infrastructureAssembly = Assembly.Load(InfrastructureAssemblyName);
        var diType = infrastructureAssembly.GetType(InfrastructureClassName)
            ?? throw new InvalidOperationException(
                $"Could not find type '{InfrastructureClassName}' in assembly '{InfrastructureAssemblyName}'.");

        var initMethod = diType.GetMethod(InitializeInfrastructureMethodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                $"Could not find method '{InitializeInfrastructureMethodName}' on type '{InfrastructureClassName}'.");

        var result = initMethod.Invoke(null, [serviceProvider]);

        if (result is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }
}
