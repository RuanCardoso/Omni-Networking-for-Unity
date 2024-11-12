#if UNITY_EDITOR || !UNITY_SERVER
// 
#else
using Omni.Shared;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Omni.Core.NetworkManager;

namespace Omni.Core.Components
{
	public class NetworkPhysicsIsolate : MonoBehaviour
	{
		private static NetworkPhysicsIsolate m_Instance;

		private List<Collider[]> m_ServerIdentities = new();
		private List<Collider[]> m_ClientIdentities = new();

		private readonly List<Collider[]> m_ServerColliders = new();
		private readonly List<Collider[]> m_ClientColliders = new();

		[SerializeField]
		private Color m_Color;

		[SerializeField]
		private bool m_HideRenderer = false;

		[SerializeField]
		private Material m_ShaderMaterial;

		private void Awake()
		{
			m_Instance = this;
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

		private void Update()
		{
			if (!IsServerActive || !IsClientActive)
				return;

#if UNITY_EDITOR || !UNITY_SERVER
			FindIdentities();
			IgnorePhysics();
			SetupRenderer();
#endif
		}

		private void FindIdentities()
		{
			m_ServerIdentities = Server.Identities.Values.Select(x => x.GetComponentsInChildren<Collider>(true)).ToList();
			m_ClientIdentities = Client.Identities.Values.Select(x => x.GetComponentsInChildren<Collider>(true)).ToList();

			foreach (var col in m_ServerColliders)
				m_ServerIdentities.Add(col);

			foreach (var col in m_ClientColliders)
				m_ClientIdentities.Add(col);
		}

		private void IgnorePhysics()
		{
			// Ignore all collisions between server and client objects
			foreach (var serverColliders in m_ServerIdentities)
			{
				foreach (var clientColliders in m_ClientIdentities)
				{
					foreach (var serverCollider in serverColliders)
					{
						foreach (var clientCollider in clientColliders)
						{
							if (serverCollider == null || clientCollider == null)
								continue;

							Physics.IgnoreCollision(serverCollider, clientCollider, true);
						}
					}
				}
			}
		}

		private void SetupRenderer()
		{
			foreach (var identity in Server.Identities.Values)
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

					if (m_ShaderMaterial != null)
					{
						renderer.materials[i] = m_ShaderMaterial;
					}
				}
			}
		}

		public static void AddToServer(GameObject obj)
		{
			if (!IsServerActive || !IsClientActive)
				return;

#if UNITY_EDITOR || !UNITY_SERVER
			if (m_Instance == null)
				throw new Exception("NetworkPhysicsIsolate is not initialized. Please add it to the NetworkManager object.");

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
				throw new Exception("NetworkPhysicsIsolate is not initialized. Please add it to the NetworkManager object.");

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
					"NetworkPhysicsIsolate should not be attached to an object with a NetworkIdentity."
				);
			}
#endif
		}
	}
}