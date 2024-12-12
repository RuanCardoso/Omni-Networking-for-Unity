using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-2800)] // It must be the last thing to happen, user scripts must take priority.
    public sealed class NetworkIdentitySpawner : ServerBehaviour
    {
        [SerializeField] private NetworkIdentity m_LocalPlayer;

        [FormerlySerializedAs("m_ObjectsToSpawn")] [SerializeField]
        private List<NetworkIdentity> m_IdentitiesToSpawn;

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

            foreach (NetworkIdentity identity in m_IdentitiesToSpawn)
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
            foreach (NetworkIdentity identity in m_IdentitiesToSpawn)
            {
                if (identity != null)
                {
                    Spawn(identity.name, NetworkManager.ServerSide.ServerPeer);
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
            ThrowIfHasIdentity();
            base.Reset();
        }

        protected override void OnValidate()
        {
            ThrowIfHasIdentity();
            base.OnValidate();
        }

        [Conditional("OMNI_DEBUG")]
        private void ThrowIfHasIdentity()
        {
            if (GetComponentInChildren<NetworkIdentity>() != null)
            {
                throw new NotSupportedException(
                    $"{nameof(NetworkIdentitySpawner)} component should not be attached to an object with a NetworkIdentity."
                );
            }
        }
    }
}