/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omni.Shared;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
    /// <summary>
    /// The HttpLite class serves as a container for HTTP simulation functionalities.
    /// It provides an inner implementation, which is responsible for simulating
    /// HTTP GET and POST requests similar to Express.js through the transporter(Sockets).
    /// </summary>
    public static class HttpLite
    {
        public class HttpFetch
        {
            private int routeId = int.MinValue;
            internal readonly Dictionary<int, TaskCompletionSource<DataBuffer>> asyncTasks = new();

            /// <summary>
            /// Asynchronously sends an HTTP GET request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the GET request is sent.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server.</returns>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public Task<DataBuffer> GetAsync(
                string routeName,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                int lastId = routeId;
                using DataBuffer message = DefaultHeader(routeName, lastId);
                routeId++;

                return Send(
                    MessageType.HttpGetFetchAsync,
                    message,
                    timeout,
                    deliveryMode,
                    lastId,
                    sequenceChannel
                );
            }

            /// <summary>
            /// Asynchronously sends an HTTP POST request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the POST request is sent.</param>
            /// <param name="callback">A callback function that processes(writes) the DataBuffer before sending the request.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server.</returns>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public async Task<DataBuffer> PostAsync(
                string routeName,
                Func<DataBuffer, Task> callback,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                int lastId = routeId;
                using DataBuffer message = DefaultHeader(routeName, lastId);
                routeId++;

                // Await written the data before sending!
                await callback(message);
                return await Send(
                    MessageType.HttpPostFetchAsync,
                    message,
                    timeout,
                    deliveryMode,
                    lastId,
                    sequenceChannel
                );
            }

            /// <summary>
            /// Asynchronously sends an HTTP POST request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the POST request is sent.</param>
            /// <param name="callback">A callback function that processes(writes) the DataBuffer before sending the request.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server.</returns>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public Task<DataBuffer> PostAsync(
                string routeName,
                Action<DataBuffer> callback,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                int lastId = routeId;
                using DataBuffer message = DefaultHeader(routeName, lastId);
                routeId++;

                callback(message);
                return Send(
                    MessageType.HttpPostFetchAsync,
                    message,
                    timeout,
                    deliveryMode,
                    lastId,
                    sequenceChannel
                );
            }

            private Task<DataBuffer> Send(
                byte msgId,
                DataBuffer message,
                int timeout,
                DeliveryMode deliveryMode,
                int lastId,
                byte sequenceChannel
            )
            {
                Client.SendMessage(msgId, message, deliveryMode, sequenceChannel);

                // Timeout system
                TaskCompletionSource<DataBuffer> source = new();
                CancellationTokenSource cts = new(timeout);
                cts.Token.Register(() =>
                {
                    if (!source.Task.IsCompletedSuccessfully)
                    {
                        NetworkLogger.__Log__(
                            $"The request has timed out. Ensure that the route exists and that the server is running or the request will fail.",
                            NetworkLogger.LogType.Error
                        );

                        cts.Cancel();
                        cts.Dispose();
                        source.TrySetCanceled();
                    }
                    else
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }
                });

                asyncTasks.Add(lastId, source);
                return source.Task;
            }

            private DataBuffer DefaultHeader(string routeName, int lastId)
            {
                var message = Pool.Rent(); // disposed by the caller
                message.FastWrite(routeName);
                message.FastWrite(lastId);
                return message;
            }
        }

        public class HttpExpress
        {
            internal readonly Dictionary<
                string,
                Func<DataBuffer, NetworkPeer, Task>
            > asyncGetTasks = new();

            internal readonly Dictionary<
                string,
                Func<DataBuffer, DataBuffer, NetworkPeer, Task>
            > asyncPostTasks = new();

            internal readonly Dictionary<string, Action<DataBuffer, NetworkPeer>> getTasks = new();

            internal readonly Dictionary<
                string,
                Action<DataBuffer, DataBuffer, NetworkPeer>
            > postTasks = new();

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void GetAsync(string routeName, Func<DataBuffer, NetworkPeer, Task> callback)
            {
                asyncGetTasks.Add(routeName, callback);
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void GetAsync(string routeName, Action<DataBuffer, NetworkPeer> callback)
            {
                getTasks.Add(routeName, callback);
            }

            /// <summary>
            /// Registers an asynchronous POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.</param>
            public void PostAsync(
                string routeName,
                Func<DataBuffer, DataBuffer, NetworkPeer, Task> callback
            )
            {
                asyncPostTasks.Add(routeName, callback);
            }

            /// <summary>
            /// Registers an asynchronous POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.</param>
            public void PostAsync(
                string routeName,
                Action<DataBuffer, DataBuffer, NetworkPeer> callback
            )
            {
                postTasks.Add(routeName, callback);
            }
        }

        /// <summary>
        /// Provides methods to simulate HTTP GET and POST requests.
        /// </summary>
        public static HttpFetch Fetch { get; } = new();

        /// <summary>
        /// Handles asynchronous GET and POST requests by maintaining lists of routes
        /// and their associated callback functions, simulating an Express.js-like behavior.
        /// </summary>
        public static HttpExpress Http { get; } = new();

        internal static void Initialize()
        {
            Client.OnMessage += OnClientMessage;
            Server.OnMessage += OnServerMessage;
        }

        private static async void OnServerMessage(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            int sequenceChannel
        )
        {
            buffer.ResetReadPosition();
            string routeName = buffer.ReadString();
            int routeId = buffer.Read<int>();
            if (msgId == MessageType.HttpGetFetchAsync)
            {
                if (
                    Http.asyncGetTasks.TryGetValue(
                        routeName,
                        out Func<DataBuffer, NetworkPeer, Task> asyncCallback
                    )
                )
                {
                    using var response = Pool.Rent();
                    await asyncCallback(response, peer);
                    Send(MessageType.HttpGetResponseAsync, response);
                }
                else if (
                    Http.getTasks.TryGetValue(
                        routeName,
                        out Action<DataBuffer, NetworkPeer> callback
                    )
                )
                {
                    using var response = Pool.Rent();
                    callback(response, peer);
                    Send(MessageType.HttpGetResponseAsync, response);
                }
            }
            else if (msgId == MessageType.HttpPostFetchAsync)
            {
                if (
                    Http.asyncPostTasks.TryGetValue(
                        routeName,
                        out Func<DataBuffer, DataBuffer, NetworkPeer, Task> asyncCallback
                    )
                )
                {
                    using var request = Pool.Rent();
                    request.Write(buffer.GetSpan());
                    request.ResetWrittenCount();

                    using var response = Pool.Rent();
                    await asyncCallback(request, response, peer);
                    Send(MessageType.HttpPostResponseAsync, response);
                }
                else if (
                    Http.postTasks.TryGetValue(
                        routeName,
                        out Action<DataBuffer, DataBuffer, NetworkPeer> callback
                    )
                )
                {
                    using var request = Pool.Rent();
                    request.Write(buffer.GetSpan());
                    request.ResetWrittenCount();

                    using var response = Pool.Rent();
                    callback(request, response, peer);
                    Send(MessageType.HttpPostResponseAsync, response);
                }
            }

            void Send(byte msgId, DataBuffer response)
            {
                using var header = Pool.Rent();
                header.FastWrite(routeName);
                header.FastWrite(routeId);
                header.Write(response.WrittenSpan);

                if (!response.SendEnabled)
                {
                    throw new Exception("Maybe you're forgetting to call Send().");
                }

                if (
                    response.DeliveryMode == DeliveryMode.Unreliable
                    || response.DeliveryMode == DeliveryMode.Sequenced
                )
                {
                    throw new NotImplementedException(
                        "HTTP Lite: Unreliable and sequenced delivery modes are not supported yet."
                    );
                }

                // Send the get response
                Server.SendMessage(msgId, peer.Id, header, Target.Self);
            }
        }

        private static void OnClientMessage(byte msgId, DataBuffer buffer, int sequenceChannel)
        {
            buffer.ResetReadPosition();
            if (
                msgId == MessageType.HttpGetResponseAsync
                || msgId == MessageType.HttpPostResponseAsync
            )
            {
                string routeName = buffer.ReadString();
                int routeId = buffer.Read<int>();

                if (Fetch.asyncTasks.Remove(routeId, out TaskCompletionSource<DataBuffer> source))
                {
                    var message = Pool.Rent(); // Disposed by the caller!
                    message.Write(buffer.GetSpan());
                    message.ResetWrittenCount();

                    // Set task as completed
                    source.TrySetResult(message);
                }
            }
        }
    }
}
