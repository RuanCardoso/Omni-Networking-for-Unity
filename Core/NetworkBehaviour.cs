using Omni.Core.Interfaces;
using Omni.Core.Modules.Ntp;
using Omni.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

#pragma warning disable

namespace Omni.Core
{
	public class NetworkBehaviour
		: NetworkVariablesBehaviour,
			IInvokeMessage,
			ITickSystem,
			IEquatable<NetworkBehaviour>
	{
		// Hacky: DIRTY CODE!
		// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
		// Despite its appearance, this approach is essential to achieve high performance.
		// Avoid refactoring as these techniques are crucial for optimizing execution speed.
		// Works with il2cpp.

		public class NbClient
		{
			private readonly NetworkBehaviour m_NetworkBehaviour;

			internal NbClient(NetworkBehaviour networkBehaviour)
			{
				m_NetworkBehaviour = networkBehaviour;
			}

			/// <summary>
			/// Sends a manual 'NetworkVariable' message to the server with the specified property and property id.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="property">The property value to synchronize.</param>
			/// <param name="propertyId">The ID of the property being synchronized.</param>
			public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options)
			{
				ManualSync<T>(property, propertyId, options.DeliveryMode, options.SequenceChannel);
			}

			/// <summary>
			/// Sends a manual 'NetworkVariable' message to the server with the specified property and property id.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="property">The property value to synchronize.</param>
			/// <param name="propertyId">The ID of the property being synchronized.</param>
			/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
			/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
			public void ManualSync<T>(
				T property,
				byte propertyId,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				byte sequenceChannel = 0
			)
			{
				using DataBuffer message = m_NetworkBehaviour.CreateHeader(property, propertyId);
				Invoke(NetworkConstants.NET_VAR_RPC_ID, message, deliveryMode, sequenceChannel);
			}

			/// <summary>
			/// Automatically sends a 'NetworkVariable' message to the server based on the caller member name.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			public void AutoSync<T>(
				NetworkVariableOptions options,
				[CallerMemberName] string ___ = ""
			)
			{
				AutoSync<T>(options.DeliveryMode, options.SequenceChannel, ___);
			}

			/// <summary>
			/// Automatically sends a 'NetworkVariable' message to the server based on the caller member name.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
			/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
			public void AutoSync<T>(
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				byte sequenceChannel = 0,
				[CallerMemberName] string ___ = ""
			)
			{
				IPropertyInfo property = m_NetworkBehaviour.GetPropertyInfoWithCallerName<T>(
					___,
					m_NetworkBehaviour.m_BindingFlags
				);

				IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

				if (property != null)
				{
					using DataBuffer message = m_NetworkBehaviour.CreateHeader(
						propertyGeneric.Invoke(),
						property.Id
					);

					Invoke(NetworkConstants.NET_VAR_RPC_ID, message, deliveryMode, sequenceChannel);
				}
			}

			/// <summary>
			/// Invokes a message on the server, similar to a Remote Procedure Call (RPC).
			/// </summary>
			/// <param name="msgId">The ID of the message to be invoked.</param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke(byte msgId, SyncOptions options)
			{
				Invoke(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
			}

			/// <summary>
			/// Invokes a message on the server, similar to a Remote Procedure Call (RPC).
			/// </summary>
			/// <param name="msgId">The ID of the message to be invoked.</param>
			/// <param name="buffer">The buffer containing the message data. Default is null.</param>
			/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
			/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke(
				byte msgId,
				DataBuffer buffer = null,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				byte sequenceChannel = 0
			)
			{
				NetworkManager.Client.Invoke(
					msgId,
					m_NetworkBehaviour.IdentityId,
					m_NetworkBehaviour.Id,
					buffer,
					deliveryMode,
					sequenceChannel
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke(byte msgId, IMessage message, SyncOptions options = default)
			{
				using var _ = message.Serialize();
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1>(byte msgId, T1 p1, SyncOptions options = default)
				where T1 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				using var _ = NetworkManager.FastWrite(p1);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, SyncOptions options = default)
				where T1 : unmanaged
				where T2 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				using var _ = NetworkManager.FastWrite(p1, p2);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2, T3>(
				byte msgId,
				T1 p1,
				T2 p2,
				T3 p3,
				SyncOptions options = default
			)
				where T1 : unmanaged
				where T2 : unmanaged
				where T3 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
				using var _ = NetworkManager.FastWrite(p1, p2, p3);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2, T3, T4>(
				byte msgId,
				T1 p1,
				T2 p2,
				T3 p3,
				T4 p4,
				SyncOptions options = default
			)
				where T1 : unmanaged
				where T2 : unmanaged
				where T3 : unmanaged
				where T4 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
				using var _ = NetworkManager.FastWrite(p1, p2, p3, p4);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2, T3, T4, T5>(
				byte msgId,
				T1 p1,
				T2 p2,
				T3 p3,
				T4 p4,
				T5 p5,
				SyncOptions options = default
			)
				where T1 : unmanaged
				where T2 : unmanaged
				where T3 : unmanaged
				where T4 : unmanaged
				where T5 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p5);
				using var _ = NetworkManager.FastWrite(p1, p2, p3, p4, p5);
				options.Buffer = _;
				Invoke(msgId, options);
			}
		}

