using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Omni.Core
{
    internal interface IPropertyMemberInfo
    {
        string Name { get; }
        byte Id { get; }
    }

    internal interface IPropertyMemberInfo<T>
    {
        public Func<T> GetFunc { get; set; }
    }

    internal class PropertyMemberInfo<T> : IPropertyMemberInfo, IPropertyMemberInfo<T>
    {
        internal PropertyMemberInfo(string name, byte id)
        {
            Name = name;
            Id = id;
        }

        public Func<T> GetFunc { get; set; }
        public string Name { get; }
        public byte Id { get; }
    }

    public class NetVarBehaviour : MonoBehaviour
    {
        internal Dictionary<string, IPropertyMemberInfo> Properties { get; } = new();

        internal NetworkBuffer CreateHeader<T>(T @object, byte id)
        {
            NetworkBuffer message = NetworkManager.Pool.Rent();
            message.FastWrite(id);
            message.ToBinary(@object);
            return message;
        }

        internal IPropertyMemberInfo GetPropertyInfoWithCallerName<T>(string callerName)
        {
            if (!Properties.TryGetValue(callerName, out IPropertyMemberInfo memberInfo))
            {
                PropertyInfo propertyInfo =
                    GetType()
                        .GetProperty(
                            callerName,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                        )
                    ?? throw new NullReferenceException(
                        $"NetVar: Property not found: {callerName}. Use the other overload of this function."
                    );

                string name = propertyInfo.Name;
                NetVarAttribute attribute =
                    propertyInfo.GetCustomAttribute<NetVarAttribute>()
                    ?? throw new NullReferenceException(
                        $"NetVar: NetVarAttribute not found on property: {name}."
                    );

                byte id = attribute.Id;
                memberInfo = new PropertyMemberInfo<T>(name, id);
                ((IPropertyMemberInfo<T>)memberInfo).GetFunc =
                    propertyInfo.GetGetMethod().CreateDelegate(typeof(Func<T>), this) as Func<T>;
                Properties.Add(callerName, memberInfo);
                return memberInfo;
            }
            else
            {
                return memberInfo;
            }
        }

#pragma warning disable IDE1006
        protected virtual void ___OnPropertyChanged___(NetworkBuffer buffer, NetworkPeer peer)
#pragma warning restore IDE1006
        {
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

        protected virtual void OnClientPropertyChanged(int id) { }

        protected virtual void OnServerPropertyChanged(int id, NetworkPeer peer) { }
    }
}
