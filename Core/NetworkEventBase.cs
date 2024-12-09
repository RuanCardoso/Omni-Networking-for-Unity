using Omni.Shared;
using System;
using System.ComponentModel;
using TriInspector;
using UnityEngine;

#pragma warning disable

namespace Omni.Core
{
	[DeclareFoldoutGroup("Network Variables", Expanded = true, Title = "Network Variables - (Auto Synced)")]
	[DeclareBoxGroup("Service Settings")]
	[StackTrace]
	public class NetworkEventBase : NetworkVariablesBehaviour
	{
		[GroupNext("Service Settings")]
		[SerializeField]
		internal string m_ServiceName;

		[SerializeField]
		internal int m_Id;

		internal BindingFlags m_BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private NetworkEventClient local;
		private NetworkEventServer remote;
		internal bool m_UnregisterOnLoad = true;

		// public api: allow send from other object
		/// <summary>
		/// Gets the <see cref="NetworkEventClient"/> instance used to invoke messages on the server from the client.
		/// </summary>
		public NetworkEventClient Local
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
		public NetworkEventServer Remote
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
		/// Called when the service is initialized.
		/// </summary>
		protected virtual void OnAwake() { }

		/// <summary>
		/// Called when the service is initialized.
		/// </summary>
		protected virtual void OnStart() { }

		/// <summary>
		/// Called when the service is stopped/destroyed/unregistered.
		/// </summary>
		protected virtual void OnStop() { }

		/// <summary>
		/// Rents a data buffer from the network manager's pool. The caller must ensure the buffer is disposed or used within a using statement.
		/// </summary>
		/// <returns>A rented data buffer.</returns>
		protected DataBuffer Rent()
		{
			return NetworkManager.Pool.Rent();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Don't override this method! The source generator will override it.")]
		protected internal virtual void ___InjectServices___() { }

		protected virtual void Reset()
		{
			OnValidate();
		}

		protected virtual void OnValidate()
		{
			//if (remote != null || local != null)
			//	___NotifyEditorChange___(); // Override by the source generator.

			if (m_Id == 0)
			{
				m_Id = NetworkHelper.GenerateSceneUniqueId();
				NetworkHelper.EditorSaveObject(gameObject);
			}

			if (string.IsNullOrEmpty(m_ServiceName))
			{
				m_ServiceName = GetType().Name;
				NetworkHelper.EditorSaveObject(gameObject);
			}

#if OMNI_DEBUG
			if (GetComponentInChildren<NetworkIdentity>() != null)
			{
				throw new NotSupportedException(
					"NetworkEventBase should not be attached to an object with a NetworkIdentity. Use 'NetworkBehaviour' instead."
				);
			}
#endif
		}
	}
}
