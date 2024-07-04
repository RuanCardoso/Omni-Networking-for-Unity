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
using Omni.Shared;
using UnityEngine;
#if OMNI_RELEASE
using System.Runtime.CompilerServices;
#endif

namespace Omni.Core
{
    /// <summary>
    /// Service Locator is a pattern used to provide global access to a service instance.
    /// This class provides a static methods to store and retrieve services by name.
    /// </summary>
    [DefaultExecutionOrder(-10500)]
    public class NetworkService : MonoBehaviour
    {
        // (Service Name, Service Instance)
        private static readonly Dictionary<string, object> m_Services = new();

        [Header("Service Settings")]
        [SerializeField]
        private string m_ServiceName;

        [SerializeField]
        private bool m_DontDestroyOnLoad;

        [SerializeField]
        private bool m_KeepOldInstanceReference = false;

        protected virtual void Awake()
        {
            InitializeServiceLocator();
        }

        /// <summary>
        /// Adds the current instance to the service locator using the provided service name.
        /// If `dontDestroyOnLoad` is set to true, the instance will persist across scene loads.
        /// Called automatically by <c>Awake</c>, if you override <c>Awake</c> call this method yourself.
        /// </summary>
        protected void InitializeServiceLocator()
        {
            if (TryRegister(this, m_ServiceName))
            {
                if (m_DontDestroyOnLoad)
                {
                    if (transform.root == transform)
                    {
                        DontDestroyOnLoad(gameObject);
                    }
                    else
                    {
                        NetworkLogger.__Log__(
                            "Service: Only the root object can be set to DontDestroyOnLoad",
                            NetworkLogger.LogType.Error
                        );
                    }
                }
            }
            else
            {
                if (m_DontDestroyOnLoad)
                {
                    if (m_KeepOldInstanceReference)
                    {
                        // Keep the old reference, destroy the new one.
                        Destroy(gameObject);
                    }
                    else
                    {
                        // Keep/Update the current reference, destroy the old one.
                        var oldRef = Get<NetworkService>(m_ServiceName);
                        if (oldRef != null && oldRef is MonoBehaviour unityObject)
                        {
                            Destroy(unityObject.gameObject);
                        }

                        DontDestroyOnLoad(gameObject);
                        Update(this, m_ServiceName);
                    }
                }
                else
                {
                    // Every keep the new reference.
                    Update(this, m_ServiceName);
                }
            }
        }

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

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(m_ServiceName))
            {
                m_ServiceName = GetType().Name;
            }
        }

        protected virtual void Reset()
        {
            OnValidate();
        }
    }
}
