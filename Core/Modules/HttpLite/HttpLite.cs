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
            internal readonly Dictionary<(string, int), Action<DataBuffer>> events = new(); // 0: Get, 1: Post

            /// <summary>
            /// Registers a GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.
            /// Note: The registered callback function is invoked by the system when a GET request matching
            /// this route is received from other clients, not directly from the client making the request.
            /// </param>
            /// <remarks>
            /// This method registers the specified callback function to be executed by the system when a GET request
            /// matching the provided route name is received from clients other than the one making the request.
            /// It does not trigger the callback directly upon registration.
            /// </remarks>
            public void AddGetHandler(string routeName, Action<DataBuffer> callback)
            {
                if (!events.TryAdd((routeName, 0), callback))
                {
                    throw new InvalidOperationException(
                        "Route name already exists. Ensure that the route name is unique."
                    );
                }
            }

            /// <summary>
            /// Registers a POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.
            /// Note: The registered callback function is invoked by the system when a POST request matching
            /// this route is received from other clients, not directly from the client making the request.
            /// </param>
            /// <remarks>
            /// This method registers the specified callback function to be executed by the system when a POST request
            /// matching the provided route name is received from clients other than the one making the request.
            /// It does not trigger the callback directly upon registration.
            /// </remarks>
            public void AddPostHandler(string routeName, Action<DataBuffer> callback)
            {
                if (!events.TryAdd((routeName, 1), callback))
                {
                    throw new InvalidOperationException(
                        "Route name already exists. Ensure that the route name is unique."
                    );
                }
            }

            /// <summary>
            /// Asynchronously sends an HTTP GET request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the GET request is sent.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server. The caller must ensure the buffer is disposed or used within a using statement.</returns>
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
            /// Sends an HTTP POST request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the POST request is sent.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public async void Post(
                string routeName,
                Action<DataBuffer> req,
                Action<DataBuffer> res,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                using var buffer = await PostAsync(
                    routeName,
                    req,
                    timeout,
                    deliveryMode,
                    sequenceChannel
                );

                res(buffer);
            }

            /// <summary>
            /// Asynchronously sends an HTTP GET request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the GET request is sent.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public async void Get(
                string routeName,
                Action<DataBuffer> res,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                using var buffer = await GetAsync(
                    routeName,
                    timeout,
                    deliveryMode,
                    sequenceChannel
                );

                res(buffer);
            }

            /// <summary>
            /// Asynchronously sends an HTTP POST request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the POST request is sent.</param>
            /// <param name="callback">A callback function that processes(writes) the DataBuffer before sending the request.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server. The caller must ensure the buffer is disposed or used within a using statement.</returns>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public async Task<DataBuffer> PostAsync(
                string routeName,
                Func<DataBuffer, Task> callback,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                // Await written the data before sending!
                using var message = Pool.Rent();
                await callback(message);

                int lastId = routeId;
                using DataBuffer header = DefaultHeader(routeName, lastId);
                header.Write(message.WrittenSpan);

                // Next request id
                routeId++;

                return await Send(
                    MessageType.HttpPostFetchAsync,
                    header,
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
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server. The caller must ensure the buffer is disposed or used within a using statement.</returns>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public Task<DataBuffer> PostAsync(
                string routeName,
                Action<DataBuffer> callback,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                using var message = Pool.Rent();
                callback(message);

                int lastId = routeId;
                using var header = DefaultHeader(routeName, lastId);
                header.Write(message.WrittenSpan);

                // Next request id
                routeId++;

                return Send(
                    MessageType.HttpPostFetchAsync,
                    header,
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
            /// Registers an POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.</param>
            public void Post(string route, Action<DataBuffer, DataBuffer, NetworkPeer> res)
            {
                PostAsync(route, res);
            }

            /// <summary>
            /// Registers an GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void Get(string route, Action<DataBuffer, NetworkPeer> res)
            {
                GetAsync(route, res);
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void GetAsync(string routeName, Func<DataBuffer, NetworkPeer, Task> callback)
            {
                if (!asyncGetTasks.TryAdd(routeName, callback) || getTasks.ContainsKey(routeName))
                {
                    throw new InvalidOperationException(
                        "Route name already exists. Ensure that the route name is unique."
                    );
                }
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void GetAsync(string routeName, Action<DataBuffer, NetworkPeer> callback)
            {
                if (!getTasks.TryAdd(routeName, callback) || asyncGetTasks.ContainsKey(routeName))
                {
                    throw new InvalidOperationException(
                        "Route name already exists. Ensure that the route name is unique."
                    );
                }
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
                if (!asyncPostTasks.TryAdd(routeName, callback) || postTasks.ContainsKey(routeName))
                {
                    throw new InvalidOperationException(
                        "Route name already exists. Ensure that the route name is unique."
                    );
                }
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
                if (!postTasks.TryAdd(routeName, callback) || asyncPostTasks.ContainsKey(routeName))
                {
                    throw new InvalidOperationException(
                        "Route name already exists. Ensure that the route name is unique."
                    );
                }
            }
        }

        /// <summary>
        /// Provides methods to simulate HTTP GET and POST requests on the client side.
        /// </summary>
        public static HttpFetch Fetch { get; } = new();

        /// <summary>
        /// Handles asynchronous GET and POST requests by maintaining lists of routes on the server side.
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
                else
                {
                    throw new Exception(
                        $"The route {routeName} has not been registered. Ensure that you register it first."
                    );
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
                else
                {
                    throw new Exception(
                        $"The route {routeName} has not been registered. Ensure that you register it first."
                    );
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
                    throw new Exception("Http Lite: Maybe you're forgetting to call Send().");
                }

                if (response.ForceSendToSelf)
                {
                    Server.SendMessage(
                        msgId,
                        peer.Id,
                        header,
                        Target.Self,
                        response.DeliveryMode,
                        0,
                        response.CacheId,
                        response.CacheMode,
                        response.SequenceChannel
                    );
                }

                // Send the get response
                Server.SendMessage(
                    msgId,
                    peer.Id,
                    header,
                    response.Target,
                    response.DeliveryMode,
                    response.GroupId,
                    response.CacheId,
                    response.CacheMode,
                    response.SequenceChannel
                );
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
                else
                {
                    using var eventMessage = Pool.Rent();
                    eventMessage.Write(buffer.GetSpan());
                    eventMessage.ResetWrittenCount();

                    if (msgId == MessageType.HttpGetResponseAsync)
                    {
                        if (
                            Fetch.events.TryGetValue(
                                (routeName, 0),
                                out Action<DataBuffer> callback
                            )
                        )
                        {
                            callback?.Invoke(eventMessage);
                        }
                    }
                    else if (msgId == MessageType.HttpPostResponseAsync)
                    {
                        if (
                            Fetch.events.TryGetValue(
                                (routeName, 1),
                                out Action<DataBuffer> callback
                            )
                        )
                        {
                            callback?.Invoke(eventMessage);
                        }
                    }
                    else
                    {
                        throw new Exception(
                            $"The route {routeName} has not been registered. Ensure that you register it first."
                        );
                    }
                }
            }
        }
    }
}
