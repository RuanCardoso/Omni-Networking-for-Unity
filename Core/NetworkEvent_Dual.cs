using Omni.Core.Interfaces;
using Omni.Shared;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Omni.Core.NetworkManager;

#pragma warning disable

namespace Omni.Core
{
	// Hacky: DIRTY CODE!
	// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
	// Despite its appearance, this approach is essential to achieve high performance.
	// Avoid refactoring as these techniques are crucial for optimizing execution speed.
	// Works with il2cpp.

	[DefaultExecutionOrder(-3000)]
	public class DualBehaviour : NetworkEventBase, IInvokeMessage, IServiceBehaviour
	{
		/// <summary>
		/// Gets the identifier of the associated <see cref="IInvokeMessage"/>.
		/// </summary>
		/// <value>The identifier of the associated <see cref="IInvokeMessage"/> as an integer.</value>
		public int IdentityId => m_Id;

		private readonly InvokeBehaviour<DataBuffer, int, Null, Null, Null> cInvoker = new();
		private readonly InvokeBehaviour<DataBuffer, NetworkPeer, int, Null, Null> sInvoker = new();

		private NetworkEventClient local;
		private NetworkEventServer remote;

		// public api: allow send from other object
		/// <summary>
		/// Gets the <see cref="NetworkEventClient"/> instance used to invoke messages on the server from the client.
		/// </summary>
		public NetworkEventClient Client
		{
			get
			{
				if (local == null)
				{
					NetworkLogger.PrintHyperlink();
					throw new NullReferenceException(
						"This property(Local) is intended for client-side use only. It appears to be accessed from the server side. Or Call Awake() and Start() base first or initialize manually."
					);
				}

				return local;
			}
			internal set => local = value;
		}

		// public api: allow send from other object
		/// <summary>
		/// Gets the <see cref="NetworkEventServer"/> instance used to invoke messages on the client from the server.
		/// </summary>
		public NetworkEventServer Server
		{
			get
			{
				if (remote == null)
				{
					NetworkLogger.PrintHyperlink();
					throw new NullReferenceException(
						"This property(Remote) is intended for server-side use only. It appears to be accessed from the client side. Or Call Awake() and Start() base first or initialize manually."
					);
				}

				return remote;
			}
			internal set => remote = value;
		}

		/// <summary>
		/// The `Awake` method is virtual, allowing it to be overridden in derived classes
		/// for additional startup logic. If overridden, it is essential to call the base class's
		/// `Awake` method to ensure proper initialization. Not doing so may result in incomplete
		/// initialization and unpredictable behavior.
		/// </summary>
		public virtual void Awake()
		{
			InitAwake();
		}

		private void InitAwake()
		{
			if (NetworkService.Exists(m_ServiceName))
			{
				m_UnregisterOnLoad = false;
				return;
			}

			if (m_UnregisterOnLoad)
			{
				InitializeServiceLocator();
				InitializeBehaviour();
				RegisterSystemEvents();
				OnAwake();
			}
		}

		/// <summary>
		/// The `Start` method is virtual, allowing it to be overridden in derived classes
		/// for additional startup logic. If overridden, it is essential to call the base class's
		/// `Start` method to ensure proper initialization. Not doing so may result in incomplete
		/// initialization and unpredictable behavior.
		/// </summary>
		public virtual void Start()
		{
			___InjectServices___();
			InitStart();
		}

