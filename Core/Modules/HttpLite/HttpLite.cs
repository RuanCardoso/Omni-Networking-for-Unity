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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Omni.Shared;

namespace Omni.Core
{
    public static class HttpLite
    {
        public static HttpServer Http { get; } = new HttpServer();
        public static HttpClient Fetch { get; } = new HttpClient();

        internal class RuntimeHttpServer
        {
            internal RuntimeHttpServer(
                Action<DataBuffer, DataBuffer, NetworkPeer> func,
                Func<DataBuffer, DataBuffer, NetworkPeer, Task> funcAsync,
                bool isAsync
            )
            {
                Func = func;
                FuncAsync = funcAsync;
                IsAsync = isAsync;
            }

            internal bool IsAsync { get; }
            internal Action<DataBuffer, DataBuffer, NetworkPeer> Func { get; }
            internal Func<DataBuffer, DataBuffer, NetworkPeer, Task> FuncAsync { get; }
        }

        public class HttpServer
        {
            internal Dictionary<string, RuntimeHttpServer> m_Routes = new();

            public void Post(string route, Action<DataBuffer, DataBuffer, NetworkPeer> res)
            {
                if (res == null)
                {
                    throw new ArgumentNullException(
                        nameof(res),
                        "The request or response is null. Please ensure valid instances of request and response are provided."
                    );
                }

                if (!m_Routes.TryAdd(route, new RuntimeHttpServer(res, default, false)))
                {
                    throw new NotSupportedException(
                        $"The route '{route}' is global and must be unique. Please make sure to provide a unique route name."
                    );
                }
            }

            public void Get(string route, Action<DataBuffer, DataBuffer, NetworkPeer> res)
            {
                Post(route, res);
            }

            public void PostAsync(string route, Func<DataBuffer, DataBuffer, NetworkPeer, Task> res)
            {
                if (res == null)
                {
                    throw new ArgumentNullException(
                        nameof(res),
                        "The request or response is null. Please ensure valid instances of request and response are provided."
                    );
                }

                if (!m_Routes.TryAdd(route, new RuntimeHttpServer(default, res, true)))
                {
                    throw new NotSupportedException(
                        $"The route '{route}' is global and must be unique. Please make sure to provide a unique route name."
                    );
                }
            }

            public void GetAsync(
                string route,
                Func<DataBuffer, DataBuffer, NetworkPeer, Task> resAsync
            )
            {
                PostAsync(route, resAsync);
            }
        }

        public class HttpClient
        {
            internal int m_RequestId = int.MinValue;
            internal Dictionary<int, Action<DataBuffer>> m_Results = new();

            public void Post(
                string route,
                Action<DataBuffer> req,
                Action<DataBuffer> res,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                if (req == null || res == null)
                {
                    throw new ArgumentNullException(
                        nameof(res),
                        "The request or response is null. Please ensure valid instances of request and response are provided."
                    );
                }

                if (deliveryMode == DeliveryMode.Unreliable)
                {
                    throw new NotSupportedException(
                        "The 'Unreliable' data delivery mode is not supported for GET and POST requests, as reliability must be guaranteed. Please choose a supported delivery mode."
                    );
                }

                int requestId = m_RequestId;
                if (m_Results.TryAdd(requestId, res))
                {
                    using var message = NetworkManager.Pool.Rent();
                    message.Write7BitEncodedInt(requestId);
                    message.FastWrite(route);
                    req(message);

                    // Send the fetch request to the server
                    NetworkManager.Client.SendMessage(
                        MessageType.HttpFetch,
                        message,
                        deliveryMode,
                        sequenceChannel
                    );

                    m_RequestId++;
                }
                else
                {
                    throw new NotSupportedException(
                        $"The request ID '{requestId}' is already in use."
                    );
                }
            }

            public void Get(
                string route,
                Action<DataBuffer> response,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                Post(route, (_) => { }, response, deliveryMode, sequenceChannel);
            }

