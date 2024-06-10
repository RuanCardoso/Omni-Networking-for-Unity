/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Omni.Core
{
    [DefaultExecutionOrder(-500)]
    public class NetworkService : MonoBehaviour
    {
        private static readonly Dictionary<string, object> globalServices = new(); // Service Name

        [SerializeField]
        private string serviceName;

        [SerializeField]
        private bool dontDestroyOnLoad;

        protected virtual void Awake()
        {
            if (!string.IsNullOrEmpty(serviceName))
            {
                globalServices.Add(serviceName, this);
            }

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(this);
            }
        }

        public static T GetService<T>(string serviceName)
        {
            try
            {
                if (globalServices.TryGetValue(serviceName, out var service))
                {
                    return (T)service;
                }
                else
                {
                    throw new Exception(
                        $"Could not find service with name: \"{serviceName}\" you must register the service before using it."
                    );
                }
            }
            catch (InvalidCastException)
            {
                throw new Exception(
                    $"Could not cast service with name: \"{serviceName}\" to type: \"{typeof(T)}\" check if you registered the service with the correct type."
                );
            }
        }

        public static void AddService<T>(T service, string serviceName)
        {
            if (!globalServices.TryAdd(serviceName, service))
            {
                throw new Exception(
                    $"Could not add service with name: \"{serviceName}\" because it already exists."
                );
            }
        }

        public static void UpdateService<T>(T service, string serviceName)
        {
            if (globalServices.ContainsKey(serviceName))
            {
                globalServices[serviceName] = service;
            }
            else
            {
                throw new Exception(
                    $"Could not update service with name: \"{serviceName}\" because it does not exist."
                );
            }
        }

        public static bool DeleteService(string serviceName)
        {
            return globalServices.Remove(serviceName);
        }
    }
}
