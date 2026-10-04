using Microsoft.Extensions.Options;

namespace FuseBox.App.Services.Pricing;

public static class PricingServiceCollectionExtensions
{
    public static IServiceCollection AddFuseBoxPricing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PricingOptions>()
            .Bind(configuration.GetSection(PricingOptions.SectionName));

        services.AddSingleton<TmePriceCacheStore>();

        services.AddHttpClient<TmeApiClient>((serviceProvider, http) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<PricingOptions>>()
                .Value;

            var baseUrl = string.IsNullOrWhiteSpace(options.Tme.BaseUrl)
                ? "https://api.tme.eu/"
                : options.Tme.BaseUrl.Trim();

            if (!baseUrl.EndsWith('/'))
                baseUrl += "/";

            http.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            http.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<PanelPricingService>();

        return services;
    }
}
