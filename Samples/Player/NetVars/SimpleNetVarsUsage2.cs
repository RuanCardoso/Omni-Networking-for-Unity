using MemoryPack;
using Omni.Collections;
using Omni.Core;
using Omni.Inspector;
using System;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace Omni.Samples
{
	public enum Panels
	{
		Connection,
		Rooms,
		Wait,
		Game
	}

	/// <summary>
	/// Basic usage of [NetworkVariable] attribute, see the documentation for more information.
	/// </summary>
	public partial class SimpleNetVarsUsage2 : NetworkBehaviour
	{
		// [NetworkVariable(CheckEquality = false)]
		[SerializeField]
		private ObservableDictionary<int, string> m_DictTest = new();


		[SerializeField]
		private ObservableDictionary<Panels, GameObject> panels;


		[NetworkVariable]
		[SerializeField]
		private float m_Speed = 1.0f;

		[Button]
		public void IncreaseSpeed()
		{
			Speed++;
		}

		private void Start()
		{
			foreach (var kvp in panels)
			{
				Debug.Log(kvp.Key + " : " + kvp.Value);
			}

			print("Chave:" + panels[Panels.Connection]);
		}

		partial void OnSpeedChanged(float prevSpeed, float nextSpeed, bool isWriting)
		{
			Debug.Log("NetVar Speed Changed!!!");
		}
	}
}
