using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Omni.Core.NetworkManager;


namespace Omni.Core.Components
{
	public class NetworkPhysicsIsolate : MonoBehaviour
	{
		[SerializeField]
		private List<Collider[]> m_ServerIdentities = new();

		[SerializeField]
		private List<Collider[]> m_ClientIdentities = new();

		[SerializeField]
		private Color m_Color;

		private void Update()
		{
			FindIdentities();
			IgnorePhysics();
			SetupRenderer();
		}

		private void FindIdentities()
		{
			m_ServerIdentities = Server.Identities.Values.Select(x => x.GetComponentsInChildren<Collider>(true)).ToList();
			m_ClientIdentities = Client.Identities.Values.Select(x => x.GetComponentsInChildren<Collider>(true)).ToList();
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
				Renderer[] renderers = identity.GetComponentsInChildren<Renderer>(true);
				foreach (Renderer renderer in renderers)
				{
					renderer.material.color = m_Color;
					renderer.enabled = true;
				}
			}
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