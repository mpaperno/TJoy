using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TouchPortalSDK.Configuration;
using TouchPortalSDK.Interfaces;
using TouchPortalSDK.Messages.Commands;
using TouchPortalSDK.Messages.Events;
using TouchPortalSDK.Messages.Models;
using TouchPortalSDK.Messages.Models.Enums;

namespace TouchPortalSDK.Clients
{
    public class TouchPortalClient : ITouchPortalClient, IMessageHandler
    {
        /// <inheritdoc cref="ITouchPortalClient" />
        public bool IsConnected => !_closing && (_touchPortalSocket?.IsConnected ?? false);

        private readonly ILogger<TouchPortalClient> _logger;
        private readonly ITouchPortalEventHandler _eventHandler;
        private readonly ITouchPortalSocket _touchPortalSocket;

        private CancellationTokenSource _cts;
        private CancellationToken _cancellationToken;
        private readonly ConcurrentQueue<byte[]> _incommingMessages;
        private readonly ManualResetEventSlim _messageReadyEvent;
        private Task _incomingMessageTask = null;
        private bool _closing = false;

        private readonly ManualResetEvent _infoWaitHandle;

        private InfoEvent _lastInfoEvent;

        public TouchPortalClient(ITouchPortalEventHandler eventHandler,
                                 ITouchPortalSocketFactory socketFactory,
                                 ILoggerFactory loggerFactory = null)
        {
            if (string.IsNullOrWhiteSpace(eventHandler?.PluginId))
                throw new InvalidOperationException($"{nameof(ITouchPortalEventHandler)}: PluginId cannot be null or empty.");

            _eventHandler = eventHandler;
            _touchPortalSocket = socketFactory.Create(this);
            _logger = loggerFactory?.CreateLogger<TouchPortalClient>();

            _incommingMessages = new ConcurrentQueue<byte[]>();
            _messageReadyEvent = new ManualResetEventSlim();
            _infoWaitHandle = new ManualResetEvent(false);
        }

        #region Setup

        /// <inheritdoc cref="ITouchPortalClient" />
        bool ITouchPortalClient.Connect()
        {
            //Connect:
            _logger?.LogInformation("Connecting to TouchPortal.");
            var connected = _touchPortalSocket.Connect();
            if (!connected)
                return false;

            //Listen:
            // set up message processing queue and task
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;
            _incommingMessages.Clear();
            _logger?.LogDebug("Starting message processing queue task.");
            _incomingMessageTask = Task.Run(MessageHandlerTask);
            _incomingMessageTask.ConfigureAwait(false);
            // start socket reader thread
            _logger?.LogDebug("Start Socket listener.");
            var listening = _touchPortalSocket.Listen();
            if (!listening)
                return false;
            _logger?.LogDebug("Listener created.");

            //Pair:
            _logger?.LogInformation("Sending pair message.");
            var pairCommand = new PairCommand(_eventHandler.PluginId);
            var pairing = SendCommand(pairCommand);
            if (!pairing)
                return false;

            //Waiting for InfoMessage:
            if (_infoWaitHandle.WaitOne(10000))
                _logger?.LogInformation("Received pair response.");
            else
                Close("Pair response timed out! Closing connection.");

            return _lastInfoEvent != null;
        }

        /// <inheritdoc cref="ITouchPortalClient" />
        void ITouchPortalClient.Close()
            => Close("Closed by plugin.");

        private void Close(string message, Exception exception = default)
        {
            if (_closing)
                return;
            _closing = true;

            _logger?.LogInformation(exception, $"Closing TouchPortal Plugin: '{message}'");

            _eventHandler.OnClosedEvent(message);

            _touchPortalSocket?.CloseSocket();

            if (_incomingMessageTask == null)  // shouldn't happen but JIC
                return;

            _cts?.Cancel();
            if (_incomingMessageTask.Status == TaskStatus.Running && !_incomingMessageTask.Wait(2000))
                _logger?.LogWarning("The incoming message processor task is hung!");

            try { _incomingMessageTask.Dispose(); }
            catch { /* ignore in case it hung */ }
            _incomingMessageTask = null;
            _cts?.Dispose();
            _cts = null;
        }

        private async void CloseAsync(string message)
        {
            await Task.Run(delegate { Close(message); });
        }

        #endregion

