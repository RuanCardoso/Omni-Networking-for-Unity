using Omni.Shared;
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
        Func<T> Invoke { get; set; }
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
        private readonly Dictionary<string, IPropertyInfo> properties = new();

        internal DataBuffer CreateHeader<T>(T @object, byte id)
        {
            DataBuffer message = NetworkManager.Pool.Rent(); // disposed by the caller
            message.Write(id);

            // If the object implements ISerializable, serialize it.
            if (@object is IMessage data)
            {
                using var serializedData = data.Serialize();
                serializedData.CopyTo(message);
                return message;
            }

            message.WriteAsBinary(@object);
            return message;
        }

        internal IPropertyInfo GetPropertyInfoWithCallerName<T>(string callerName, BindingFlags flags)
        {
            if (!properties.TryGetValue(callerName, out IPropertyInfo memberInfo))
            {
                // Reflection is slow, but cached for performance optimization!
                // Delegates are used to avoid reflection overhead, it is much faster, like a direct call.

                PropertyInfo propertyInfo = GetType().GetProperty(callerName, (System.Reflection.BindingFlags)flags) ??
                                            throw new NullReferenceException(
                                                $"NetworkVariable: Property not found: {callerName}. Use the other overload of this function.");

                string name = propertyInfo.Name;
                NetworkVariableAttribute attribute = propertyInfo.GetCustomAttribute<NetworkVariableAttribute>() ??
                                                     throw new NullReferenceException(
                                                         $"NetworkVariable: NetworkVariableAttribute not found on property: {name}. Ensure it has 'NetworkVariable' attribute.");

                MethodInfo getMethod = propertyInfo.GetMethod;
                if (getMethod == null)
                {
                    throw new NullReferenceException(
                        $"NetworkVariable: GetMethod not found on property: {name}. Ensure it has 'NetworkVariable' attribute."
                    );
                }

                byte id = attribute.Id;
                if (id <= 0)
                {
                    throw new ArgumentException($"NetworkVariable: Id must be greater than 0.");
                }

                memberInfo = new PropertyInfo<T>(name, id);
                // hack: performance optimization!
                ((IPropertyInfo<T>)memberInfo).Invoke = getMethod.CreateDelegate(typeof(Func<T>), this) as Func<T>;
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
        protected virtual void ___OnPropertyChanged___(string propertyName, byte propertyId, NetworkPeer peer,
            DataBuffer buffer)
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
        protected virtual void ___NotifyChange___()
        {
        }

        // This property is intended to be used by the source generator to determine if the method has been called from the editor.
        protected bool ___NotifyEditorChange___Called { get; set; } = false;

        // This method is intended to be overridden by the caller using source generators and reflection techniques. Magic wow!
        // https://github.com/RuanCardoso/OmniNetSourceGenerator
        // never override this method!
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't override this method! The source generator will override it.")]
        [Conditional("UNITY_EDITOR")]
        protected virtual void ___NotifyEditorChange___()
        {
            // Obsolete: Editor is fully supported!
            ___NotifyEditorChange___Called = false;
        }

        /// <summary>
        /// Handles property changes on the client side based on the provided property ID.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        protected virtual void OnClientPropertyChanged(string propertyName, int propertyId)
        {
        }

        /// <summary>
        /// Handles property changes on the server side based on the provided property ID and network peer.
        /// </summary>
        /// <param name="id">The ID of the property that has changed.</param>
        /// <param name="peer">The network peer associated with the property change.</param>
        protected virtual void OnServerPropertyChanged(string propertyName, int propertyId, NetworkPeer peer)
        {
        }

        /// <summary>
        /// Synchronizes the current state of all network variables and other relevant states, 
        /// ensuring that any updates are transmitted to connected clients. This method triggers 
        /// notifications for registered listeners, allowing clients to receive and apply the 
        /// latest server-side data and changes.
        /// </summary>
        protected void SyncNetworkState()
        {
            ___NotifyChange___();
        }

        /// <summary>
        /// Compares two values of type T for deep equality.
        /// </summary>
        /// <returns>True if the values are deeply equal; otherwise, false.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't use this method! The source generator will use it.")]
        protected bool DeepEquals<T>(T oldValue, T newValue, string name)
        {
            if (!___NotifyEditorChange___Called)
            {
                return OnNetworkVariableDeepEquals(oldValue, newValue, name);
            }

            // Obsolete: Editor is fully supported!
#if OMNI_DEBUG
            NetworkLogger.__Log__(
                $"[NetworkVariable({name})] -> Editing network variables through the Unity Editor (Inspector) is not fully allowed. " +
                "Any modifications made in this manner will cause all network variables to synchronize automatically. " +
                "Network variables should only be updated via scripts to ensure consistency and prevent unintended behavior." +
                "Editing via the Unity Editor (Inspector) is permitted solely for debugging purposes; however, be aware that this will synchronize all network variables, including those that have not been changed.",
                NetworkLogger.LogType.Warning);
#else
			throw new NotSupportedException("[NetworkVariable] -> Editing network variables through the Unity Editor (Inspector) is not supported.");
#endif
            return false;
        }

        /// <summary>
        /// Compares two values of type T for deep equality for network variables.
        /// </summary>
        /// <typeparam name="T">The type of the values to compare.</typeparam>
        /// <param name="oldValue">The old value to compare.</param>
        /// <param name="newValue">The new value to compare.</param>
        /// <param name="name">The name of the network variable.</param>
        /// <returns>True if the values are deeply equal; otherwise, false.</returns>
        protected virtual bool OnNetworkVariableDeepEquals<T>(T oldValue, T newValue, string name)
        {
            return typeof(T).IsPrimitive && oldValue.Equals(newValue);
        }
    }
}