		public class NbServer
		{
			private readonly NetworkBehaviour m_NetworkBehaviour;

			internal NbServer(NetworkBehaviour networkBehaviour)
			{
				m_NetworkBehaviour = networkBehaviour;
			}

			/// <summary>
			/// Sends a manual 'NetworkVariable' message to all(default) clients with the specified property and property ID.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="property">The property value to synchronize.</param>
			/// <param name="propertyId">The ID of the property being synchronized.</param>
			public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions options)
			{
				ManualSync(
					property,
					propertyId,
					options.Target,
					options.DeliveryMode,
					options.GroupId,
					options.DataCache,
					options.SequenceChannel
				);
			}

			/// <summary>
			/// Sends a manual 'NetworkVariable' message to all(default) clients with the specified property and property ID.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="property">The property value to synchronize.</param>
			/// <param name="propertyId">The ID of the property being synchronized.</param>
			/// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
			/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
			/// <param name="groupId">The group ID for the message. Default is 0.</param>
			/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
			/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
			public void ManualSync<T>(
				T property,
				byte propertyId,
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0
			)
			{
				dataCache ??= DataCache.None;
				using DataBuffer message = m_NetworkBehaviour.CreateHeader(property, propertyId);
				Invoke(
					NetworkConstants.NET_VAR_RPC_ID,
					message,
					target,
					deliveryMode,
					groupId,
					dataCache,
					sequenceChannel
				);
			}

