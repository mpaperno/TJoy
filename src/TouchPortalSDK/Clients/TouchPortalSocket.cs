using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using TouchPortalSDK.Interfaces;

namespace TouchPortalSDK.Clients
{
    public class TouchPortalSocket : ITouchPortalSocket
    {
        /// <inheritdoc cref="ITouchPortalSocket" />
        public bool IsConnected { get => _socket?.Connected ?? false; }
        /// <inheritdoc cref="ITouchPortalSocket" />
        public int ReceiveBufferSize {
            get => _rcvBufferSz;
            set {
                if (_listenerThread != null && _listenerThread.IsAlive)
                    throw new ArgumentException("Must set buffer size before starting the Listen process.");
                _rcvBufferSz = value;
            }
        }

        private readonly TouchPortalOptions _options;
        private readonly IMessageHandler _messageHandler;
        private readonly ILogger<TouchPortalSocket> _logger;
        private readonly Socket _socket;
        private readonly Thread _listenerThread;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _cancellationToken;
        private readonly Encoding _encoding;

        private int _rcvBufferSz = 1024 * 2;   // really just needs to hold at least one full TP message at a time, too large will be less efficient.

        public TouchPortalSocket(TouchPortalOptions options,
                                 IMessageHandler messageHandler,
                                 ILoggerFactory loggerFactory = null)
        {
            _options = options;
            _messageHandler = messageHandler;
            _logger = loggerFactory?.CreateLogger<TouchPortalSocket>();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = 1000;   // only for blocking mode, controls the listener loop effective sleep period
            _socket.SendTimeout = 1000;      // only for blocking mode, retry sending at this interval

            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;
            //The encoder needs to be without a BOM / Utf8 Identifier:
            _encoding = new UTF8Encoding(false);
            _listenerThread = new Thread(ListenerThreadSync) { IsBackground = false };
        }

        /// <inheritdoc cref="ITouchPortalSocket" />
        public bool Connect()
        {
            try
            {
                //Connect
                var ipAddress = IPAddress.Parse(_options.IpAddress);
                var socketAddress = new IPEndPoint(ipAddress, _options.Port);

                _socket.Connect(socketAddress);

                // Socket can be run in blocking mode or not, but see listener thread method for notes.
                // Blocking seems to use fewer CPU cycles while "sleeping" and has advantage of immediate wakeup upon data.
                // In any case set socket to non-blocking after Connect, for simplicity.
                //_socket.Blocking = false;

                _logger?.LogDebug("TouchPortal connected.");

                return _socket.Connected;
            }
            //Warning: SocketErrors in .Net might depend on OS and Runtime: https://blog.jetbrains.com/dotnet/2020/04/27/socket-error-codes-depend-runtime-operating-system/
            catch (SocketException exception)
                when (exception.SocketErrorCode == SocketError.ConnectionRefused)
            {
                //Could not connect to Touch Portal, ex. Touch Portal is not running.
                //Ex. No connection could be made because the target machine actively refused it. 127.0.0.1:12136
                _logger?.LogError(exception, "Could not connect to Touch Portal, connection refused. Touch Portal might not be running.");
            }
            catch (SocketException exception)
            {
                _logger?.LogError(exception, $"Could not connect to Touch Portal with error code: '{exception.SocketErrorCode}'.");
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, "Unknown error while trying to connect socket, connection failed.");
            }
            return false;
        }

        /// <inheritdoc cref="ITouchPortalSocket" />
        public bool Listen()
        {
            //Create listener thread:
            _logger?.LogDebug("Starting Listener thread...");
            _listenerThread.Start();

            return _listenerThread.IsAlive;
        }

        /// <inheritdoc cref="ITouchPortalSocket" />
        public bool SendMessage(string jsonMessage)
        {
            if (!string.IsNullOrWhiteSpace(jsonMessage))
                return SendMessage(_encoding.GetBytes(jsonMessage));
            return false;
        }

