using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TouchPortalSDK.Interfaces;

namespace TouchPortalSDK.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTouchPortalSdk(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            //Add configuration:
            if (configuration != null)
                serviceCollection.Configure<TouchPortalOptions>(configuration.GetSection("TouchPortalOptions"));
            serviceCollection.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<TouchPortalOptions>>().Value);
            
            //Add services, only expose Interfaces:
            serviceCollection.AddTransient(serviceProvider => new ServiceProviderFactory(serviceProvider));
            serviceCollection.AddTransient<ITouchPortalSocketFactory>(serviceProvider => serviceProvider.GetRequiredService<ServiceProviderFactory>());
            serviceCollection.AddTransient<ITouchPortalClientFactory>(serviceProvider => serviceProvider.GetRequiredService<ServiceProviderFactory>());
        }
    }
}
