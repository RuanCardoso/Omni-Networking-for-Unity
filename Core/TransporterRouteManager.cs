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

using Omni.Shared;
using Omni.Threading.Tasks;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
    /// <summary>
    /// This class is responsible for managing routes on both the client and server sides.
    /// It provides an interface for registering GET and POST routes, and for sending and receiving data
    /// through these routes.
    /// </summary>
    public class TransporterRouteManager
    {
        public class ClientRouteManager
        {
            private int m_RouteId = 1;
            internal readonly Dictionary<int, UniTaskCompletionSource<DataBuffer>> m_Tasks = new();
            internal readonly Dictionary<(string, int), Action<DataBuffer>> m_Events = new(); // 0: Get, 1: Post

            /// <summary>
            /// Registers a GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received. This function is invoked by the system when a client receives a GET request matching this route from other clients.</param>
            /// <remarks>
            /// This method registers the specified callback function for execution by the system when a GET request matching the provided route name is received from clients other than the one making the request. It does not trigger the callback directly upon registration.
            /// </remarks>
            public void AddGetHandler(string routeName, Action<DataBuffer> callback)
            {
                var routeKey = (routeName, 0);
                if (!m_Events.TryAdd(routeKey, callback))
                {
                    m_Events[routeKey] = callback;
                }
            }

            /// <summary>
            /// Registers a POST route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered for the POST request.</param>
            /// <param name="callback">The callback function to be executed when the POST request is received. This function is invoked by the system when a client receives a POST request matching this route from other clients.</param>
            /// <remarks>
            /// This method registers the specified callback function for execution by the system when a POST request matching the provided route name is received from clients other than the one making the request. It does not trigger the callback directly upon registration.
            /// </remarks>
            public void AddPostHandler(string routeName, Action<DataBuffer> callback)
            {
                var routeKey = (routeName, 1);
                if (!m_Events.TryAdd((routeName, 1), callback))
                {
                    m_Events[routeKey] = callback;
                }
            }

            /// <summary>
            /// Asynchronously sends a GET request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the GET request is sent.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server. The caller must ensure the buffer is disposed or used within a using statement.</returns>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public UniTask<DataBuffer> GetAsync(string routeName, int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                int lastId = m_RouteId;
                using DataBuffer message = DefaultHeader(routeName, lastId);
                m_RouteId++;

                return Send(MessageType.GetFetchAsync, message, timeout, deliveryMode, lastId, sequenceChannel);
            }

            /// <summary>
            /// Sends a POST request to the specified route and processes the response using the provided callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to which the POST request should be sent.</param>
            /// <param name="req">A callback function that writes to the DataBuffer which represents the data to be sent with the POST request.</param>
            /// <param name="res">A callback function that processes the DataBuffer received in the response.</param>
            /// <param name="timeout">The maximum amount of time, in milliseconds, to wait for a response before throwing a TimeoutException. Default is 5000 milliseconds.</param>
            /// <param name="deliveryMode">The mode in which the message is delivered, with the default being ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for managing message order, with the default value being 0.</param>
            /// <exception cref="TimeoutException">Thrown if the request exceeds the specified timeout duration without receiving a response.</exception>
            public async void Post(string routeName, Action<DataBuffer> req, Action<DataBuffer> res, int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                using var buffer = await PostAsync(routeName, req, timeout, deliveryMode, sequenceChannel);
                res(buffer);
            }

            /// <summary>
            /// Sends a GET request to the specified route and processes the response using the provided callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to which the GET request is sent.</param>
            /// <param name="res">The callback function to execute with the response data buffer upon successful receipt of response.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public async void Get(string routeName, Action<DataBuffer> res, int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                using var buffer = await GetAsync(routeName, timeout, deliveryMode, sequenceChannel);
                res(buffer);
            }

            /// <summary>
            /// Asynchronously sends a POST request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the POST request is sent.</param>
            /// <param name="callback">A callback function that processes the DataBuffer before sending the request.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. The default is 5000 milliseconds.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. The default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. The default is 0.</param>
            /// <returns>A Task representing the asynchronous operation. The task result contains the data buffer received from the server.</returns>
            /// <exception cref="TimeoutException">Thrown if the request times out.</exception>
            public async UniTask<DataBuffer> PostAsync(string routeName, Func<DataBuffer, UniTask> callback,
                int timeout = 5000, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                // Await written the data before sending!
                using var message = Pool.Rent();
                message.SuppressTracking();
                await callback(message);

                if (UseSecureRoutes)
                {
                    message.EncryptInPlace(SharedPeer);
                }

                int lastId = m_RouteId;
                using DataBuffer header = DefaultHeader(routeName, lastId);
                header.Write(message.BufferAsSpan);

                // Next request id
                m_RouteId++;

                return await Send(MessageType.PostFetchAsync, header, timeout, deliveryMode, lastId, sequenceChannel);
            }

            /// <summary>
            /// Asynchronously sends an POST request to the specified route.
            /// </summary>
            /// <param name="routeName">The name of the route to which the POST request is sent.</param>
            /// <param name="callback">A callback function that processes(writes) the DataBuffer before sending the request.</param>
            /// <param name="timeout">The maximum time to wait for a response, in milliseconds. Default is 5000ms.</param>
            /// <param name="deliveryMode">The mode of delivery for the message. Default is ReliableOrdered.</param>
            /// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
            /// <returns>A Task that represents the asynchronous operation. The task result contains the data buffer received from the server. The caller must ensure the buffer is disposed or used within a using statement.</returns>
            /// <exception cref="TimeoutException">Thrown when the request times out.</exception>
            public UniTask<DataBuffer> PostAsync(string routeName, Action<DataBuffer> callback, int timeout = 5000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
            {
                using var message = Pool.Rent();
                callback(message);

                if (UseSecureRoutes)
                {
                    message.EncryptInPlace(SharedPeer);
                }

                int lastId = m_RouteId;
                using var header = DefaultHeader(routeName, lastId);
                header.Write(message.BufferAsSpan);

                // Next request id
                m_RouteId++;

                return Send(MessageType.PostFetchAsync, header, timeout, deliveryMode, lastId, sequenceChannel);
            }

            private UniTask<DataBuffer> Send(byte msgId, DataBuffer message, int timeout, DeliveryMode deliveryMode,
                int lastId, byte sequenceChannel)
            {
                ClientSide.SendMessage(msgId, message, deliveryMode, sequenceChannel);
                UniTaskCompletionSource<DataBuffer> source = CreateTask(timeout);
                m_Tasks.Add(lastId, source);
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
                message.Internal_Write(lastId);
                return message;
            }
        }

        public class ServerRouteManager
        {
            // Get tasks async
            internal readonly Dictionary<string, Func<DataBuffer, NetworkPeer, UniTask>> m_g_Tasks = new();

            // Post tasks async
            internal readonly Dictionary<string, Func<DataBuffer, DataBuffer, NetworkPeer, UniTask>> m_p_Tasks = new();

            // get tasks
            internal readonly Dictionary<string, Action<DataBuffer, NetworkPeer>> m_Tasks = new();

            // post tasks
            internal readonly Dictionary<string, Action<DataBuffer, DataBuffer, NetworkPeer>> m_a_Tasks = new();

            /// <summary>
            /// Registers a POST route and its associated callback function.
            /// </summary>
            /// <param name="route">The name of the route to be registered.</param>
            /// <param name="res">The callback function to be executed when the POST request is received, which processes the request and response data along with the network peer information.</param>
            public void Post(string route, Action<DataBuffer, DataBuffer, NetworkPeer> res)
            {
                PostAsync(route, res);
            }

            /// <summary>
            /// Registers a POST route and its associated callback function.
            /// </summary>
            /// <param name="route">The name of the route to be registered.</param>
            /// <param name="res">The callback function to be executed when the POST request is received, which takes a request buffer, response buffer, and the network peer.</param>
            public void Post(string route, Action<DataBuffer, DataBuffer> res)
            {
                PostAsync(route, (_req, _res, _peer) => res(_req, _res));
            }

            /// <summary>
            /// Registers a GET route and its associated callback function.
            /// </summary>
            /// <param name="route">The name of the route to be registered.</param>
            /// <param name="res">The callback function to be executed when a GET request is received for this route.</param>
            public void Get(string route, Action<DataBuffer, NetworkPeer> res)
            {
                GetAsync(route, res);
            }

            /// <summary>
            /// Registers a GET route and its associated callback function.
            /// </summary>
            /// <param name="route">The route to be registered for handling GET requests.</param>
            /// <param name="res">The callback function to be executed when data is received for the specified route.</param>
            public void Get(string route, Action<DataBuffer> res)
            {
                GetAsync(route, (_res, _peer) => res(_res));
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received, which includes both the received data and the network peer information.</param>
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
                if (!m_g_Tasks.TryAdd(routeName, callback) || m_Tasks.ContainsKey(routeName))
                {
                    m_g_Tasks[routeName] = callback;
                }
            }

            /// <summary>
            /// Registers an asynchronous GET route and its associated callback function.
            /// </summary>
            /// <param name="routeName">The name of the route to be registered.</param>
            /// <param name="callback">The callback function to be executed when the GET request is received, involving asynchronous processing.</param>
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
                if (!m_Tasks.TryAdd(routeName, callback) || m_g_Tasks.ContainsKey(routeName))
                {
                    m_Tasks[routeName] = callback;
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
            public void PostAsync(string routeName, Func<DataBuffer, DataBuffer, NetworkPeer, UniTask> callback)
            {
                if (!m_p_Tasks.TryAdd(routeName, callback) || m_a_Tasks.ContainsKey(routeName))
                {
                    m_p_Tasks[routeName] = callback;
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
            public void PostAsync(string routeName, Action<DataBuffer, DataBuffer, NetworkPeer> callback)
            {
                if (!m_a_Tasks.TryAdd(routeName, callback) || m_p_Tasks.ContainsKey(routeName))
                {
                    m_a_Tasks[routeName] = callback;
                }
            }
        }

        /// <summary>
        /// Provides methods to simulate GET and POST requests on the client side.
        /// </summary>
        public ClientRouteManager Client { get; } = new();

        /// <summary>
        /// Handles asynchronous GET and POST requests by maintaining lists of routes on the server side.
        /// and their associated callback functions, simulating an Express.js-like behavior.
        /// </summary>
        public ServerRouteManager Server { get; } = new();

        internal void Initialize()
        {
            ClientSide.OnMessage += OnClientMessage;
            ServerSide.OnMessage += OnServerMessage;

            NetworkService.Register(this);
        }

        private async void OnServerMessage(byte msgId, DataBuffer buffer, NetworkPeer peer, int sequenceChannel)
        {
            buffer.SeekToBegin();
            string routeName = buffer.ReadString();
            int routeId = buffer.Internal_Read();
            if (msgId == MessageType.GetFetchAsync)
            {
                if (Server.m_g_Tasks.TryGetValue(routeName, out Func<DataBuffer, NetworkPeer, UniTask> asyncCallback))
                {
                    try
                    {
                        using var response = Pool.Rent();
                        response.SuppressTracking();
                        await asyncCallback(response, peer);
                        Send(MessageType.GetResponseAsync, response);
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.PrintHyperlink(ex);
                        throw;
                    }
                }
                else if (Server.m_Tasks.TryGetValue(routeName, out Action<DataBuffer, NetworkPeer> callback))
                {
                    try
                    {
                        using var response = Pool.Rent();
                        callback(response, peer);
                        Send(MessageType.GetResponseAsync, response);
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.PrintHyperlink(ex);
                        throw;
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
            else if (msgId == MessageType.PostFetchAsync)
            {
                if (Server.m_p_Tasks.TryGetValue(routeName,
                        out Func<DataBuffer, DataBuffer, NetworkPeer, UniTask> asyncCallback))
                {
                    using var request = Pool.Rent();
                    request.SuppressTracking();
                    request.Write(buffer.Internal_GetSpan(buffer.Length));
                    request.SeekToBegin();

                    if (UseSecureRoutes)
                    {
                        request.DecryptInPlace(SharedPeer);
                    }

                    try
                    {
                        using var response = Pool.Rent();
                        response.SuppressTracking();

                        await asyncCallback(request, response, peer);
                        Send(MessageType.PostResponseAsync, response);
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.PrintHyperlink(ex);
                        throw;
                    }
                }
                else if (Server.m_a_Tasks.TryGetValue(routeName,
                             out Action<DataBuffer, DataBuffer, NetworkPeer> callback))
                {
                    using var request = Pool.Rent();
                    request.Write(buffer.Internal_GetSpan(buffer.Length));
                    request.SeekToBegin();

                    if (UseSecureRoutes)
                    {
                        request.DecryptInPlace(SharedPeer);
                    }

                    try
                    {
                        using var response = Pool.Rent();
                        callback(request, response, peer);
                        Send(MessageType.PostResponseAsync, response);
                    }
                    catch (Exception ex)
                    {
                        NetworkLogger.PrintHyperlink(ex);
                        throw;
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

            void Send(byte msgId, DataBuffer response)
            {
                if (UseSecureRoutes)
                {
                    // Let's just encrypt the response without including the header.
                    response.EncryptInPlace(SharedPeer);
                }

                using var header = Pool.Rent();
                header.WriteString(routeName);
                header.Internal_Write(routeId);
                header.Write(response.BufferAsSpan);

                if (!response.SendEnabled)
                {
                    NetworkLogger.__Log__(
                        $"Routes: Maybe you're forgetting to call Send(). Ensure that you call Send() before sending the response -> Route: '{routeName}'",
                        NetworkLogger.LogType.Error
                    );

                    return;
                }

                // Self:
                ServerSide.SendMessage(msgId, peer, header, Target.SelfOnly, response.DeliveryMode, 0,
                    response.DataCache, response.SequenceChannel);

                Target target = response.Target switch
                {
                    RouteTarget.SelfOnly => Target.SelfOnly,
                    RouteTarget.AllPlayers => Target.AllPlayersExceptSelf,
                    RouteTarget.GroupOnly => Target.GroupExceptSelf,
                    RouteTarget.UngroupedPlayers => Target.UngroupedPlayersExceptSelf,
                    _ => Target.SelfOnly,
                };

                // Send the response, except for Self
                if (target != Target.SelfOnly)
                {
                    ServerSide.SendMessage(msgId, peer, header, target, response.DeliveryMode, response.GroupId,
                        response.DataCache, response.SequenceChannel);
                }
            }
        }

        private void OnClientMessage(byte msgId, DataBuffer buffer, int sequenceChannel)
        {
            buffer.SeekToBegin();
            if (msgId == MessageType.GetResponseAsync || msgId == MessageType.PostResponseAsync)
            {
                string routeName = buffer.ReadString();
                int routeId = buffer.Internal_Read();

                if (Client.m_Tasks.Remove(routeId, out UniTaskCompletionSource<DataBuffer> source))
                {
                    var message = Pool.Rent(); // Disposed by the caller!
                    message.Write(buffer.Internal_GetSpan(buffer.Length));
                    message.SeekToBegin();

                    // Set task as completed
                    if (UseSecureRoutes)
                    {
                        message.DecryptInPlace(SharedPeer);
                    }

                    source.TrySetResult(message);
                }
                else
                {
                    using var eventMessage = Pool.Rent();
                    eventMessage.Write(buffer.Internal_GetSpan(buffer.Length));
                    eventMessage.SeekToBegin();

                    if (UseSecureRoutes)
                    {
                        eventMessage.DecryptInPlace(SharedPeer);
                    }

                    if (msgId == MessageType.GetResponseAsync)
                    {
                        if (Client.m_Events.TryGetValue((routeName, 0), out Action<DataBuffer> callback))
                        {
                            try
                            {
                                callback?.Invoke(eventMessage);
                            }
                            catch (Exception ex)
                            {
                                NetworkLogger.PrintHyperlink(ex);
                                throw;
                            }
                        }
                    }
                    else if (msgId == MessageType.PostResponseAsync)
                    {
                        if (Client.m_Events.TryGetValue((routeName, 1), out Action<DataBuffer> callback))
                        {
                            try
                            {
                                callback?.Invoke(eventMessage);
                            }
                            catch (Exception ex)
                            {
                                NetworkLogger.PrintHyperlink(ex);
                                throw;
                            }
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