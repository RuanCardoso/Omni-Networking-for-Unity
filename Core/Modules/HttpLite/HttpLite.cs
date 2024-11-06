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
	/// The HttpLite class serves as a container for HTTP simulation functionalities.
	/// It provides an inner implementation, which is responsible for simulating
	/// HTTP GET and POST requests similar to Express.js through the transporter(Sockets).
	/// </summary>
	public static class HttpLite
	{
		public class HttpFetch
		{
			private int m_RouteId = 1;
			internal readonly Dictionary<int, UniTaskCompletionSource<DataBuffer>> m_Tasks = new();
			internal readonly Dictionary<(string, int), Action<DataBuffer>> m_Events = new(); // 0: Get, 1: Post

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
				if (!m_Events.TryAdd(routeKey, callback))
				{
					m_Events[routeKey] = callback;
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
				if (!m_Events.TryAdd((routeName, 1), callback))
				{
					m_Events[routeKey] = callback;
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
				int lastId = m_RouteId;
				using DataBuffer message = DefaultHeader(routeName, lastId);
				m_RouteId++;

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

				if (UseSecureHttpLite)
				{
					message.EncryptRaw(SharedPeer);
				}

				int lastId = m_RouteId;
				using DataBuffer header = DefaultHeader(routeName, lastId);
				header.Write(message.BufferAsSpan);

				// Next request id
				m_RouteId++;

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

				if (UseSecureHttpLite)
				{
					message.EncryptRaw(SharedPeer);
				}

				int lastId = m_RouteId;
				using var header = DefaultHeader(routeName, lastId);
				header.Write(message.BufferAsSpan);

				// Next request id
				m_RouteId++;

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

		public class HttpExpress
		{
			internal readonly Dictionary<string, Func<DataBuffer, NetworkPeer, UniTask>> m_g_Tasks =
				new(); // Get tasks async

			internal readonly Dictionary<
				string,
				Func<DataBuffer, DataBuffer, NetworkPeer, UniTask>
			> m_p_Tasks = new(); // Post tasks async

			internal readonly Dictionary<string, Action<DataBuffer, NetworkPeer>> m_Tasks = new(); // get tasks

			internal readonly Dictionary<
				string,
				Action<DataBuffer, DataBuffer, NetworkPeer>
			> m_a_Tasks = new(); // post tasks

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
				if (!m_g_Tasks.TryAdd(routeName, callback) || m_Tasks.ContainsKey(routeName))
				{
					m_g_Tasks[routeName] = callback;
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
			public void PostAsync(
				string routeName,
				Func<DataBuffer, DataBuffer, NetworkPeer, UniTask> callback
			)
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
			public void PostAsync(
				string routeName,
				Action<DataBuffer, DataBuffer, NetworkPeer> callback
			)
			{
				if (!m_a_Tasks.TryAdd(routeName, callback) || m_p_Tasks.ContainsKey(routeName))
				{
					m_a_Tasks[routeName] = callback;
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
			int routeId = buffer.Internal_Read();
			if (msgId == MessageType.HttpGetFetchAsync)
			{
				if (
					Http.m_g_Tasks.TryGetValue(
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
					Http.m_Tasks.TryGetValue(
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
					Http.m_p_Tasks.TryGetValue(
						routeName,
						out Func<DataBuffer, DataBuffer, NetworkPeer, UniTask> asyncCallback
					)
				)
				{
					using var request = Pool.Rent();
					request.SuppressTracking();
					request.Write(buffer.Internal_GetSpan(buffer.Length));
					request.SeekToBegin();

					if (UseSecureHttpLite)
					{
						request.DecryptRaw(SharedPeer);
					}

					using var response = Pool.Rent();
					response.SuppressTracking();

					await asyncCallback(request, response, peer);
					Send(MessageType.HttpPostResponseAsync, response);
				}
				else if (
					Http.m_a_Tasks.TryGetValue(
						routeName,
						out Action<DataBuffer, DataBuffer, NetworkPeer> callback
					)
				)
				{
					using var request = Pool.Rent();
					request.Write(buffer.Internal_GetSpan(buffer.Length));
					request.SeekToBegin();

					if (UseSecureHttpLite)
					{
						request.DecryptRaw(SharedPeer);
					}

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
				if (UseSecureHttpLite)
				{
					// Let's just encrypt the response without including the header.
					response.EncryptRaw(SharedPeer);
				}

				using var header = Pool.Rent();
				header.WriteString(routeName);
				header.Internal_Write(routeId);
				header.Write(response.BufferAsSpan);

				if (!response.SendEnabled)
				{
					NetworkLogger.__Log__(
						$"Http Lite: Maybe you're forgetting to call Send(). Ensure that you call Send() before sending the response -> Route: '{routeName}'",
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
					response.DataCache,
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

				// Send the response, except for Self
				if (target != Target.Self)
				{
					Server.SendMessage(
						msgId,
						peer,
						header,
						target,
						response.DeliveryMode,
						response.GroupId,
						response.DataCache,
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
				int routeId = buffer.Internal_Read();

				if (Fetch.m_Tasks.Remove(routeId, out UniTaskCompletionSource<DataBuffer> source))
				{
					var message = Pool.Rent(); // Disposed by the caller!
					message.Write(buffer.Internal_GetSpan(buffer.Length));
					message.SeekToBegin();

					// Set task as completed
					if (UseSecureHttpLite)
					{
						message.DecryptRaw(SharedPeer);
					}
					source.TrySetResult(message);
				}
				else
				{
					using var eventMessage = Pool.Rent();
					eventMessage.Write(buffer.Internal_GetSpan(buffer.Length));
					eventMessage.SeekToBegin();

					if (UseSecureHttpLite)
					{
						eventMessage.DecryptRaw(SharedPeer);
					}

					if (msgId == MessageType.HttpGetResponseAsync)
					{
						if (
							Fetch.m_Events.TryGetValue(
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
							Fetch.m_Events.TryGetValue(
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