			/// <summary>
			/// Automatically sends a 'NetworkVariable' message to all(default) clients based on the caller member name.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			public void AutoSync<T>(
				NetworkVariableOptions options,
				[CallerMemberName] string ___ = ""
			)
			{
				AutoSync<T>(
					options.Target,
					options.DeliveryMode,
					options.GroupId,
					options.DataCache,
					options.SequenceChannel,
					___
				);
			}

			/// <summary>
			/// Automatically sends a 'NetworkVariable' message to all(default) clients based on the caller member name.
			/// </summary>
			/// <typeparam name="T">The type of the property to synchronize.</typeparam>
			/// <param name="target">The target for the message. Default is <see cref="Target.All"/>.</param>
			/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
			/// <param name="groupId">The group ID for the message. Default is 0.</param>
			/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
			/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
			public void AutoSync<T>(
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0,
				[CallerMemberName] string ___ = ""
			)
			{
				dataCache ??= DataCache.None;
				IPropertyInfo propertyInfo = m_NetworkBehaviour.GetPropertyInfoWithCallerName<T>(
					___,
					m_NetworkBehaviour.m_BindingFlags
				);

				IPropertyInfo<T> propertyInfoGeneric = propertyInfo as IPropertyInfo<T>;

				if (propertyInfo != null)
				{
					using DataBuffer message = m_NetworkBehaviour.CreateHeader(
						propertyInfoGeneric.Invoke(),
						propertyInfo.Id
					);

					Invoke(
						NetworkConstants.NET_VAR_RPC_ID,
						message,
						target,
						deliveryMode,
						groupId,
						dataCache,
						sequenceChannel
					);
				}
			}

			/// <summary>
			/// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
			/// </summary>
			/// <param name="msgId">The ID of the message to be invoked.</param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke(byte msgId, SyncOptions options)
			{
				Invoke(
					msgId,
					options.Buffer,
					options.Target,
					options.DeliveryMode,
					options.GroupId,
					options.DataCache,
					options.SequenceChannel
				);
			}

			/// <summary>
			/// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
			/// </summary>
			/// <param name="msgId">The ID of the message to be invoked.</param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void InvokeByPeer(byte msgId,
				NetworkPeer peer, SyncOptions options)
			{
				InvokeByPeer(msgId, peer, options.Buffer, options.Target, options.DeliveryMode, options.GroupId, options.DataCache, options.SequenceChannel);
			}

			/// <summary>
			/// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
			/// </summary>
			/// <param name="msgId">The ID of the message to be invoked.</param>
			/// <param name="buffer">The buffer containing the message data. Default is null.</param>
			/// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
			/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
			/// <param name="groupId">The group ID for the message. Default is 0.</param>
			/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
			/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void InvokeByPeer(
				byte msgId,
				NetworkPeer peer,
				DataBuffer buffer = null,
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0
			)
			{
				dataCache ??= DataCache.None;
				NetworkManager.Server.Invoke(
					msgId,
					peer,
					m_NetworkBehaviour.IdentityId,
					m_NetworkBehaviour.Id,
					buffer,
					target,
					deliveryMode,
					groupId,
					dataCache,
					sequenceChannel
				);
			}

			/// <summary>
			/// Invokes a message on the client, similar to a Remote Procedure Call (RPC).
			/// </summary>
			/// <param name="msgId">The ID of the message to be invoked.</param>
			/// <param name="buffer">The buffer containing the message data. Default is null.</param>
			/// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
			/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
			/// <param name="groupId">The group ID for the message. Default is 0.</param>
			/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
			/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke(
				byte msgId,
				DataBuffer buffer = null,
				Target target = Target.All,
				DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
				int groupId = 0,
				DataCache dataCache = default,
				byte sequenceChannel = 0
			)
			{
				dataCache ??= DataCache.None;
				InvokeByPeer(msgId, m_NetworkBehaviour.Identity.Owner, buffer, target, deliveryMode, groupId, dataCache, sequenceChannel);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke(byte msgId, IMessage message, SyncOptions options = default)
			{
				using var _ = message.Serialize();
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1>(byte msgId, T1 p1, SyncOptions options = default)
				where T1 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				using var _ = NetworkManager.FastWrite(p1);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2>(byte msgId, T1 p1, T2 p2, SyncOptions options = default)
				where T1 : unmanaged
				where T2 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				using var _ = NetworkManager.FastWrite(p1, p2);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2, T3>(
				byte msgId,
				T1 p1,
				T2 p2,
				T3 p3,
				SyncOptions options = default
			)
				where T1 : unmanaged
				where T2 : unmanaged
				where T3 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
				using var _ = NetworkManager.FastWrite(p1, p2, p3);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2, T3, T4>(
				byte msgId,
				T1 p1,
				T2 p2,
				T3 p3,
				T4 p4,
				SyncOptions options = default
			)
				where T1 : unmanaged
				where T2 : unmanaged
				where T3 : unmanaged
				where T4 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
				using var _ = NetworkManager.FastWrite(p1, p2, p3, p4);
				options.Buffer = _;
				Invoke(msgId, options);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Invoke<T1, T2, T3, T4, T5>(
				byte msgId,
				T1 p1,
				T2 p2,
				T3 p3,
				T4 p4,
				T5 p5,
				SyncOptions options = default
			)
				where T1 : unmanaged
				where T2 : unmanaged
				where T3 : unmanaged
				where T4 : unmanaged
				where T5 : unmanaged
			{
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p3);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p4);
				NetworkHelper.ThrowAnErrorIfIsInternalTypes(p5);
				using var _ = NetworkManager.FastWrite(p1, p2, p3, p4, p5);
				options.Buffer = _;
				Invoke(msgId, options);
			}
		}

		// Hacky: DIRTY CODE!
		// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
		// Despite its appearance, this approach is essential to achieve high performance.
		// Avoid refactoring as these techniques are crucial for optimizing execution speed.
		// Works with il2cpp.

		private readonly InvokeBehaviour<DataBuffer, int, Null, Null, Null> cInvoker = new();
		private readonly InvokeBehaviour<DataBuffer, NetworkPeer, int, Null, Null> sInvoker = new();

		[SerializeField]
		[Header("Service Settings")]
		private string m_ServiceName;

		[SerializeField]
		private byte m_Id = 0;

		[SerializeField]
		internal BindingFlags m_BindingFlags =
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		/// <summary>
		/// Gets or sets the identifier of this instance.
		/// </summary>
		/// <value>The identifier as a byte.</value>
		public byte Id
		{
			get { return m_Id; }
			internal set { m_Id = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="NetworkIdentity"/> associated with this instance.
		/// </summary>
		/// <value>The <see cref="NetworkIdentity"/> associated with this instance.</value>
		public NetworkIdentity Identity
		{
			get
			{
				if (_identity == null)
				{
					throw new InvalidOperationException(
						"The 'NetworkIdentity' property has not been assigned yet. Make sure to set it before accessing it. If you are trying to access it during the object's initialization, ensure that the object has been fully initialized before accessing it."
					);
				}

				return _identity;
			}
			internal set => _identity = value;
		}

		/// <summary>
		/// Gets the identifier of the associated <see cref="NetworkIdentity"/>.
		/// </summary>
		/// <value>The identifier of the associated <see cref="NetworkIdentity"/> as an integer.</value>
		public int IdentityId => Identity.IdentityId;

		/// <summary>
		/// Gets a value indicating whether this instance represents the local player.
		/// </summary>
		/// <value><c>true</c> if this instance represents the local player; otherwise, <c>false</c>.</value>
		public bool IsLocalPlayer => Identity.IsLocalPlayer;

		/// <summary>
		/// Gets a value indicating whether this instance represents the local player.
		/// </summary>
		/// <value><c>true</c> if this instance represents the local player; otherwise, <c>false</c>.</value>
		/// <remarks>This property is an alias for <see cref="IsLocalPlayer"/>.</remarks>
		public bool IsMine => IsLocalPlayer;

		/// <summary>
		/// Gets a value indicating whether this instance is on the server.
		/// </summary>
		/// <value><c>true</c> if this instance is on the server; otherwise, <c>false</c>.</value>
		public bool IsServer => Identity.IsServer;

		/// <summary>
		/// Gets a value indicating whether this instance is on the client.
		/// </summary>
		/// <value><c>true</c> if this instance is on the client; otherwise, <c>false</c>.</value>
		public bool IsClient => Identity.IsClient;

		/// <summary>
		/// Gets the <see cref="SimpleNtp"/> instance that provides access to the server and client time.
		/// </summary>
		/// <value>
		/// The <see cref="SimpleNtp"/> instance that provides the server and client time.
		/// </value>
		protected SimpleNtp Sntp => NetworkManager.Sntp;

		/// <summary>
		/// Gets the synchronized time between the server and the clients as a <see cref="double"/>.
		/// This property returns the current synchronized time, providing a consistent reference point across all clients and the server.
		/// Although the time is not exactly the same due to precision differences, it is synchronized to be as close as possible between the server and the clients.
		/// </summary>
		protected double SynchronizedTime => IsServer ? Sntp.Server.Time : Sntp.Client.Time;

		/// <summary>
		/// Gets the synchronized time between the client and the server as a <see cref="double"/>.
		/// This property returns the current synchronized time as perceived by the client, providing a reference point that is consistent between that client and the server.
		/// Unlike the <see cref="SynchronizedTime"/> property, <see cref="PeerTime"/> represents the time from the perspective of the individual client and may differ between clients.
		/// </summary>
		protected double PeerTime => Identity.Owner.Time;

		private NbClient _local;

		// public api: allow send from other object
		/// <summary>
		/// Gets the <see cref="NbClient"/> instance used to invoke messages on the server from the client.
		/// </summary>
		public NbClient Local
		{
			get
			{
				if (_local == null)
				{
					throw new Exception(
						"This property(Local) is intended for client-side use only. It appears to be accessed from the server side."
					);
				}

				return _local;
			}
			private set => _local = value;
		}

		private NbServer _remote;
		private NetworkIdentity _identity;

		// public api: allow send from other object
		/// <summary>
		/// Gets the <see cref="NbServer"/> instance used to invoke messages on the client from the server.
		/// </summary>
		public NbServer Remote
		{
			get
			{
				if (_remote == null)
				{
					throw new Exception(
						"This property(Remote) is intended for server-side use only. It appears to be accessed from the client side."
					);
				}

				return _remote;
			}
			private set => _remote = value;
		}

		/// <summary>
		/// Called after the object is instantiated and registered, but before it is active.
		/// </summary>
		/// <remarks>
		/// Override this method to perform any initialization that needs to happen
		/// before the object becomes active.
		/// </remarks>
		protected internal virtual void OnAwake() { }

		/// <summary>
		/// Called after the object is instantiated and after it becomes active.
		/// </summary>
		/// <remarks>
		/// Override this method to perform any initialization or setup that needs to happen
		/// after the object has become active.
		/// </remarks>
		protected internal virtual void OnStart() { }

		/// <summary>
		/// Called after the local player object is instantiated.
		/// </summary>
		/// <remarks>
		/// Override this method to perform any initialization or setup specific to the local player
		/// </remarks>
		protected internal virtual void OnStartLocalPlayer() { }

		/// <summary>
		/// Called after a remote player object is instantiated.
		/// </summary>
		/// <remarks>
		/// Override this method to perform any initialization or setup specific to remote players
		/// </remarks>
		protected internal virtual void OnStartRemotePlayer() { }

		/// <summary>
		/// Called on the server once the client-side object has been fully spawned and registered. 
		/// This method ensures that all initializations on the client have been completed before 
		/// allowing the server to perform any post-spawn actions or setups specific to the client. 
		/// 
		/// Override this method to implement server-side logic that depends on the client object's 
		/// full availability and readiness. Typical use cases may include initializing server-side 
		/// resources linked to the client or sending initial data packets to the client after 
		/// confirming it has been completely registered on the network.
		/// </summary>
		protected internal virtual void OnSpawned()
		{
			// Synchronizes all network variables with the client to ensure that the client has 
			// the most up-to-date data from the server immediately after the spawning process.
			SyncNetworkState();
		}

		/// <summary>
		/// Called on each update tick.
		/// </summary>
		/// <remarks>
		/// Override this method to perform any per-tick processing that needs to occur
		/// during the object's active state. This method is called on a regular interval
		/// determined by the system's tick rate.
		/// </remarks>
		/// <param name="data">The data associated with the current tick.</param>
		public virtual void OnTick(ITickInfo data) { }

		protected internal void Register()
		{
			CheckIfOverridden();
			if (Identity.IsServer)
			{
				sInvoker.FindEvents<ServerAttribute>(this, m_BindingFlags);
				Remote = new NbServer(this);
			}
			else
			{
				cInvoker.FindEvents<ClientAttribute>(this, m_BindingFlags);
				Local = new NbClient(this);
			}

			InitializeServiceLocator();
			AddEventBehaviour();

			if (NetworkManager.TickSystemModuleEnabled)
			{
				NetworkManager.TickSystem.Register(this);
			}

			Identity.OnRequestAction += OnRequestedAction;
			Identity.OnSpawn += OnSpawned;

			NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
			NetworkManager.OnSceneLoaded += OnSceneLoaded;
			NetworkManager.OnSceneUnloaded += OnSceneUnloaded;
		}

		[Conditional("OMNI_DEBUG")]
		private void CheckIfOverridden() // Warning only.
		{
			Type type = GetType();
			MethodInfo method = type.GetMethod(nameof(OnTick));

			if (
				method.DeclaringType.Name != nameof(NetworkBehaviour)
				&& !NetworkManager.TickSystemModuleEnabled
			)
			{
				NetworkLogger.__Log__(
					"Tick System Module must be enabled to use OnTick. You can enable it in the inspector.",
					logType: NetworkLogger.LogType.Error
				);
			}
		}

		protected internal void Unregister()
		{
			var eventBehaviours = Identity.IsServer
				? NetworkManager.Server.LocalEventBehaviours
				: NetworkManager.Client.LocalEventBehaviours;

			var key = (IdentityId, m_Id);
			if (!eventBehaviours.Remove(key))
			{
				NetworkLogger.__Log__(
					$"Unregister Error: EventBehaviour with ID '{m_Id}' and peer ID '{IdentityId}' does not exist. Please ensure the EventBehaviour is registered before attempting to unregister.",
					NetworkLogger.LogType.Error
				);
			}

			if (NetworkManager.TickSystemModuleEnabled)
			{
				NetworkManager.TickSystem.Unregister(this);
			}

			Identity.OnRequestAction -= OnRequestedAction;
			Identity.OnSpawn -= OnSpawned;

			if (!Identity.Unregister(m_ServiceName))
			{
				NetworkLogger.__Log__(
					$"Unregister Error: ServiceLocator with name '{m_ServiceName}' does not exist. Please ensure the ServiceLocator is registered before attempting to unregister.",
					NetworkLogger.LogType.Error
				);
			}

			OnDestroy();
		}

		/// <summary>
		/// Invokes a remote action on the server-side entity, triggered by a client-side entity. 
		/// This method should be overridden to define the specific action that will be performed 
		/// by the server in response to a client request.
		/// </summary>
		protected virtual void OnRequestedAction(DataBuffer data) { }

		protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			NetworkManager.OnSceneLoaded -= OnSceneLoaded;
		}

		protected virtual void OnSceneUnloaded(Scene scene)
		{
			NetworkManager.OnSceneUnloaded -= OnSceneUnloaded;
		}

		protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
		{
			NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
		}

		private void InitializeServiceLocator()
		{
			if (!Identity.TryRegister(this, m_ServiceName))
			{
				// Update the old reference to the new one.
				Identity.UpdateService(this, m_ServiceName);
			}
		}

		private void AddEventBehaviour()
		{
			var eventBehaviours = Identity.IsServer
				? NetworkManager.Server.LocalEventBehaviours
				: NetworkManager.Client.LocalEventBehaviours;

			var key = (IdentityId, m_Id);
			if (!eventBehaviours.TryAdd(key, this))
			{
				eventBehaviours[key] = this;
			}
		}

		private void TryClientLocate(byte msgId, DataBuffer buffer, int seqChannel)
		{
			if (cInvoker.Exists(msgId, out int argsCount))
			{
				switch (argsCount)
				{
					case 0:
						cInvoker.Invoke(msgId);
						break;
					case 1:
						cInvoker.Invoke(msgId, buffer);
						break;
					case 2:
						cInvoker.Invoke(msgId, buffer, seqChannel);
						break;
					case 3:
						cInvoker.Invoke(msgId, buffer, seqChannel, default);
						break;
					case 4:
						cInvoker.Invoke(msgId, buffer, seqChannel, default, default);
						break;
					case 5:
						cInvoker.Invoke(msgId, buffer, seqChannel, default, default, default);
						break;
				}
			}
		}

		private void TryServerLocate(
			byte msgId,
			DataBuffer buffer,
			NetworkPeer peer,
			int seqChannel
		)
		{
			if (sInvoker.Exists(msgId, out int argsCount))
			{
				switch (argsCount)
				{
					case 0:
						sInvoker.Invoke(msgId);
						break;
					case 1:
						sInvoker.Invoke(msgId, buffer);
						break;
					case 2:
						sInvoker.Invoke(msgId, buffer, peer);
						break;
					case 3:
						sInvoker.Invoke(msgId, buffer, peer, seqChannel);
						break;
					case 4:
						sInvoker.Invoke(msgId, buffer, peer, seqChannel, default);
						break;
					case 5:
						sInvoker.Invoke(msgId, buffer, peer, seqChannel, default, default);
						break;
				}
			}
		}

		protected virtual void OnDestroy() { }

		[EditorBrowsable(EditorBrowsableState.Never)]
		public void OnMessageInvoked(
			byte msgId,
			DataBuffer buffer,
			NetworkPeer peer,
			bool _,
			int seqChannel
		)
		{
			if (Identity.IsServer)
			{
				TryServerLocate(msgId, buffer, peer, seqChannel);
			}
			else
			{
				TryClientLocate(msgId, buffer, seqChannel);
			}
		}

		protected virtual void OnValidate()
		{
			if (_identity != null && _identity.IsRegistered)
				___NotifyChange___(); // Overriden by the source generator.

			if (m_Id < 0)
			{
				m_Id = 0;
				NetworkHelper.EditorSaveObject(gameObject);
			}
			else if (m_Id > 255)
			{
				m_Id = 255;
				NetworkHelper.EditorSaveObject(gameObject);
			}

			if (string.IsNullOrEmpty(m_ServiceName))
			{
				int uniqueId = 0;
				string serviceName = GetType().Name;
				NetworkBehaviour[] services =
					transform.root.GetComponentsInChildren<NetworkBehaviour>(true);

				if ((uniqueId = services.Count(x => x.m_ServiceName == serviceName)) >= 1)
				{
					m_ServiceName = $"{serviceName}_{uniqueId}";
				}
				else
				{
					m_ServiceName = serviceName;
				}

				NetworkHelper.EditorSaveObject(gameObject);
			}
		}

		protected virtual void Reset()
		{
			OnValidate();
		}

		public override bool Equals(object obj)
		{
			if (obj is NetworkBehaviour other)
			{
				return Equals(other);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(IdentityId, m_Id, IsServer);
		}

		public bool Equals(NetworkBehaviour other)
		{
			if (Application.isPlaying && _identity != null)
			{
				bool isTheSameBehaviour = m_Id == other.m_Id;
				bool isTheSameIdentity = Identity.Equals(other.Identity);
				return isTheSameBehaviour && isTheSameIdentity && IsServer == other.IsServer;
			}

			return false;
		}
	}
}