            public Task<DataBuffer> PostAsync(
                string route,
                Action<DataBuffer> request,
                int timeout = 3000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                TaskCompletionSource<DataBuffer> tcs = new();
                CancellationTokenSource cts = new();

                Fetch.Post(
                    route,
                    request,
                    (res) =>
                    {
                        if (cts != null && !cts.IsCancellationRequested)
                        {
                            cts.Cancel();
                            tcs.SetResult(res);
                            cts.Dispose();
                        }
                    },
                    deliveryMode,
                    sequenceChannel
                );

                Task.Run(
                    async () =>
                    {
                        await Task.Delay(timeout, cts.Token);
                        if (cts != null && !cts.IsCancellationRequested)
                        {
                            cts.Cancel();
                            tcs.SetException(new TimeoutException("The request timed out."));
                            cts.Dispose();
                        }
                    },
                    cts.Token
                );
                return tcs.Task;
            }

            public Task<DataBuffer> GetAsync(
                string route,
                int timeout = 3000,
                DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
                byte sequenceChannel = 0
            )
            {
                return PostAsync(route, (_) => { }, timeout, deliveryMode, sequenceChannel);
            }
        }

        internal static void Initialize()
        {
            NetworkManager.Server.OnMessage += OnServerRoute;
            NetworkManager.Client.OnMessage += OnClientRoute;
        }

        private static void OnClientRoute(byte msgId, DataBuffer buffer, int sequenceChannel)
        {
            buffer.ResetReadPosition();
            if (msgId == MessageType.HttpResponse)
            {
                int requestId = buffer.Read7BitEncodedInt();
                string route = buffer.ReadString();

                if (Fetch.m_Results.Remove(requestId, out var func))
                {
                    func(buffer);
                }
                else
                {
                    throw new Exception($"Request ID '{requestId}' does not exist.");
                }
            }
        }

        private static async void OnServerRoute(
            byte msgId,
            DataBuffer buffer,
            NetworkPeer peer,
            int sequenceChannel
        )
        {
            buffer.ResetReadPosition();
            if (msgId == MessageType.HttpFetch)
            {
                int requestId = buffer.Read7BitEncodedInt();
                string route = buffer.ReadString();

                if (Http.m_Routes.TryGetValue(route, out RuntimeHttpServer runtime))
                {
                    #region Resources

                    using DataBuffer httpReq = NetworkManager.Pool.Rent();
                    using DataBuffer httpRes = NetworkManager.Pool.Rent();
                    using DataBuffer httpfData = NetworkManager.Pool.Rent();

                    #endregion

                    #region Initialize

                    httpReq.Write(buffer.GetSpan()); // Copy because buffer will be disposed & not awaited
                    httpReq.ResetWrittenCount();

                    httpfData.Write7BitEncodedInt(requestId);
                    httpfData.FastWrite(route);
                    if (runtime.IsAsync)
                    {
                        await runtime.FuncAsync(httpReq, httpRes, peer);
                    }
                    else
                    {
                        runtime.Func(httpReq, httpRes, peer);
                    }

                    httpfData.Write(httpRes.WrittenSpan);
                    if (httpfData.WrittenCount > 0)
                    {
                        if (httpRes.DeliveryMode == DeliveryMode.Unreliable)
                        {
                            throw new Exception("Maybe you're forgetting to call Send().");
                        }

                        NetworkManager.Server.SendMessage(
                            MessageType.HttpResponse,
                            peer.Id,
                            httpfData,
                            Target.Self,
                            httpRes.DeliveryMode,
                            httpRes.GroupId,
                            httpRes.CacheId,
                            httpRes.CacheMode,
                            httpRes.SequenceChannel
                        );
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            $"The server successfully received the requested route ({route}), but the response returned empty.",
                            NetworkLogger.LogType.Error
                        );
                    }

                    #endregion
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"The route {route} does not exists. Ensure that the route exists.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
        }
    }
}
