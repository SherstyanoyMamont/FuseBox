using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FuseBox.Ai;

public static class ConfiguratorAiServiceCollectionExtensions
{
    public static IServiceCollection AddFuseBoxConfiguratorAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OpenAiConfiguratorOptions>()
            .Bind(configuration.GetSection(OpenAiConfiguratorOptions.SectionName));

        services.AddHttpClient<ConfiguratorAiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        return services;
    }
}
