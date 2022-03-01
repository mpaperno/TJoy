using System;
using Microsoft.Extensions.Logging;
using TouchPortalSDK.Clients;
using TouchPortalSDK.Interfaces;

namespace TouchPortalSDK
{

    /// <summary>
    /// Factories are a pattern that works well with callbacks.
    /// </summary>
    public class TouchPortalFactory : ITouchPortalSocketFactory, ITouchPortalClientFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly TouchPortalOptions _options;
        
        /// <summary>
        /// Private so we don't expose the socket factory.
        /// </summary>
        public TouchPortalFactory(TouchPortalOptions options, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _options = options ?? new TouchPortalOptions();
        }

        /// <summary>
        /// Factory for creating the Touch Portal client.
        /// </summary>
        /// <param name="eventHandler">Handler the events from Touch Portal, normally the plugin instance.</param>
        /// <param name="options">Optional options, if null, default values are selected.</param>
        /// <param name="loggerFactory">Optional logger factory, if null, no logger is created.</param>
        /// <returns></returns>
        public static ITouchPortalClient CreateClient(ITouchPortalEventHandler eventHandler, TouchPortalOptions options = null, ILoggerFactory loggerFactory = null)
        {
            ITouchPortalClientFactory factory = new TouchPortalFactory(options, loggerFactory);
            
            return factory.Create(eventHandler);
        }

        /// <inheritdoc cref="ITouchPortalClientFactory" />
        ITouchPortalClient ITouchPortalClientFactory.Create(ITouchPortalEventHandler eventHandler)
        {
            if (eventHandler is null)
                throw new ArgumentNullException(nameof(eventHandler));

            return new TouchPortalClient(eventHandler, this, _loggerFactory);
        }

        /// <inheritdoc cref="ITouchPortalSocketFactory" />
        ITouchPortalSocket ITouchPortalSocketFactory.Create(IMessageHandler messageHandler)
        {
            if (messageHandler is null)
                throw new ArgumentNullException(nameof(messageHandler));

            return new TouchPortalSocket(_options, messageHandler, _loggerFactory);
        }
    }
}