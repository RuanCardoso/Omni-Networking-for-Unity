using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Omni.Inspector;
using UnityEngine;

namespace Omni.Core.Components
{
    [DefaultExecutionOrder(-2800)] // It should be the last thing to happen, user scripts should take priority. !!! Important
    [DeclareFoldoutGroup("Spawn Options", Expanded = false)]
    public sealed class NetworkIdentitySpawner : ServerBehaviour
    {
        [SerializeField] private NetworkIdentity m_LocalPlayer;
        [SerializeField] private List<NetworkIdentity> m_IdentitiesToSpawn;
        private DataCache m_InstantiateCache;

        [GroupNext("Spawn Options")]
        [SerializeField, LabelWidth(150)] private bool m_EnableCache = true;
        [SerializeField, ShowIf(nameof(m_EnableCache)), LabelWidth(150)] private bool m_AutoDestroyCache = true;
        [SerializeField, LabelWidth(150)] private bool m_SpawnAfterGroupJoin = false;
        [SerializeField, LabelWidth(150), ShowIf(nameof(m_SpawnAfterGroupJoin))] private bool m_AutoCreateGroup = true;
        [SerializeField, LabelWidth(150), ShowIf(nameof(m_AutoCreateGroup)), ShowIf(nameof(m_SpawnAfterGroupJoin))] private string m_GroupName = "_mainGroup";


        protected override void OnAwake()
        {
            if (m_SpawnAfterGroupJoin)
            {
                m_InstantiateCache = m_AutoDestroyCache ? new(CachePresets.GroupNewWithAutoDestroy) : new(CachePresets.GroupNew);
            }
            else
            {
                m_InstantiateCache = m_AutoDestroyCache ? new(CachePresets.ServerNewWithAutoDestroy) : new(CachePresets.ServerNew);
            }
        }

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
            if (m_SpawnAfterGroupJoin)
            {
                if (m_AutoCreateGroup)
                {
                    var group = NetworkManager.Matchmaking.Server.AddGroup(m_GroupName);
                    group.DestroyWhenEmpty = true;
                }

                return;
            }

            SpawnNetworkIdentities(0);
        }

        private void SpawnNetworkIdentities(int groupId)
        {
            foreach (NetworkIdentity identity in m_IdentitiesToSpawn)
            {
                if (identity != null)
                {
                    if (m_LocalPlayer != null && identity.name == m_LocalPlayer.name)
                        continue;

                    Spawn(identity.name, NetworkManager.ServerSide.ServerPeer, groupId);
                }
            }
        }

        protected override void OnServerPeerConnected(NetworkPeer peer, Phase phase)
        {
            if (phase == Phase.End)
            {
                if (m_SpawnAfterGroupJoin)
                {
                    if (m_AutoCreateGroup)
                    {
                        var _mainGroup = NetworkManager.Matchmaking.Server.GetGroup(m_GroupName);
                        NetworkManager.Matchmaking.Server.JoinGroup(_mainGroup, peer);
                    }

                    return;
                }

                SpawnLocalPlayer(peer, 0);
            }
        }

        protected override void OnPlayerJoinedGroup(DataBuffer buffer, NetworkGroup group, NetworkPeer peer)
        {
            if (m_SpawnAfterGroupJoin)
            {
                if (group.IsSubGroup)
                    return;

                SpawnLocalPlayer(peer, group.Id);

                if (!group.Data.ContainsKey("_isCreated"))
                {
                    SpawnNetworkIdentities(group.Id);
                    group.Data["_isCreated"] = true;
                }
            }
        }

        private void SpawnLocalPlayer(NetworkPeer peer, int groupId)
        {
            if (m_EnableCache)
                m_InstantiateCache.SendToPeer(peer, m_SpawnAfterGroupJoin ? groupId : 0);

            if (m_LocalPlayer != null)
            {
                Spawn(m_LocalPlayer.name, peer, groupId);
            }
        }

        private void Spawn(string prefabName, NetworkPeer peer, int groupId)
        {
            var prefab = NetworkManager.GetPrefab(prefabName);
            var identity = prefab.Spawn(peer, dataCache: m_EnableCache ? m_InstantiateCache : DataCache.None, groupId: groupId);

            if (NetworkManager.Matchmaking.Server.TryGetGroup(groupId, out var group))
            {
                group.AddIdentity(identity);
            }
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

#if UNITY_EDITOR
        [Button("Find All Identities")]
        [ContextMenu("Find All Identities")]
        private void FindAllIdentities()
        {
            m_IdentitiesToSpawn = FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.InstanceID).ToList();
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