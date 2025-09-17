using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using Omni.Inspector;
using UnityEngine;

/// <summary>
/// This component is intended for rapid prototyping of networked animation synchronization in Unity.
/// It provides a basic and generic way to sync Animator parameters over the network, allowing you to quickly test multiplayer animation flows.
/// <para>
/// <b>Important:</b> For production or advanced projects, it is highly recommended to implement your own custom animation synchronization logic
/// tailored to your game's specific needs, as this component may not cover all edge cases or complex animation setups.
/// </para>
/// </summary>

namespace Omni.Core.Components
{
    [Serializable]
    [DeclareHorizontalGroup("Horizontal")]
    public class Parameter
    {
        [Group("Horizontal")]
        [LabelText("Name ->")]
        [LabelWidth(60)]
        [DisplayAsString]
        public string Name;

        [Group("Horizontal")]
        [LabelWidth(45)]
        [LabelText("Sync")]
        public bool Sync = true;

        [HideInInspector]
        public int NameHash;
    }

    public partial class NetworkAnimatorSync : NetworkBehaviour
    {
        private Animator m_Animator;
        private AnimatorControllerParameter[] m_FloatParameters, m_BoolParameters, m_IntParameters;

        private Delta8<bool> b_l_Delta;
        private Delta8<float> f_l_Delta;
        private Delta8<int> i_l_Delta;

        [SerializeField]
        [ListDrawerSettings(AlwaysExpanded = false, HideAddButton = true, HideRemoveButton = true, Draggable = true)]
        private List<Parameter> m_Parameters = new();
        private Dictionary<int, Parameter> m_ParametersLookup;

        [Range(1f, 124f)]
        [LabelWidth(130)]
        public float m_SendRate = 15;

        [LabelWidth(130)]
        public AuthorityMode authorityMode = AuthorityMode.Owner;

        protected override void OnAwake()
        {
            m_Animator = GetComponent<Animator>();
            m_ParametersLookup = new Dictionary<int, Parameter>(m_Parameters.Count);

            foreach (var p in m_Parameters)
                m_ParametersLookup[p.NameHash] = p;
        }

        protected override void OnStart()
        {
            var parameters = m_Animator.parameters;
            m_FloatParameters = parameters.Where(p => p.type == AnimatorControllerParameterType.Float).OrderBy(p => p.nameHash).ToArray();
            m_BoolParameters = parameters.Where(p => p.type == AnimatorControllerParameterType.Bool).OrderBy(p => p.nameHash).ToArray();
            m_IntParameters = parameters.Where(p => p.type == AnimatorControllerParameterType.Int).OrderBy(p => p.nameHash).ToArray();
        }

        private float m_Time;
        void Update()
        {
            if ((IsMine && authorityMode == AuthorityMode.Owner) || (IsServer && authorityMode == AuthorityMode.Server))
            {
                m_Time += Time.deltaTime;
                float sendRate = 1f / m_SendRate;
                if (m_Time >= sendRate)
                {
                    WriteParameters();
                    m_Time -= sendRate; // avoid drift
                }
            }
        }

        private void WriteParameters()
        {
            // FLOATS
            Delta8<float> f_Delta = new()
            {
                a = GetFloatByIndex(0),
                b = GetFloatByIndex(1),
                c = GetFloatByIndex(2),
                d = GetFloatByIndex(3),
                e = GetFloatByIndex(4),
                f = GetFloatByIndex(5),
                g = GetFloatByIndex(6),
                h = GetFloatByIndex(7),
            };

            // BOOLS
            Delta8<bool> b_Delta = new()
            {
                a = GetBoolByIndex(0),
                b = GetBoolByIndex(1),
                c = GetBoolByIndex(2),
                d = GetBoolByIndex(3),
                e = GetBoolByIndex(4),
                f = GetBoolByIndex(5),
                g = GetBoolByIndex(6),
                h = GetBoolByIndex(7),
            };

            // INTS
            Delta8<int> i_Delta = new()
            {
                a = GetIntByIndex(0),
                b = GetIntByIndex(1),
                c = GetIntByIndex(2),
                d = GetIntByIndex(3),
                e = GetIntByIndex(4),
                f = GetIntByIndex(5),
                g = GetIntByIndex(6),
                h = GetIntByIndex(7),
            };

            using DataBuffer finalBlock = Rent();

            bool fChanged = f_Delta.Write(ref f_l_Delta, finalBlock);
            bool bChanged = b_Delta.Write(ref b_l_Delta, finalBlock);
            bool iChanged = i_Delta.Write(ref i_l_Delta, finalBlock);

            if (fChanged || bChanged || iChanged)
            {
                if (authorityMode == AuthorityMode.Owner) Client.Rpc(1, finalBlock);
                else Server.Rpc(1, finalBlock);
            }
        }