		private void InitStart()
		{
			if (m_UnregisterOnLoad)
			{
				RegisterMatchmakingEvents();
				StartCoroutine(Internal_OnServerStart());
				StartCoroutine(Internal_OnClientStart());

				OnStart();
				Service.UpdateReference(m_ServiceName);
			}

			m_UnregisterOnLoad = !NetworkHelper.IsDontDestroyOnLoad(gameObject);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public void Internal_Awake()
		{
			InitAwake();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public void Internal_Start()
		{
			InitStart();
		}

		protected void InitializeServiceLocator()
		{
			if (!NetworkService.TryRegister(this, m_ServiceName))
			{
				// Update the old reference to the new one.
				NetworkService.Update(this, m_ServiceName);
			}
		}

		private IEnumerator Internal_OnServerStart()
		{
			yield return new WaitUntil(() => IsServerActive);
			OnServerStart();
		}

		private IEnumerator Internal_OnClientStart()
		{
			yield return new WaitUntil(() => IsClientActive);
			OnClientStart();
		}

		/// <summary>
		/// Invoked when the server becomes active. This method functions similarly to Unity's Start(),
		/// but is specifically called when the server is up and running.
		/// </summary>
		protected virtual void OnServerStart() { }

		/// <summary>
		/// Invoked when the client becomes active. This method functions similarly to Unity's Start(),
		/// but is specifically called when the client is up and running.
		/// </summary>
		protected virtual void OnClientStart() { }

		protected void InitializeBehaviour()
		{
			cInvoker.FindEvents<ClientAttribute>(this, m_BindingFlags);
			sInvoker.FindEvents<ServerAttribute>(this, m_BindingFlags);

			NetworkManager.ClientSide.AddEventBehaviour(m_Id, this);
			NetworkManager.ServerSide.AddEventBehaviour(m_Id, this);

			Client = new NetworkEventClient(this, m_BindingFlags);
			Server = new NetworkEventServer(this, m_BindingFlags);
		}

		protected void RegisterSystemEvents()
		{
			NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
			NetworkManager.OnClientConnected += OnClientConnected;
			NetworkManager.OnClientDisconnected += OnClientDisconnected;
			NetworkManager.ClientSide.OnMessage += OnClientMessage;

			NetworkManager.OnServerInitialized += OnServerInitialized;
			NetworkManager.OnServerPeerConnected += OnServerPeerConnected;
			NetworkManager.OnServerPeerDisconnected += OnServerPeerDisconnected;
			NetworkManager.ServerSide.OnMessage += OnServerMessage;
		}

		protected void RegisterMatchmakingEvents()
		{
			if (MatchmakingModuleEnabled)
			{
				Matchmaking.Client.OnJoinedGroup += OnJoinedGroup;
				Matchmaking.Client.OnLeftGroup += OnLeftGroup;

				Matchmaking.Server.OnPlayerJoinedGroup += OnPlayerJoinedGroup;
				Matchmaking.Server.OnPlayerLeftGroup += OnPlayerLeftGroup;

				Matchmaking.Server.OnPlayerFailedJoinGroup += OnPlayerFailedJoinGroup;
				Matchmaking.Server.OnPlayerFailedLeaveGroup += OnPlayerFailedLeaveGroup;
			}
		}

		protected void Unregister()
		{
			NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
			NetworkManager.OnClientConnected -= OnClientConnected;
			NetworkManager.OnClientDisconnected -= OnClientDisconnected;
			NetworkManager.ClientSide.OnMessage -= OnClientMessage;

			NetworkManager.OnServerInitialized -= OnServerInitialized;
			NetworkManager.OnServerPeerConnected -= OnServerPeerConnected;
			NetworkManager.OnServerPeerDisconnected -= OnServerPeerDisconnected;
			NetworkManager.ServerSide.OnMessage -= OnServerMessage;

			if (MatchmakingModuleEnabled)
			{
				Matchmaking.Client.OnJoinedGroup -= OnJoinedGroup;
				Matchmaking.Client.OnLeftGroup -= OnLeftGroup;

				Matchmaking.Server.OnPlayerJoinedGroup -= OnPlayerJoinedGroup;
				Matchmaking.Server.OnPlayerLeftGroup -= OnPlayerLeftGroup;

				Matchmaking.Server.OnPlayerFailedJoinGroup -= OnPlayerFailedJoinGroup;
				Matchmaking.Server.OnPlayerFailedLeaveGroup -= OnPlayerFailedLeaveGroup;
			}

			NetworkService.Unregister(m_ServiceName);
			OnStop();
		}

		protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
		{
			if (m_UnregisterOnLoad)
			{
				Unregister();
			}
		}

		#region Client
		protected virtual void OnClientConnected() { }

		protected virtual void OnClientDisconnected(string reason) { }

		protected virtual void OnClientMessage(byte msgId, DataBuffer buffer, int seqChannel)
		{
			buffer.SeekToBegin();
			TryInvoke(msgId, buffer, seqChannel); // Global Invoke
		}

		private void TryInvoke(byte msgId, DataBuffer buffer, int seqChannel)
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

		protected virtual void OnJoinedGroup(string groupName, DataBuffer buffer) { }

		protected virtual void OnLeftGroup(string groupName, string reason) { }
		#endregion

		#region Server
		protected virtual void OnServerInitialized() { }

		protected virtual void OnServerPeerConnected(NetworkPeer peer, Phase phase) { }

		protected virtual void OnServerPeerDisconnected(NetworkPeer peer, Phase phase) { }

		protected virtual void OnPlayerFailedLeaveGroup(NetworkPeer peer, string reason) { }

		protected virtual void OnPlayerFailedJoinGroup(NetworkPeer peer, string reason) { }

		protected virtual void OnServerMessage(
			byte msgId,
			DataBuffer buffer,
			NetworkPeer peer,
			int seqChannel
		)
		{
			buffer.SeekToBegin();
			TryInvoke(msgId, buffer, peer, seqChannel); // Global Invoke
		}

		private void TryInvoke(byte msgId, DataBuffer buffer, NetworkPeer peer, int seqChannel)
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

		protected virtual void OnPlayerJoinedGroup(
			DataBuffer buffer,
			NetworkGroup group,
			NetworkPeer peer
		)
		{ }

		protected virtual void OnPlayerLeftGroup(
			NetworkGroup group,
			NetworkPeer peer,
			Phase phase,
			string reason
		)
		{ }
		#endregion

		public void OnMessageInvoked(
			byte methodId,
			DataBuffer buffer,
			NetworkPeer peer,
			bool isServer,
			int seqChannel
		)
		{
			if (isServer)
			{
				sInvoker.ThrowNoMethodFound(methodId);
				TryInvoke(methodId, buffer, peer, seqChannel); // server Invoke
			}
			else
			{
				cInvoker.ThrowNoMethodFound(methodId);
				TryInvoke(methodId, buffer, seqChannel); // client Invoke
			}
		}
	}
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.
