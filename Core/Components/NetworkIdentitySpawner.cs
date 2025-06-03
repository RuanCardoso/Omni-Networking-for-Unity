using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Omni.Inspector;
using Omni.Threading.Tasks;
using UnityEngine;

namespace Omni.Core.Components
{
    [Serializable]
    class CachedIdentity
    {
        public string m_PrefabName;
        public NetworkIdentity m_Identity;
    }

    [DefaultExecutionOrder(-2800)] // It should be the last thing to happen, user scripts should take priority. !!! Important
    [DeclareFoldoutGroup("Spawn Options", Expanded = false)]
    public partial class NetworkIdentitySpawner : DualBehaviour
    {
        private const int k_SpawnRpcId = 1;
        private const int k_SpawnCacheRpcId = 2;

        [ReadOnly]
        [SerializeField]
        private List<CachedIdentity> m_CachedIdentities = new();

        [InfoBox("This component is designed for rapid development, prototyping and testing purposes. It provides a streamlined approach to network identity spawning and management.", TriMessageType.Warning)]
        [SerializeField, Min(1), LabelWidth(115)] private int m_SpawnCount = 1;
        [SerializeField, LabelWidth(115), Indent] private NetworkIdentity m_LocalPlayer;
        [SerializeField] private List<NetworkIdentity> m_ServerIdentities;

        public override void Start()
        {
            base.Start();
            // Add the objects to the list of objects to spawn.
            if (m_LocalPlayer != null)
            {
                NetworkManager.AddPrefab(m_LocalPlayer);
                DisableSceneObject(m_LocalPlayer.gameObject);
            }

            foreach (NetworkIdentity identity in m_ServerIdentities)
            {
                if (identity != null)
                {
                    NetworkManager.AddPrefab(identity);
                    DisableSceneObject(identity.gameObject);
                }
            }
        }

        protected override async void OnServerStart()
        {
            SpawnNetworkIdentities();
            while (Application.isPlaying)
            {
                foreach (CachedIdentity cachedIdentity in m_CachedIdentities.ToList())
                {
                    if (cachedIdentity.m_Identity == null)
                        m_CachedIdentities.Remove(cachedIdentity);
                }

                // Clear the invalid cached identities every second.
                await UniTask.Delay(1000);
            }
        }

        private void SpawnNetworkIdentities()
        {
            foreach (NetworkIdentity identity in m_ServerIdentities)
            {
                if (identity != null)
                {
                    if (m_LocalPlayer != null && identity.name == m_LocalPlayer.name)
                        continue;

                    Spawn(identity.name, NetworkManager.ServerSide.ServerPeer);
                }
            }
        }

        protected override void OnServerPeerConnected(NetworkPeer peer, Phase phase)
        {
            if (phase == Phase.Ended)
            {
                SpawnLocalPlayer(peer);
            }
        }

        private void SpawnLocalPlayer(NetworkPeer peer)
        {
            foreach (CachedIdentity cachedIdentity in m_CachedIdentities)
            {
                NetworkIdentity identity = cachedIdentity.m_Identity;
                if (identity == null)
                    continue;

                Server.Rpc(k_SpawnCacheRpcId, peer, cachedIdentity.m_PrefabName, identity.Owner.Id, identity.Id);
            }

            if (m_LocalPlayer != null)
            {
                for (int i = 0; i < m_SpawnCount; i++)
                    Spawn(m_LocalPlayer.name, peer);
            }
        }

        private void Spawn(string prefabName, NetworkPeer peer)
        {
            var prefab = NetworkManager.GetPrefab(prefabName);
            var identity = prefab.SpawnOnServer(peer.Id, prefab.EntityType);
            m_CachedIdentities.Add(new CachedIdentity { m_PrefabName = prefabName, m_Identity = identity });
            Server.Rpc(k_SpawnRpcId, peer, prefabName, peer.Id, identity.Id);
        }

        [Server(k_SpawnRpcId, Target = Target.Everyone)]
        [Server(k_SpawnCacheRpcId, Target = Target.Self)]
        private void SpawnStubRpc() { } // stub rpc -> only used for rpc registration with optional parameters

        [Client(k_SpawnRpcId)]
        [Client(k_SpawnCacheRpcId)]
        private void SpawnRpc(string prefabName, int peerId, int identityId)
        {
            var prefab = NetworkManager.GetPrefab(prefabName);
            prefab.SpawnOnClient(peerId, identityId);
        }

        private void DisableSceneObject(GameObject sceneObj)
        {
            if (!NetworkHelper.IsPrefab(sceneObj))
            {
                // Disable the original scene object, enabled after instantiation.
                sceneObj.SetActive(false);
                sceneObj.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            }
        }

#if UNITY_EDITOR
        [Button("Register Scene Objects")]
        [ContextMenu("Register Scene Objects")]
        private void FindAllIdentities()
        {
            m_ServerIdentities = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.InstanceID).ToList();
        }
#endif

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