        #region TouchPortal Command Handlers

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.SettingUpdate(string name, string value)
        {
            try {
                return SendCommand(new SettingUpdateCommand(name, value));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "SettingUpdateCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.CreateState(string stateId, string desc, string defaultValue)
        {
            try {
                return SendCommand(new CreateStateCommand(stateId, desc, defaultValue));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "CreateStateCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.RemoveState(string stateId)
        {
            try {
                return SendCommand(new RemoveStateCommand(stateId));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "RemoveStateCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.StateUpdate(string stateId, string value)
        {
            try {
                return SendCommand(new StateUpdateCommand(stateId, value));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "StateUpdateCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.ChoiceUpdate(string choiceId, string[] values, string instanceId)
        {
            try {
                return SendCommand(new ChoiceUpdateCommand(choiceId, values, instanceId));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "ChoiceUpdateCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.UpdateActionData(string dataId, double minValue, double maxValue, ActionDataType dataType, string instanceId)
        {
            try {
                return SendCommand(new UpdateActionDataCommand(dataId, minValue, maxValue, dataType, instanceId));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "UpdateActionDataCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.ShowNotification(string notificationId, string title, string message, NotificationOptions[] notificationOptions)
        {
            try {
                return SendCommand(new ShowNotificationCommand(notificationId, title, message, notificationOptions));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "ShowNotificationCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.ConnectorUpdate(string connectorId, int value)
        {
            try {
                return SendCommand(new ConnectorUpdateCommand(_eventHandler.PluginId, connectorId, value));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "ConnectorUpdateCommand() validation failed.");
                return false;
            }
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.ConnectorUpdateShort(string shortId, int value)
        {
            try {
                return SendCommand(new ConnectorUpdateShortCommand(_eventHandler.PluginId, shortId, value));
            }
            catch (ArgumentException e) {
                _logger?.LogWarning(e, "ConnectorUpdateShortCommand() validation failed.");
                return false;
            }
        }

        public bool SendCommand<TCommand>(TCommand command, [CallerMemberName]string callerMemberName = "")
            where TCommand : ITouchPortalMessage
        {
            var jsonMessage = JsonSerializer.SerializeToUtf8Bytes(command, Options.JsonSerializerOptions);

            var success = _touchPortalSocket.SendMessage(jsonMessage);

            _logger?.LogDebug($"[{callerMemberName}] sent: '{success}'.");

            return success;
        }

        /// <inheritdoc cref="ICommandHandler" />
        bool ICommandHandler.SendMessage(string message)
            => _touchPortalSocket.SendMessage(message);

        #endregion

        #region TouchPortal Event Handler

        /// <inheritdoc cref="IMessageHandler" />
        public void OnError(string message, Exception exception = default)
        // Block here so that we can finish up before the socket listener thread exits. This is not ideal but it works
            => Close("Terminating due to socket error:", exception);

        /// <inheritdoc cref="IMessageHandler" />
        void IMessageHandler.OnMessage(byte[] message)
        {
            _incommingMessages.Enqueue(message);
            _messageReadyEvent.Set();
        }

        private void MessageHandlerTask()
        {
            _logger?.LogDebug("Message processing queue task started.");
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                  _messageReadyEvent.Wait(_cancellationToken);
                  _messageReadyEvent.Reset();
                  while (!_cancellationToken.IsCancellationRequested && _incommingMessages.TryDequeue(out byte[] message)) {
                      try {
                          var eventMessage = MessageResolver.ResolveMessage(message);
                          switch (eventMessage)
                          {
                              case InfoEvent infoEvent:
                                  _lastInfoEvent = infoEvent;
                                  _infoWaitHandle.Set();
                                  _eventHandler.OnInfoEvent(infoEvent);
                                  break;
                              case CloseEvent _:
                                  CloseAsync("TouchPortal sent a Plugin close event.");
                                  break;
                              case ListChangeEvent listChangeEvent:
                                  _eventHandler.OnListChangedEvent(listChangeEvent);
                                  break;
                              case BroadcastEvent broadcastEvent:
                                  _eventHandler.OnBroadcastEvent(broadcastEvent);
                                  break;
                              case SettingsEvent settingsEvent:
                                  _eventHandler.OnSettingsEvent(settingsEvent);
                                  break;
                              case NotificationOptionClickedEvent notificationEvent:
                                  _eventHandler.OnNotificationOptionClickedEvent(notificationEvent);
                                  break;
                              case ConnectorChangeEvent connectorChangeEvent:
                                  _eventHandler.OnConnecterChangeEvent(connectorChangeEvent);
                                  break;
                              case ShortConnectorIdNotificationEvent shortConnectorIdEvent:
                                  _eventHandler.OnShortConnectorIdNotificationEvent(shortConnectorIdEvent);
                                  break;
                              //All of Action, Up, Down:
                              case ActionEvent actionEvent:
                                  _eventHandler.OnActionEvent(actionEvent);
                                  break;
                              default:
                                  _eventHandler.OnUnhandledEvent(System.Text.Encoding.UTF8.GetString(message));
                                  break;
                          }
                        }
                        // Catch any parsing exceptions (unlikely)
                        catch (JsonException e) {
                            _logger?.LogWarning(e, $"JSON parsing exception, see trace for details. Continuing execution with next message.'");
                            continue;
                        }
                        // Catch any exceptions in the plugin user's callback code itself.
                        // This does assume the plugin author is looking at their logs/console and not relying on us crashing on their exceptions.
                        catch (Exception e) {
                          _logger?.LogWarning(e, $"Exception in message event handler. Continuing execution with next message.'");
                          continue;
                      }
                  }
                }
                catch (OperationCanceledException) { break; }  // when _messageReadyEvent.Wait() is canceled by token (or task was started with cancellation token)
                catch (ObjectDisposedException)    { break; }  // shouldn't happen but if it does it means we're shutting down anyway
                catch (Exception e) {                          // ok here we really don't know what happened
                  _logger.LogError(e, "Exception in processing queue task, cannot continue.");
                  break;
                }
            }
            _logger?.LogDebug("Message processing queue task exited.");
        }

        #endregion
    }
}
