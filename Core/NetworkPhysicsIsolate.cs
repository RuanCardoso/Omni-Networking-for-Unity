#if UNITY_EDITOR || !UNITY_SERVER
// 
#else
using Omni.Shared;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Omni.Inspector;
using UnityEngine;
using static Omni.Core.NetworkManager;

namespace Omni.Core.Components
{
    [DeclareFoldoutGroup("Material Settings")]
    [DeclareBoxGroup("General Settings")]
    public class NetworkPhysicsIsolate : OmniBehaviour
    {
        private static NetworkPhysicsIsolate m_Instance;

        private List<Collider[]> m_ServerIdentities = new();
        private List<Collider[]> m_ClientIdentities = new();

        private readonly List<Collider[]> m_ServerColliders = new();
        private readonly List<Collider[]> m_ClientColliders = new();

        [SerializeField]
        [GroupNext("General Settings")]
        private bool m_IgnoreByLayer = false;

        [SerializeField, ShowIf("m_IgnoreByLayer"), DisableInPlayMode]
        private LayerMask m_ClientLayer;

        [SerializeField, ShowIf("m_IgnoreByLayer"), DisableInPlayMode]
        private LayerMask m_ServerLayer;

        [SerializeField]
        [GroupNext("Material Settings")]
        private Color m_Color;
        [SerializeField] private bool m_HideRenderer = false;

        private void Awake()
        {
            m_Instance = this;
            if (m_IgnoreByLayer)
                IgnoreLayerMaskCollisions(m_ClientLayer, m_ServerLayer, true);
        }

        private void Start()
        {
#if UNITY_EDITOR || !UNITY_SERVER
            // Ignore all collisions between server and client objects
#else
			NetworkLogger.__Log__(
				"Isolate mode is not supported on server build. all isolate components will be ignored.",
				NetworkLogger.LogType.Warning
		    );
#endif
        }

#if UNITY_EDITOR || !UNITY_SERVER
        private void Update()
        {
            if (!IsServerActive || !IsClientActive)
                return;

            FindIdentities();
            IgnorePhysics();
            SetupRenderer();
        }
#endif

        private void FindIdentities()
        {
            m_ServerIdentities = ServerSide.Identities.Values.Select(x => x.GetComponentsInChildren<Collider>(true)).ToList();
            m_ClientIdentities = ClientSide.Identities.Values.Select(x => x.GetComponentsInChildren<Collider>(true)).ToList();

            foreach (var col in m_ServerColliders)
                m_ServerIdentities.Add(col);

            foreach (var col in m_ClientColliders)
                m_ClientIdentities.Add(col);
        }

        private void IgnorePhysics()
        {
            if (m_IgnoreByLayer)
            {
                SetLayers(m_ServerIdentities, m_ServerLayer);
                SetLayers(m_ClientIdentities, m_ClientLayer);
                return;
            }

            IgnoreAllCollisions(m_ServerIdentities, m_ClientIdentities);
        }

        private void IgnoreAllCollisions(List<Collider[]> groupA, List<Collider[]> groupB)
        {
            foreach (var colliderA in groupA.SelectMany(c => c))
            {
                if (colliderA == null) continue;

                foreach (var colliderB in groupB.SelectMany(c => c))
                {
                    if (colliderB == null) continue;

                    Physics.IgnoreCollision(colliderA, colliderB, true);
                }
            }
        }

        private void SetLayers(List<Collider[]> identities, LayerMask targetLayer)
        {
            int layer = Mathf.RoundToInt(Mathf.Log(targetLayer.value, 2));
            foreach (var colliders in identities)
            {
                foreach (var collider in colliders)
                {
                    if (collider == null) continue;
                    collider.gameObject.layer = layer;
                }
            }
        }

        private void IgnoreLayerMaskCollisions(LayerMask maskA, LayerMask maskB, bool ignore)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((maskA.value & (1 << i)) == 0) continue;

                for (int j = 0; j < 32; j++)
                {
                    if ((maskB.value & (1 << j)) == 0) continue;

                    Physics.IgnoreLayerCollision(i, j, ignore);
                }
            }
        }

        private void SetupRenderer()
        {
            foreach (var identity in ServerSide.Identities.Values)
            {
                SetupRenderer(identity.gameObject);
            }
        }

        private void SetupRenderer(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = !m_HideRenderer;
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    material.color = m_Color;
                }
            }
        }

        public static void AddToServer(GameObject obj)
        {
            if (!IsServerActive || !IsClientActive)
                return;

#if UNITY_EDITOR || !UNITY_SERVER
            if (m_Instance == null)
                throw new Exception(
                    "NetworkPhysicsIsolate component is not initialized. Please add it to the NetworkManager object.");

            var colliders = obj.GetComponentsInChildren<Collider>(true);
            m_Instance.m_ServerColliders.Add(colliders);
            m_Instance.SetupRenderer(obj);
#endif
        }

        public static void AddToClient(GameObject obj)
        {
            if (!IsServerActive || !IsClientActive)
                return;

#if UNITY_EDITOR || !UNITY_SERVER
            if (m_Instance == null)
                throw new Exception(
                    "NetworkPhysicsIsolate component is not initialized. Please add it to the NetworkManager object.");

            var colliders = obj.GetComponentsInChildren<Collider>(true);
            m_Instance.m_ClientColliders.Add(colliders);
#endif
        }

        private void Reset()
        {
            OnValidate();
        }

        private void OnValidate()
        {
#if OMNI_DEBUG
            if (GetComponentInChildren<NetworkIdentity>() != null)
            {
                throw new NotSupportedException(
                    "NetworkPhysicsIsolate component should not be attached to an object with a NetworkIdentity."
                );
            }
#endif
        }
    }
}