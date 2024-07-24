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
using Omni.Core.Interfaces;
using Omni.Shared;
using UnityEngine;
#if OMNI_RELEASE
using System.Runtime.CompilerServices;
#endif

namespace Omni.Core
{
    public static class Service
    {
        /// <summary>
        /// Called when a service is added or updated, can be called multiple times.
        /// Be sure to unsubscribe to avoid double subscriptions. <br/><br/>
        /// - Subscribers should be called from the <c>OnAwake</c> method.<br/>
        /// - Unsubscribers should be called from the <c>OnStop</c> method.<br/>
        /// </summary>
        public static event Action<string> OnReferenceChanged;

        public static void UpdateReference(string componentName)
        {
            OnReferenceChanged?.Invoke(componentName);
        }
    }

    public static partial class NetworkService
    {
        public static void GetAsComponent<T>(out T service)
            where T : class
        {
            GetAsComponent<T>(typeof(T).Name, out service);
        }

        public static void GetAsComponent<T>(string componentName, out T service)
            where T : class
        {
            service = Get<INetworkComponentService>(componentName).Component as T;
        }

        public static T GetAsComponent<T>()
            where T : class
        {
            return GetAsComponent<T>(typeof(T).Name);
        }

        public static T GetAsComponent<T>(string componentName)
            where T : class
        {
            return Get<INetworkComponentService>(componentName).Component as T;
        }

        public static bool TryGetAsComponent<T>(out T service)
            where T : class
        {
            return TryGetAsComponent<T>(typeof(T).Name, out service);
        }

        public static bool TryGetAsComponent<T>(string componentName, out T service)
            where T : class
        {
            service = null;
            bool success =
                TryGet<INetworkComponentService>(componentName, out var componentService)
                && componentService.Component is T;

            if (success)
            {
                service = componentService.Component as T;
            }

            return success;
        }

        public static GameObject GetAsGameObject<T>()
        {
            return GetAsGameObject(typeof(T).Name);
        }

        public static GameObject GetAsGameObject(string gameObjectName)
        {
            return Get<INetworkComponentService>(gameObjectName).GameObject;
        }

        public static bool TryGetAsGameObject<T>(out GameObject service)
        {
            return TryGetAsGameObject(typeof(T).Name, out service);
        }

        public static bool TryGetAsGameObject(string gameObjectName, out GameObject service)
        {
            service = null;
            bool success = TryGet<INetworkComponentService>(
                gameObjectName,
                out var componentService
            );

            if (success)
            {
                service = componentService.GameObject;
            }

            return success;
        }
    }

    /// <summary>
    /// Service Locator is a pattern used to provide global access to a service instance.
    /// This class provides a static methods to store and retrieve services by name.
    /// </summary>
    public static partial class NetworkService
    {
        // (Service Name, Service Instance)
        private static readonly Dictionary<string, object> m_Services = new();

