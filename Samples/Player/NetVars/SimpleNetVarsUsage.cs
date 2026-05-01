using MemoryPack;
using Omni.Collections;
using Omni.Core;
using Omni.Inspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Omni.Samples
{
	[Serializable]
	[MemoryPackable]
	public partial class PlayerDataTest
	{
		public int Health;
		public int Mana;
	}

	/// <summary>
	/// Basic usage of [NetworkVariable] attribute, see the documentation for more information.
	/// </summary>
	public partial class SimpleNetVarsUsage : NetworkBehaviour
	{
		// Simple types serialization

		[NetworkVariable]
		private int m_Health;

		[NetworkVariable]
		[SerializeField]
		private int m_Mana;

		// Reference type serialization

		[NetworkVariable(CheckEquality = false)]
		[SerializeField]
		private ObservableList<PlayerDataTest> m_PlayerDataTests;

		// TEST BUTTONS

		[Button("Add Player Data")]
		public void AddPlayerData()
		{
			PlayerDataTests.Add(new());
		}

		[Button("Random First Item")]
		public void FillFirst()
		{
			Random rng = new Random();
			if (m_PlayerDataTests.Count > 0)
			{
				m_PlayerDataTests[0] = new PlayerDataTest()
				{
					Mana = rng.Next(),
					Health = rng.Next()
				};
			}
		}

		[Button("Random Values")]
		public void FillAll()
		{
			Random rng = new Random();
			foreach (var p in PlayerDataTests)
			{
				p.Mana = rng.Next();
				p.Health = rng.Next();
			}

			SyncPlayerDataTests();
		}
	}
}
