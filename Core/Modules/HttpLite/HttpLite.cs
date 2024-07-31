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
using Omni.Shared;
using Omni.Threading.Tasks;
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
            internal readonly Dictionary<int, UniTaskCompletionSource<DataBuffer>> asyncTasks =
                new();
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
                var routeKey = (routeName, 0);
                if (!events.TryAdd(routeKey, callback))
                {
                    events[routeKey] = callback;
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
                var routeKey = (routeName, 1);
                if (!events.TryAdd((routeName, 1), callback))
                {
                    events[routeKey] = callback;
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
            public UniTask<DataBuffer> GetAsync(
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
            public async UniTask<DataBuffer> PostAsync(
                string routeName,
                Func<DataBuffer, UniTask> callback,
                int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                // Await written the data before sending!
                using var message = Pool.Rent();
                message.SuppressTracking();
                await callback(message);

                int lastId = routeId;
                using DataBuffer header = DefaultHeader(routeName, lastId);
                header.Write(message.BufferAsSpan);

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
            public UniTask<DataBuffer> PostAsync(
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
                header.Write(message.BufferAsSpan);

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

            private UniTask<DataBuffer> Send(
                byte msgId,
                DataBuffer message,
                int timeout,
                DeliveryMode deliveryMode,
                int lastId,
                byte sequenceChannel
            )
            {
                Client.SendMessage(msgId, message, deliveryMode, sequenceChannel);

                UniTaskCompletionSource<DataBuffer> source = CreateTask(timeout);
                asyncTasks.Add(lastId, source);
                return source.Task;
            }

            private UniTaskCompletionSource<DataBuffer> CreateTask(int timeout)
            {
                UniTaskCompletionSource<DataBuffer> source = new();
                CancellationTokenSource cts = new(timeout);

                // Register a callback to handle the cancellation of the request.
                cts.Token.Register(() =>
                {
                    bool success = source.Task.Status.IsCompletedSuccessfully();
                    if (!success)
                    {
                        NetworkLogger.__Log__(
                            $"The request has timed out. Ensure that the route exists and that the server is running or the request will fail.",
                            NetworkLogger.LogType.Error
                        );

                        cts.Cancel();
                        cts.Dispose();
                        source.TrySetCanceled();
                        return;
                    }

                    cts.Cancel();
                    cts.Dispose();
                });

                return source;
            }

            private DataBuffer DefaultHeader(string routeName, int lastId)
            {
                var message = Pool.Rent(); // disposed by the caller
                message.WriteString(routeName);
                message.Write(lastId);
                return message;
            }
        }

        public class HttpExpress
        {
            internal readonly Dictionary<
                string,
                Func<DataBuffer, NetworkPeer, UniTask>
            > asyncGetTasks = new();

            internal readonly Dictionary<
                string,
                Func<DataBuffer, DataBuffer, NetworkPeer, UniTask>
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
            /// Registers an POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.</param>
            public void Post(string route, Action<DataBuffer, DataBuffer> res)
            {
                PostAsync(route, (_req, _res, _peer) => res(_req, _res));
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
            /// Registers an GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void Get(string route, Action<DataBuffer> res)
            {
                GetAsync(route, (_res, _peer) => res(_res));
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void GetAsync(string routeName, Func<DataBuffer, UniTask> callback)
            {
                GetAsync(routeName, (_res, _peer) => callback(_res));
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void GetAsync(string routeName, Func<DataBuffer, NetworkPeer, UniTask> callback)
            {
                if (!asyncGetTasks.TryAdd(routeName, callback) || getTasks.ContainsKey(routeName))
                {
                    asyncGetTasks[routeName] = callback;
                }
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received.</param>
            public void GetAsync(string routeName, Action<DataBuffer> callback)
            {
                GetAsync(routeName, (_res, _peer) => callback(_res));
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
                    getTasks[routeName] = callback;
                }
            }

            /// <summary>
            /// Registers an asynchronous POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.</param>
            public void PostAsync(string routeName, Func<DataBuffer, DataBuffer, UniTask> callback)
            {
                PostAsync(routeName, (_req, _res, _peer) => callback(_req, _res));
            }

            /// <summary>
            /// Registers an asynchronous POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.</param>
            public void PostAsync(
                string routeName,
                Func<DataBuffer, DataBuffer, NetworkPeer, UniTask> callback
            )
            {
                if (!asyncPostTasks.TryAdd(routeName, callback) || postTasks.ContainsKey(routeName))
                {
                    asyncPostTasks[routeName] = callback;
                }
            }

            /// <summary>
            /// Registers an asynchronous POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received.</param>
            public void PostAsync(string routeName, Action<DataBuffer, DataBuffer> callback)
            {
                PostAsync(routeName, (_req, _res, _peer) => callback(_req, _res));
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
                    postTasks[routeName] = callback;
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
            buffer.SeekToBegin();
            string routeName = buffer.ReadString();
            int routeId = buffer.Read<int>();
            if (msgId == MessageType.HttpGetFetchAsync)
            {
                if (
                    Http.asyncGetTasks.TryGetValue(
                        routeName,
                        out Func<DataBuffer, NetworkPeer, UniTask> asyncCallback
                    )
                )
                {
                    using var response = Pool.Rent();
                    response.SuppressTracking();
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
                    NetworkLogger.__Log__(
                        $"The route {routeName} has not been registered. Ensure that you register it first.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
            else if (msgId == MessageType.HttpPostFetchAsync)
            {
                if (
                    Http.asyncPostTasks.TryGetValue(
                        routeName,
                        out Func<DataBuffer, DataBuffer, NetworkPeer, UniTask> asyncCallback
                    )
                )
                {
                    using var request = Pool.Rent();
                    request.SuppressTracking();
                    request.Write(buffer.Internal_GetSpan(buffer.Length));
                    request.SeekToBegin();

                    using var response = Pool.Rent();
                    response.SuppressTracking();
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
                    request.Write(buffer.Internal_GetSpan(buffer.Length));
                    request.SeekToBegin();

                    using var response = Pool.Rent();
                    callback(request, response, peer);
                    Send(MessageType.HttpPostResponseAsync, response);
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"The route {routeName} has not been registered. Ensure that you register it first.",
                        NetworkLogger.LogType.Error
                    );
                }
            }

            void Send(byte msgId, DataBuffer response)
            {
                using var header = Pool.Rent();
                header.WriteString(routeName);
                header.Write(routeId);
                header.Write(response.BufferAsSpan);

                if (!response.SendEnabled)
                {
                    NetworkLogger.__Log__(
                        "Http Lite: Maybe you're forgetting to call Send().",
                        NetworkLogger.LogType.Error
                    );

                    return;
                }

                // Self:
                Server.SendMessage(
                    msgId,
                    peer,
                    header,
                    Target.Self,
                    response.DeliveryMode,
                    0,
                    response.CacheId,
                    response.CacheMode,
                    response.SequenceChannel
                );

                Target target = response.Target switch
                {
                    HttpTarget.Self => Target.Self,
                    HttpTarget.All => Target.AllExceptSelf,
                    HttpTarget.GroupMembers => Target.GroupMembersExceptSelf,
                    HttpTarget.NonGroupMembers => Target.NonGroupMembersExceptSelf,
                    _ => Target.Self,
                };

                // Send the get response, except for Self
                if (target != Target.Self)
                {
                    Server.SendMessage(
                        msgId,
                        peer,
                        header,
                        target,
                        response.DeliveryMode,
                        response.GroupId,
                        response.CacheId,
                        response.CacheMode,
                        response.SequenceChannel
                    );
                }
            }
        }

        private static void OnClientMessage(byte msgId, DataBuffer buffer, int sequenceChannel)
        {
            buffer.SeekToBegin();
            if (
                msgId == MessageType.HttpGetResponseAsync
                || msgId == MessageType.HttpPostResponseAsync
            )
            {
                string routeName = buffer.ReadString();
                int routeId = buffer.Read<int>();

                if (
                    Fetch.asyncTasks.Remove(routeId, out UniTaskCompletionSource<DataBuffer> source)
                )
                {
                    var message = Pool.Rent(); // Disposed by the caller!
                    message.Write(buffer.Internal_GetSpan(buffer.Length));
                    message.SeekToBegin();

                    // Set task as completed
                    source.TrySetResult(message);
                }
                else
                {
                    using var eventMessage = Pool.Rent();
                    eventMessage.Write(buffer.Internal_GetSpan(buffer.Length));
                    eventMessage.SeekToBegin();

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
                        NetworkLogger.__Log__(
                            $"The route {routeName} has not been registered. Ensure that you register it first.",
                            NetworkLogger.LogType.Error
                        );
                    }
                }
            }
        }
    }
}