        /// <inheritdoc cref="ITouchPortalSocket" />
        public bool SendMessage(ReadOnlySpan<byte> messageBytes)
        {
            if (!_socket.Connected) {
                _logger?.LogWarning("Socket not connected to Touch Portal.");
                return false;
            }

            try {
                // We could turn this into a task and/or queue thing, but
                // a) we need to deliver messages sequentially (so we need a queue/buffer and a processing task/thread), and
                // b) the actual socket writing is so fast that it's not really worth the extra overhead, it seems,
                //    certainly not of launching separate tasks (stepped through this with debugger, not pretty).
                // And if TP is running too slow to receive our messages this way, they're just going to pile up in our queue or the system socket buffers anyway.
                WriteLine(messageBytes);
                return true;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, "SendMessage exception in WriteLine()");
                //_messageHandler.OnError("Connection Terminated during socket write operation (most likely Touch Portal quit without a goodbye)", exception);
                return false;
            }
        }

        /// <inheritdoc cref="ITouchPortalSocket" />
        public void CloseSocket()
        {
            // pprevent recursion
            if (_cts.IsCancellationRequested)
                return;
            _cts.Cancel();
            // Note we must check _socket.Connected here in case we are exiting due to a socket error,
            // in which case this method is likely being called from within that thread (via handler.OnError()). Hence if we try to
            // wait for the thread to exit, that's just not going to work. We rest assured that it will exit once we're done here.
            // OTOH if we're just disconnecting "nicely" then we do want to make sure the listener thread finished up after we cancelled the token.
            if (_socket != null && _socket.Connected && _listenerThread.IsAlive && !_listenerThread.Join(_socket.ReceiveTimeout * 3)) {
                _logger.LogWarning("Network stream is hung up, interrupting the listener thread.");
                _listenerThread.Interrupt();
            }
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
        }

        private void WriteLine(ReadOnlySpan<byte> msgbytes)
        {
            if (msgbytes == null)
                return;

            int len = msgbytes.Length;
            byte[] bytes = new byte[len + 1];
            var target = new Span<byte>(bytes);
            msgbytes.CopyTo(target);
            bytes[len++] = 10;
            int unsent = len;
            int sent = 0;
            int startAt = 0;
            _logger?.LogDebug($"WriteLine() Starting message send with {len} bytes.");
            do {
                try {
                    sent += _socket.Send(bytes, startAt, unsent, SocketFlags.None);
                    if (sent < len) {
                        unsent -= sent;
                        startAt = sent - 1;
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.TimedOut)    // for blocking socket, we retry again
                        continue;
                    if (e.SocketErrorCode != SocketError.WouldBlock)  // for non-blocking, Resource temporarily unavailable
                        throw;
                    continue;
                }
            }
            while (sent < len && _socket.Connected && !_cancellationToken.IsCancellationRequested);

            _logger?.LogDebug($"WriteLine() Sent {sent} bytes.");
        }

        private void ListenerThreadSync()
        {
            _logger?.LogDebug("Listener thread started.");

            byte[] rcvBuffer = new byte[_rcvBufferSz];
            int buffPos = 0;
            int rcvLen;
            //byte[] empty = new byte[1];  // for ping
            //var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try {
                while (!_cancellationToken.IsCancellationRequested && _socket.Connected)
                {
                    try {
                        //while (!_cancellationToken.IsCancellationRequested && _socket.Available > 0)
                        while (!_cancellationToken.IsCancellationRequested && (rcvLen = _socket.Receive(rcvBuffer, buffPos, _rcvBufferSz - buffPos, SocketFlags.None)) > 0)
                        {
                            //if ((rcvLen = _socket.Receive(rcvBuffer, buffPos, buffLen - buffPos, SocketFlags.None)) < 0)  // uncomment if using _socket.Available
                            //    continue;
                            int nextIdx = 0;
                            int lenRemaining = buffPos + rcvLen;
                            while ((Array.IndexOf(rcvBuffer, (byte)10, nextIdx, lenRemaining) is int nlIdx) && nlIdx > -1) {
                                if (nlIdx == 0) {
                                    ++nextIdx;
                                    if (--lenRemaining <= 0)
                                        break;
                                    continue;
                                }
                                ++nlIdx;  // include the newline
                                var lineLen = nlIdx - nextIdx;
                                byte[] line = new byte[lineLen];
                                Buffer.BlockCopy(rcvBuffer, nextIdx, line, 0, lineLen);
                                nextIdx = nlIdx;
                                lenRemaining -= lineLen;
                                //_logger?.LogDebug(_encoding.GetString(line));
                                _messageHandler.OnMessage(line);
                            }
                            if (nextIdx == 0) {
                                buffPos += rcvLen;
                            }
                            else if (lenRemaining > 0) {
                                Buffer.BlockCopy(rcvBuffer, nextIdx, rcvBuffer, 0, lenRemaining);
                                buffPos = lenRemaining;
                            }
                            else {
                                buffPos = 0;
                            }
                            if (buffPos >= _rcvBufferSz) {
                                _logger?.LogError("Receive buffer overflow!");
                                buffPos = 0;
                            }
                        }  // process bytes loop
                    }
                    // this try/catch is not needed if using _socket.Available
                    catch (SocketException e) {
                        if (e.SocketErrorCode == SocketError.TimedOut)     // for blocking socket means timeout expired, we go for next loop
                            continue;
                        if (e.SocketErrorCode != SocketError.WouldBlock)   // for non-blocking, just means no data to rcv so we continue
                            throw;                                         // otherwise, we have a fatal issue (probably TP exited)
                    }
                    // for blocking socket sleep time is controlled with _socket.ReceiveTimeout
                    if (!_socket.Blocking)
                        _cancellationToken.WaitHandle.WaitOne(25);

                    // if using _socket.Available then need to ping the socket occasionally to check health. This will throw SocketException if the host has gone away.
                    //if (stopwatch.ElapsedMilliseconds > 5000) {
                    //      stopwatch.Restart();
                    //      _socket.Send(empty, 0, 0);
                    //}

                }  // outer "wait for data or cacellation" loop

                // This will happen when using socket.Available and the socket is closed unexpectedly (bbasically equivalent of the catch statements above).
                // We could probably remove this check if not using Available, but it dosn't hurt anything to leave it in.
                if (!_socket.Connected && !_cancellationToken.IsCancellationRequested)
                    _messageHandler.OnError("Connection Terminated, Touch Portal quit without a goodbye.", null);
            }
            catch (SocketException exception)
            {
                // these are already "filtered" in the loop above and rethrown if needed, so it must be real.
                if (!_cancellationToken.IsCancellationRequested)
                    _messageHandler.OnError("Connection Terminated, most likely Touch Portal quit without a goodbye.", exception);
            }
            catch (Exception exception)
            {
                if (!_cancellationToken.IsCancellationRequested)
                    _messageHandler.OnError("Connection Terminated, unknown exception in listener thread.", exception);
            }

            _logger?.LogDebug("Listener thread exited.");
        }

    }
}
