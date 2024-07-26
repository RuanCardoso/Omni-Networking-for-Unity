using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

#pragma warning disable

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

    public class NetworkVariablesBehaviour : MonoBehaviour
    {
        readonly Dictionary<string, IPropertyInfo> properties = new();

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
                        $"NetworkVariable: Property not found: {callerName}. Use the other overload of this function(ManualSync)."
                    );

                string name = propertyInfo.Name;
                NetworkVariableAttribute attribute =
                    propertyInfo.GetCustomAttribute<NetworkVariableAttribute>()
                    ?? throw new NullReferenceException(
                        $"NetworkVariable: NetworkVariableAttribute not found on property: {name}. Ensure it has 'NetworkVariable' attribute."
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

        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        // never override this method!
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        protected virtual void ___OnPropertyChanged___(
            string propertyName,
            byte propertyId,
            NetworkPeer peer,
            DataBuffer buffer
        )
        {
            // The overriden method must call base(SourceGenerator).
            if (peer == null)
            {
                OnClientPropertyChanged(propertyName, propertyId);
            }
            else
            {
                OnServerPropertyChanged(propertyName, propertyId, peer);
            }
        }

        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        // never override this method!
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        [Conditional("UNITY_EDITOR")]
        protected virtual void ___NotifyChange___() { }

        /// <summary>
        /// Handles property changes on the client side based on the provided property ID.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        protected virtual void OnClientPropertyChanged(string propertyName, int propertyId) { }

        /// <summary>
        /// Handles property changes on the server side based on the provided property ID and network peer.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        /// <param name="peer">The network peer associated with the property change.</param>
        protected virtual void OnServerPropertyChanged(
            string propertyName,
            int propertyId,
            NetworkPeer peer
        ) { }
    }
}
