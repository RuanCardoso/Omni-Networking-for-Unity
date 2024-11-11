using System.Collections;
using System.ComponentModel;
using Omni.Core.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Omni.Core.NetworkManager;

namespace Omni.Core
{
	// Hacky: DIRTY CODE!
	// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
	// Despite its appearance, this approach is essential to achieve high performance.
	// Avoid refactoring as these techniques are crucial for optimizing execution speed.
	// Works with il2cpp.

	[DefaultExecutionOrder(-3000)]
	public class ClientBehaviour : NetworkEventBase, IInvokeMessage, IServiceBehaviour
	{
		/// <summary>
		/// Gets the identifier of the associated <see cref="IInvokeMessage"/>.
		/// </summary>
		/// <value>The identifier of the associated <see cref="IInvokeMessage"/> as an integer.</value>
		public int IdentityId => m_Id;

		private readonly InvokeBehaviour<DataBuffer, int, Null, Null, Null> invoker = new();

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
			InitStart();
		}

		private void InitStart()
		{
			if (m_UnregisterOnLoad)
			{
				RegisterMatchmakingEvents();
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

		private IEnumerator Internal_OnClientStart()
		{
			yield return new WaitUntil(() => IsClientActive);
			OnClientStart();
		}

		/// <summary>
		/// Invoked when the client becomes active. This method functions similarly to Unity's Start(),
		/// but is specifically called when the client is up and running.
		/// </summary>
		protected virtual void OnClientStart() { }

		protected void InitializeBehaviour()
		{
			invoker.FindEvents<ClientAttribute>(this, m_BindingFlags);
			Client.AddEventBehaviour(m_Id, this);
			Local = new NbClient(this, m_BindingFlags);
		}

		protected void RegisterSystemEvents()
		{
			NetworkManager.OnBeforeSceneLoad += OnBeforeSceneLoad;
			NetworkManager.OnClientConnected += OnClientConnected;
			NetworkManager.OnClientDisconnected += OnClientDisconnected;
			NetworkManager.OnClientIdentitySpawned += OnClientIdentitySpawned;
			Client.OnMessage += OnMessage;
		}



		protected void RegisterMatchmakingEvents()
		{
			if (MatchmakingModuleEnabled)
			{
				Matchmaking.Client.OnJoinedGroup += OnJoinedGroup;
				Matchmaking.Client.OnLeftGroup += OnLeftGroup;
			}
		}

		protected void Unregister()
		{
			NetworkManager.OnBeforeSceneLoad -= OnBeforeSceneLoad;
			NetworkManager.OnClientConnected -= OnClientConnected;
			NetworkManager.OnClientDisconnected -= OnClientDisconnected;
			NetworkManager.OnClientIdentitySpawned -= OnClientIdentitySpawned;
			Client.OnMessage -= OnMessage;

			if (MatchmakingModuleEnabled)
			{
				Matchmaking.Client.OnJoinedGroup -= OnJoinedGroup;
				Matchmaking.Client.OnLeftGroup -= OnLeftGroup;
			}

			NetworkService.Unregister(m_ServiceName);
			OnStop();
		}

		protected virtual void OnClientIdentitySpawned(NetworkIdentity identity)
		{
		}

		protected virtual void OnBeforeSceneLoad(Scene scene, SceneOperationMode op)
		{
			if (m_UnregisterOnLoad)
			{
				Unregister();
			}
		}

		protected virtual void OnClientConnected() { }

		protected virtual void OnClientDisconnected(string reason) { }

		protected virtual void OnMessage(byte msgId, DataBuffer buffer, int seqChannel)
		{
			buffer.SeekToBegin();
			TryInvoke(msgId, buffer, seqChannel); // Global Invoke
		}

		private void TryInvoke(byte msgId, DataBuffer buffer, int seqChannel)
		{
			if (invoker.Exists(msgId, out int argsCount))
			{
				switch (argsCount)
				{
					case 0:
						invoker.Invoke(msgId);
						break;
					case 1:
						invoker.Invoke(msgId, buffer);
						break;
					case 2:
						invoker.Invoke(msgId, buffer, seqChannel);
						break;
					case 3:
						invoker.Invoke(msgId, buffer, seqChannel, default);
						break;
					case 4:
						invoker.Invoke(msgId, buffer, seqChannel, default, default);
						break;
					case 5:
						invoker.Invoke(msgId, buffer, seqChannel, default, default, default);
						break;
				}
			}
		}

		protected virtual void OnJoinedGroup(string groupName, DataBuffer buffer) { }

		protected virtual void OnLeftGroup(string groupName, string reason) { }

		public void OnMessageInvoked(
			byte methodId,
			DataBuffer buffer,
			NetworkPeer peer,
			bool isServer,
			int seqChannel
		)
		{
			invoker.ThrowNoMethodFound(methodId);
			TryInvoke(methodId, buffer, seqChannel);
		}
	}
}

// Hacky: DIRTY CODE!
// This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
// Despite its appearance, this approach is essential to achieve high performance.
// Avoid refactoring as these techniques are crucial for optimizing execution speed.
// Works with il2cpp.
