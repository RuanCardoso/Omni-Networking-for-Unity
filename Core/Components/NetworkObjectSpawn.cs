using System;
using System.Collections.Generic;
using UnityEngine;

namespace Omni.Core.Components
{
	public sealed class SetupOnSpawn : NetworkBehaviour
	{
		protected internal override void OnAwake()
		{
			bool m_Enabled = true;
			//if (TryGetComponent<NetworkIsolate>(out var isolate) && IsServer)
			//{
			//	m_Enabled = !isolate.DisableRenderer;
			//}

			Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
			foreach (Renderer renderer in renderers)
			{
				renderer.enabled = m_Enabled;
			}

			NetworkBehaviour[] behaviours = GetComponentsInChildren<NetworkBehaviour>(true);
			foreach (NetworkBehaviour behaviour in behaviours)
			{
				behaviour.enabled = true;
			}
		}
	}

	public sealed class NetworkObjectSpawn : DualBehaviour
	{
		[SerializeField]
		private NetworkIdentity m_LocalPlayer;

		[SerializeField]
		private List<NetworkIdentity> m_ObjectsToSpawn;

		private readonly DataCache m_InstantiateCache = new(CachePresets.ServerNew);

		public override void Start()
		{
			base.Start();

			// Add the objects to the list of objects to spawn.
			if (m_LocalPlayer != null)
			{
				NetworkManager.AddPrefab(m_LocalPlayer);
				DisableSceneObject(m_LocalPlayer.gameObject);
			}

			foreach (NetworkIdentity identity in m_ObjectsToSpawn)
			{
				if (identity != null)
				{
					NetworkManager.AddPrefab(identity);
					DisableSceneObject(identity.gameObject);
				}
			}
		}

		protected override void OnServerStart()
		{
			foreach (NetworkIdentity identity in m_ObjectsToSpawn)
			{
				if (identity != null)
				{
					Spawn(identity.name, NetworkManager.Server.ServerPeer);
				}
			}
		}

		protected override void OnServerPeerConnected(NetworkPeer peer, Phase phase)
		{
			if (phase == Phase.End)
			{
				m_InstantiateCache.SendToPeer(peer);
				if (m_LocalPlayer != null)
				{
					Spawn(m_LocalPlayer.name, peer);
				}
			}
		}

		private void Spawn(string prefabName, NetworkPeer peer)
		{
			var prefab = NetworkManager.GetPrefab(prefabName);
			prefab.Spawn(peer, dataCache: m_InstantiateCache);
		}

		private void DisableSceneObject(GameObject obj)
		{
			bool isPrefab = obj.scene.name == null || obj.scene.name.ToLower() == "null";
			if (!isPrefab)
			{
				Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
				foreach (Renderer renderer in renderers)
				{
					renderer.enabled = false;
				}

				NetworkBehaviour[] behaviours = obj.GetComponentsInChildren<NetworkBehaviour>(true);
				foreach (NetworkBehaviour behaviour in behaviours)
				{
					behaviour.enabled = false;
				}

				obj.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
				obj.AddComponent<SetupOnSpawn>();
			}
		}

		protected override void Reset()
		{
			base.Reset();
			ThrowIfHasIdentity();
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			ThrowIfHasIdentity();
		}

		private void ThrowIfHasIdentity()
		{
#if OMNI_DEBUG
			if (GetComponentInChildren<NetworkIdentity>() != null)
			{
				throw new NotSupportedException(
					"NetworkSpawn should not be attached to an object with a NetworkIdentity."
				);
			}
#endif
		}
	}
}
