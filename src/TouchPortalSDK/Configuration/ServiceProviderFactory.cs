using System;
using Microsoft.Extensions.DependencyInjection;
using TouchPortalSDK.Clients;
using TouchPortalSDK.Interfaces;

namespace TouchPortalSDK.Configuration
{
    public class ServiceProviderFactory : ITouchPortalSocketFactory, ITouchPortalClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor used if registered through AddTouchPortalSdk.
        /// </summary>
        public ServiceProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="ITouchPortalClientFactory" />
        public ITouchPortalClient Create(ITouchPortalEventHandler eventHandler)
        {
            if (eventHandler is null)
                throw new ArgumentNullException(nameof(eventHandler));

            return ActivatorUtilities.CreateInstance<TouchPortalClient>(_serviceProvider, eventHandler);
        }

        /// <inheritdoc cref="ITouchPortalSocketFactory" />
        public ITouchPortalSocket Create(IMessageHandler messageHandler)
        {
            if (messageHandler is null)
                throw new ArgumentNullException(nameof(messageHandler));

            return ActivatorUtilities.CreateInstance<TouchPortalSocket>(_serviceProvider, messageHandler);
        }
    }
}
