using System;
using System.Collections.Generic;
using UnityEngine;

namespace Omni.Core.Components
{
	public sealed class NetworkObjectSpawner : ServerBehaviour
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
			if (!NetworkHelper.IsPrefab(obj))
			{
				// Disable the original scene object, enabled after instantiation.
				obj.SetActive(false);
				obj.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
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
