using System.Runtime.CompilerServices;
using Omni.Core.Interfaces;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
	// Hacky: DIRTY CODE!
	// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
	// Despite its appearance, this approach is essential to achieve high performance.
	// Avoid refactoring as these techniques are crucial for optimizing execution speed.
	// Works with il2cpp.

	public class NbClient
	{
		private readonly IInvokeMessage m_NetworkMessage;
		private readonly NetworkVariablesBehaviour m_NetworkVariablesBehaviour;
		private readonly BindingFlags m_BindingFlags;

		internal NbClient(IInvokeMessage networkMessage, BindingFlags flags)
		{
			m_NetworkMessage = networkMessage;
			m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
			m_BindingFlags = flags;
		}

		/// <summary>
		/// Sends a manual 'NetworkVariable' message to the server with the specified property and property ID.
		/// </summary>
		/// <typeparam name="T">The type of the property to synchronize.</typeparam>
		/// <param name="property">The property value to synchronize.</param>
		/// <param name="propertyId">The ID of the property being synchronized.</param>
		public void ManualSync<T>(T property, byte propertyId, NetworkVariableOptions syncOptions)
		{
			ManualSync<T>(
				property,
				propertyId,
				syncOptions.DeliveryMode,
				syncOptions.SequenceChannel
			);
		}

		/// <summary>
		/// Sends a manual 'NetworkVariable' message to the server with the specified property and property ID.
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
			using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
				property,
				propertyId
			);

			Invoke(NetworkConstants.NET_VAR_RPC_ID, message, deliveryMode, sequenceChannel);
		}

		/// <summary>
		/// Automatically sends a 'NetworkVariable' message to the server based on the caller member name.
		/// </summary>
		/// <typeparam name="T">The type of the property to synchronize.</typeparam>
		public void AutoSync<T>(NetworkVariableOptions options, [CallerMemberName] string ___ = "")
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
			IPropertyInfo property = m_NetworkVariablesBehaviour.GetPropertyInfoWithCallerName<T>(
				___,
				m_BindingFlags
			);

			IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

			if (property != null)
			{
				using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
					propertyGeneric.Invoke(),
					property.Id
				);

				Invoke(NetworkConstants.NET_VAR_RPC_ID, message, deliveryMode, sequenceChannel);
			}
		}

		/// <summary>
		/// Invokes a global message on the server, similar to a Remote Procedure Call (RPC).
		/// </summary>
		/// <param name="msgId">The ID of the message to be invoked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GlobalInvoke(byte msgId, SyncOptions options)
		{
			GlobalInvoke(msgId, options.Buffer, options.DeliveryMode, options.SequenceChannel);
		}

		/// <summary>
		/// Invokes a global message on the server, similar to a Remote Procedure Call (RPC).
		/// </summary>
		/// <param name="msgId">The ID of the message to be invoked.</param>
		/// <param name="buffer">The buffer containing the message data. Default is null.</param>
		/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
		/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GlobalInvoke(
			byte msgId,
			DataBuffer buffer = null,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			byte sequenceChannel = 0
		)
		{
			Client.GlobalInvoke(msgId, buffer, deliveryMode, sequenceChannel);
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
			Client.Invoke(
				msgId,
				m_NetworkMessage.IdentityId,
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
			using var _ = FastWrite(p1);
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
			using var _ = FastWrite(p1, p2);
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
			using var _ = FastWrite(p1, p2, p3);
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
			using var _ = FastWrite(p1, p2, p3, p4);
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
			using var _ = FastWrite(p1, p2, p3, p4, p5);
			options.Buffer = _;
			Invoke(msgId, options);
		}
	}

	public class NbServer
	{
		private readonly IInvokeMessage m_NetworkMessage;
		private readonly NetworkVariablesBehaviour m_NetworkVariablesBehaviour;
		private readonly BindingFlags m_BindingFlags;

		internal NbServer(IInvokeMessage networkMessage, BindingFlags flags)
		{
			m_NetworkMessage = networkMessage;
			m_NetworkVariablesBehaviour = m_NetworkMessage as NetworkVariablesBehaviour;
			m_BindingFlags = flags;
		}

		/// <summary>
		/// Sends a manual 'NetworkVariable' message to all(default) clients with the specified property and property ID.
		/// </summary>
		/// <typeparam name="T">The type of the property to synchronize.</typeparam>
		/// <param name="property">The property value to synchronize.</param>
		/// <param name="propertyId">The ID of the property being synchronized.</param>
		public void ManualSync<T>(
			T property,
			byte propertyId,
			NetworkVariableOptions options,
			NetworkPeer peer = null
		)
		{
			ManualSync<T>(
				property,
				propertyId,
				peer,
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
			NetworkPeer peer = null,
			Target target = Target.All,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0
		)
		{
			dataCache ??= DataCache.None;
			peer ??= Server.ServerPeer;

			using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
				property,
				propertyId
			);

			Invoke(
				NetworkConstants.NET_VAR_RPC_ID,
				peer,
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
			NetworkPeer peer = null,
			[CallerMemberName] string ___ = ""
		)
		{
			AutoSync<T>(
				peer,
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
			NetworkPeer peer = null,
			Target target = Target.All,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
			DataCache dataCache = default,
			byte sequenceChannel = 0,
			[CallerMemberName] string ___ = ""
		)
		{
			dataCache ??= DataCache.None;
			IPropertyInfo property = m_NetworkVariablesBehaviour.GetPropertyInfoWithCallerName<T>(
				___,
				m_BindingFlags
			);

			IPropertyInfo<T> propertyGeneric = property as IPropertyInfo<T>;

			if (property != null)
			{
				peer ??= Server.ServerPeer;
				using DataBuffer message = m_NetworkVariablesBehaviour.CreateHeader(
					propertyGeneric.Invoke(),
					property.Id
				);

				Invoke(
					NetworkConstants.NET_VAR_RPC_ID,
					peer,
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
		/// Invokes a global message on the client, similar to a Remote Procedure Call (RPC).
		/// </summary>
		/// <param name="msgId">The ID of the message to be invoked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GlobalInvoke(byte msgId, NetworkPeer peer, SyncOptions options)
		{
			GlobalInvoke(
				msgId,
				peer,
				options.Buffer,
				options.Target,
				options.DeliveryMode,
				options.GroupId,
				options.DataCache,
				options.SequenceChannel
			);
		}

		/// <summary>
		/// Invokes a global message on the client, similar to a Remote Procedure Call (RPC).
		/// </summary>
		/// <param name="msgId">The ID of the message to be invoked.</param>
		/// <param name="buffer">The buffer containing the message data. Default is null.</param>
		/// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
		/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
		/// <param name="groupId">The group ID for the message. Default is 0.</param>
		/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
		/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void GlobalInvoke(
			byte msgId,
			NetworkPeer peer,
			DataBuffer buffer = null,
			Target target = Target.Self,
			DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered,
			int groupId = 0,
		DataCache dataCache = default,
			byte sequenceChannel = 0
		)
		{
			dataCache ??= DataCache.None;
			Server.GlobalInvoke(
				msgId,
				peer,
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke(byte msgId, NetworkPeer peer, SyncOptions options)
		{
			Invoke(
				msgId,
				peer,
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
		/// <param name="buffer">The buffer containing the message data. Default is null.</param>
		/// <param name="target">The target(s) for the message. Default is <see cref="Target.All"/>.</param>
		/// <param name="deliveryMode">The delivery mode for the message. Default is <see cref="DeliveryMode.ReliableOrdered"/>.</param>
		/// <param name="groupId">The group ID for the message. Default is 0.</param>
		/// <param name="dataCache">Specifies the cache setting for the message, allowing it to be stored for later retrieval.</param>
		/// <param name="sequenceChannel">The sequence channel for the message. Default is 0.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke(
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
			Server.Invoke(
				msgId,
				peer,
				m_NetworkMessage.IdentityId,
				buffer,
				target,
				deliveryMode,
				groupId,
				dataCache,
				sequenceChannel
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke(
			byte msgId,
			NetworkPeer peer,
			IMessage message,
			SyncOptions options = default
		)
		{
			using var _ = message.Serialize();
			options.Buffer = _;
			Invoke(msgId, peer, options);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1>(byte msgId, NetworkPeer peer, T1 p1, SyncOptions options = default)
			where T1 : unmanaged
		{
			NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
			using var _ = FastWrite(p1);
			options.Buffer = _;
			Invoke(msgId, peer, options);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2>(
			byte msgId,
			NetworkPeer peer,
			T1 p1,
			T2 p2,
			SyncOptions options = default
		)
			where T1 : unmanaged
			where T2 : unmanaged
		{
			NetworkHelper.ThrowAnErrorIfIsInternalTypes(p1);
			NetworkHelper.ThrowAnErrorIfIsInternalTypes(p2);
			using var _ = FastWrite(p1, p2);
			options.Buffer = _;
			Invoke(msgId, peer, options);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3>(
			byte msgId,
			NetworkPeer peer,
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
			using var _ = FastWrite(p1, p2, p3);
			options.Buffer = _;
			Invoke(msgId, peer, options);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4>(
			byte msgId,
			NetworkPeer peer,
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
			using var _ = FastWrite(p1, p2, p3, p4);
			options.Buffer = _;
			Invoke(msgId, peer, options);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke<T1, T2, T3, T4, T5>(
			byte msgId,
			NetworkPeer peer,
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
			using var _ = FastWrite(p1, p2, p3, p4, p5);
			options.Buffer = _;
			Invoke(msgId, peer, options);
		}
	}
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.