        /// <summary>
        /// Retrieves a service instance by its name from the service locator.
        /// Throws an exception if the service is not found or cannot be cast to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the service should be cast.</typeparam>
        /// <param name="serviceName">The name of the service to retrieve.</param>
        /// <returns>The service instance cast to the specified type.</returns>
        /// <exception cref="Exception">
        /// Thrown if the service is not found or cannot be cast to the specified type.
        /// </exception>
        public static T Get<T>(string serviceName)
            where T : class
        {
            try
            {
                if (m_Services.TryGetValue(serviceName, out object service))
                {
#if OMNI_RELEASE
                    return Unsafe.As<T>(service);
#else
                    return (T)service;
#endif
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

        /// <summary>
        /// Attempts to retrieve a service instance by its name from the service locator.
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve.</typeparam>
        /// <param name="serviceName">The name of the service to retrieve.</param>
        /// <param name="service">When this method returns, contains the service instance cast to the specified type if the service is found; otherwise, the default value for the type of the service parameter.</param>
        /// <returns>True if the service is found and successfully cast to the specified type; otherwise, false.</returns>
        public static bool TryGet<T>(string serviceName, out T service)
            where T : class
        {
            service = default;
            if (m_Services.TryGetValue(serviceName, out object @obj))
            {
                if (@obj is T)
                {
                    service = Get<T>(serviceName);
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Retrieves a service instance by its type name from the service locator.
        /// </summary>
        /// <typeparam name="T">The type to which the service should be cast.</typeparam>
        /// <returns>The service instance cast to the specified type.</returns>
        /// <exception cref="Exception">
        /// Thrown if the service is not found or cannot be cast to the specified type.
        /// </exception>
        public static T Get<T>()
            where T : class
        {
            return Get<T>(typeof(T).Name);
        }

        /// <summary>
        /// Attempts to retrieve a service instance by its type from the service locator.
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve.</typeparam>
        /// <param name="service">When this method returns, contains the service instance cast to the specified type if the service is found; otherwise, the default value for the type of the service parameter.</param>
        /// <returns>True if the service is found and successfully cast to the specified type; otherwise, false.</returns>
        public static bool TryGet<T>(out T service)
            where T : class
        {
            service = default;
            string serviceName = typeof(T).Name;
            if (m_Services.TryGetValue(serviceName, out object @obj))
            {
                if (@obj is T)
                {
                    service = Get<T>(serviceName);
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Adds a new service instance to the service locator with a specified name.
        /// Throws an exception if a service with the same name already exists.
        /// </summary>
        /// <typeparam name="T">The type of the service to add.</typeparam>
        /// <param name="service">The service instance to add.</param>
        /// <param name="serviceName">The name to associate with the service instance.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name already exists.
        /// </exception>
        public static void Register<T>(T service, string serviceName)
        {
            if (!m_Services.TryAdd(serviceName, service))
            {
                throw new Exception(
                    $"Could not add service with name: \"{serviceName}\" because it already exists."
                );
            }
        }

        /// <summary>
        /// Attempts to retrieve adds a new service instance to the service locator with a specified name.
        /// </summary>
        /// <typeparam name="T">The type of the service to add.</typeparam>
        /// <param name="service">The service instance to add.</param>
        /// <param name="serviceName">The name to associate with the service instance.</param>
        public static bool TryRegister<T>(T service, string serviceName)
        {
            return m_Services.TryAdd(serviceName, service);
        }

        /// <summary>
        /// Updates an existing service instance in the service locator with a specified name.
        /// Throws an exception if a service with the specified name does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the service to update.</typeparam>
        /// <param name="service">The new service instance to associate with the specified name.</param>
        /// <param name="serviceName">The name associated with the service instance to update.</param>
        /// <exception cref="Exception">
        /// Thrown if a service with the specified name does not exist in the.
        /// </exception>
        public static void Update<T>(T service, string serviceName)
        {
            if (m_Services.ContainsKey(serviceName))
            {
                m_Services[serviceName] = service;
            }
            else
            {
                throw new Exception(
                    $"Could not update service with name: \"{serviceName}\" because it does not exist."
                );
            }
        }

        /// <summary>
        /// Attempts to retrieve updates an existing service instance in the service locator with a specified name.
        /// </summary>
        /// <typeparam name="T">The type of the service to update.</typeparam>
        /// <param name="service">The new service instance to associate with the specified name.</param>
        /// <param name="serviceName">The name associated with the service instance to update.</param>
        public static bool TryUpdate<T>(T service, string serviceName)
        {
            if (m_Services.ContainsKey(serviceName))
            {
                m_Services[serviceName] = service;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deletes a service instance from the service locator by its name.
        /// </summary>
        /// <param name="serviceName">The name of the service to delete.</param>
        /// <returns>True if the service was successfully removed; otherwise, false.</returns>
        public static bool Unregister(string serviceName)
        {
            return m_Services.Remove(serviceName);
        }

        /// <summary>
        /// Determines whether a service with the specified name exists in the service locator.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static bool Exists(string serviceName)
        {
            return m_Services.ContainsKey(serviceName);
        }
    }
}