        private void ReadParameters(DataBuffer message)
        {
            // FLOATS
            Delta8<float> f_Delta = Delta8<float>.Read(ref f_l_Delta, message);
            SetFloatByIndex(0, f_Delta.a);
            SetFloatByIndex(1, f_Delta.b);
            SetFloatByIndex(2, f_Delta.c);
            SetFloatByIndex(3, f_Delta.d);
            SetFloatByIndex(4, f_Delta.e);
            SetFloatByIndex(5, f_Delta.f);
            SetFloatByIndex(6, f_Delta.g);
            SetFloatByIndex(7, f_Delta.h);

            // BOOLS
            Delta8<bool> b_Delta = Delta8<bool>.Read(ref b_l_Delta, message);
            SetBoolByIndex(0, b_Delta.a);
            SetBoolByIndex(1, b_Delta.b);
            SetBoolByIndex(2, b_Delta.c);
            SetBoolByIndex(3, b_Delta.d);
            SetBoolByIndex(4, b_Delta.e);
            SetBoolByIndex(5, b_Delta.f);
            SetBoolByIndex(6, b_Delta.g);
            SetBoolByIndex(7, b_Delta.h);

            // INTS
            Delta8<int> i_Delta = Delta8<int>.Read(ref i_l_Delta, message);
            SetIntByIndex(0, i_Delta.a);
            SetIntByIndex(1, i_Delta.b);
            SetIntByIndex(2, i_Delta.c);
            SetIntByIndex(3, i_Delta.d);
            SetIntByIndex(4, i_Delta.e);
            SetIntByIndex(5, i_Delta.f);
            SetIntByIndex(6, i_Delta.g);
            SetIntByIndex(7, i_Delta.h);
        }

        private bool WriteLastState(out DataBuffer finalBlock)
        {
            Delta8<bool> b_d_Delta = default;
            Delta8<float> f_d_Delta = default;
            Delta8<int> i_d_Delta = default;

#pragma warning disable OMNI061
            finalBlock = Rent();
#pragma warning restore OMNI061
            bool fChanged = f_l_Delta.Write(ref f_d_Delta, finalBlock);
            bool bChanged = b_l_Delta.Write(ref b_d_Delta, finalBlock);
            bool iChanged = i_l_Delta.Write(ref i_d_Delta, finalBlock);
            return fChanged || bChanged || iChanged;
        }

        protected override void OnServerObjectSpawnedForPeer(NetworkPeer peer)
        {
            // if (authorityMode == AuthorityMode.Server)
            // {
            //     if (WriteLastState(out var finalBlock))
            //     {
            //         using (finalBlock)
            //         {
            //             Server.RpcToPeer(1, peer, finalBlock);
            //             print("Enviando ultimo estado de animator para peer " + peer.Id);
            //         }
            //     }
            // }

           // using RpcBuffer buffer = Rent();
           // buffer.Write(18);
           // buffer.WriteString("Hola Ruan");
            //Test(buffer);
        }

        [Client(2)]
        void TestRpcA(ReadOnlyDataBuffer buffer)
        {
           // print("value: " + buffer.Read<int>());
           // print("value: " + buffer.ReadString());
        }

        protected override void OnOwnerObjectSpawnedForPeer(int peerId)
        {
            //if (authorityMode == AuthorityMode.Owner)
            //{
            //    if (WriteLastState(out var finalBlock))
            //    {
            //        using (finalBlock)
            //        {
            //            Client.Rpc(1, finalBlock);
            //            print("Enviando ultimo estado de animator para peer " + peerId);
            //        }
            //    }
            //}

            //OnClientRpc(DataBuffer.Empty);
        }

        [Client(1, DeliveryMode = DeliveryMode.Unreliable)]
        void OnClientRpc(DataBuffer message)
        {
            ReadParameters(message);
        }

        [Server(1, DeliveryMode = DeliveryMode.Unreliable)]
        void OnServerRpc(DataBuffer message)
        {
            ReadParameters(message);
        }

        private bool IsSyncEnable(int hash)
        {
            return m_ParametersLookup.TryGetValue(hash, out var p) && p.Sync;
        }

        private float GetFloatByIndex(int index)
        {
            if (index < 0 || index >= m_FloatParameters.Length)
                return 0;

            int hash = m_FloatParameters[index].nameHash;
            return IsSyncEnable(hash) ? m_Animator.GetFloat(hash) : 0f;
        }

        private void SetFloatByIndex(int index, float value)
        {
            if (index < 0 || index >= m_FloatParameters.Length)
                return;

            m_Animator.SetFloat(m_FloatParameters[index].nameHash, value);
        }

        private bool GetBoolByIndex(int index)
        {
            if (index < 0 || index >= m_BoolParameters.Length)
                return false;

            int hash = m_BoolParameters[index].nameHash;
            return IsSyncEnable(hash) && m_Animator.GetBool(hash);
        }

        private void SetBoolByIndex(int index, bool value)
        {
            if (index < 0 || index >= m_BoolParameters.Length)
                return;

            m_Animator.SetBool(m_BoolParameters[index].nameHash, value);
        }

        private int GetIntByIndex(int index)
        {
            if (index < 0 || index >= m_IntParameters.Length)
                return 0;

            int hash = m_IntParameters[index].nameHash;
            return IsSyncEnable(hash) ? m_Animator.GetInteger(hash) : 0;
        }

        private void SetIntByIndex(int index, int value)
        {
            if (index < 0 || index >= m_IntParameters.Length)
                return;

            m_Animator.SetInteger(m_IntParameters[index].nameHash, value);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_Animator == null)
                m_Animator = GetComponent<Animator>();

            if (m_Animator == null)
                return;

            var parameters = m_Animator.parameters;
            m_Parameters ??= new List<Parameter>();
            foreach (var p in parameters.Where(p => p.type != AnimatorControllerParameterType.Trigger))
            {
                if (!m_Parameters.Any(x => x.Name == p.name))
                {
                    m_Parameters.Add(new Parameter
                    {
                        Name = p.name,
                        NameHash = p.nameHash,
                        Sync = true
                    });
                }
            }

            m_Parameters.RemoveAll(x => !parameters.Any(p => p.name == x.Name));
#if UNITY_EDITOR
            NetworkHelper.EditorSaveObject(gameObject);
#endif
        }
    }
}
