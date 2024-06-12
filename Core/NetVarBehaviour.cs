using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Omni.Core
{
    // Hacky: DIRTY CODE!
    // This class utilizes unconventional methods to minimize boilerplate code, reflection, and source generation.
    // Despite its appearance, this approach is essential to achieve high performance.
    // Avoid refactoring as these techniques are crucial for optimizing execution speed.
    // Works with il2cpp.

    internal interface IPropertyInfo
    {
        string Name { get; }
        byte Id { get; }
    }

    internal interface IPropertyInfo<T>
    {
        public Func<T> Invoke { get; set; }
    }

    internal class PropertyInfo<T> : IPropertyInfo, IPropertyInfo<T>
    {
        internal PropertyInfo(string name, byte id)
        {
            Name = name;
            Id = id;
        }

        public Func<T> Invoke { get; set; }
        public string Name { get; }
        public byte Id { get; }
    }

    public class NetVarBehaviour : MonoBehaviour
    {
        private readonly Dictionary<string, IPropertyInfo> properties = new();

        internal DataBuffer CreateHeader<T>(T @object, byte id)
        {
            DataBuffer message = NetworkManager.Pool.Rent(); // disposed by the caller
            message.FastWrite(id);
            message.ToBinary(@object);
            return message;
        }

        internal IPropertyInfo GetPropertyInfoWithCallerName<T>(string callerName)
        {
            if (!properties.TryGetValue(callerName, out IPropertyInfo memberInfo))
            {
                // Reflection is slow, but cached for performance optimization!
                // Delegates are used to avoid reflection overhead, it is much faster, like a direct call.

                PropertyInfo propertyInfo =
                    GetType()
                        .GetProperty(
                            callerName,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        )
                    ?? throw new NullReferenceException(
                        $"NetVar: Property not found: {callerName}. Use the other overload of this function(ManualSync)."
                    );

                string name = propertyInfo.Name;
                NetVarAttribute attribute =
                    propertyInfo.GetCustomAttribute<NetVarAttribute>()
                    ?? throw new NullReferenceException(
                        $"NetVar: NetVarAttribute not found on property: {name}. Ensure it has 'NetVar' attribute."
                    );

                byte id = attribute.Id;
                memberInfo = new PropertyInfo<T>(name, id);
                ((IPropertyInfo<T>)memberInfo).Invoke =
                    propertyInfo.GetGetMethod().CreateDelegate(typeof(Func<T>), this) as Func<T>; // hack: performance optimization!
                properties.Add(callerName, memberInfo);
                return memberInfo;
            }

            return memberInfo;
        }

#pragma warning disable IDE1006
        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        protected virtual void ___OnPropertyChanged___(DataBuffer buffer, NetworkPeer peer)
#pragma warning restore IDE1006
        {
            // The overriden method must call base(SourceGenerator).
            buffer.ResetReadPosition();
            byte propertyId = buffer.Read<byte>();
            if (peer == null)
            {
                OnClientPropertyChanged(propertyId);
            }
            else
            {
                OnServerPropertyChanged(propertyId, peer);
            }
        }

        /// <summary>
        /// Handles property changes on the client side based on the provided property ID.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        protected virtual void OnClientPropertyChanged(int id) { }

        /// <summary>
        /// Handles property changes on the server side based on the provided property ID and network peer.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        /// <param name="peer">The network peer associated with the property change.</param>
        protected virtual void OnServerPropertyChanged(int id, NetworkPeer peer) { }
    }
}
