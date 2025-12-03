using System;
using System.Collections.Generic;
using UnityEngine;
using Omni.Shared;
using Newtonsoft.Json.Linq;
using Omni.Collections;
#if OMNI_RELEASE
using System.Runtime.CompilerServices;
#endif

namespace Omni.Core
{
    public static class NetworkExtensions
    {
        private static readonly string[] SizeSuffixes =
        {
            "B/s",
            "kB/s",
            "mB/s",
            "gB/s",
            "tB/s",
            "pB/s",
            "eB/s",
            "zB/s",
            "yB/s"
        };

        internal static string ToSizeSuffix(this double value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException("decimalPlaces < 0");
            }

            if (value < 0)
            {
                return "-" + ToSizeSuffix(-value, decimalPlaces);
            }

            if (value == 0)
            {
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0);
            }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag)
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        /// <summary>
        /// Retrieves the <see cref="NetworkIdentity"/> component from the root of the transform
        /// affected by the 3D raycast hit.
        /// </summary>
        /// <param name="hit">The <see cref="RaycastHit"/> instance resulting from a raycast operation.</param>
        /// <returns>
        /// The <see cref="NetworkIdentity"/> component located on the root object of the impacted transform,
        /// or <c>null</c> if no <see cref="NetworkIdentity"/> is found.
        /// </returns>
        public static NetworkIdentity GetIdentity(this RaycastHit hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the <see cref="NetworkIdentity"/> component from the root of the transform
        /// impacted by the 2D raycast hit.
        /// </summary>
        /// <returns>
        /// The <see cref="NetworkIdentity"/> component located on the root object of the impacted transform,
        /// or <c>null</c> if no <see cref="NetworkIdentity"/> is found.
        /// </returns>
        public static NetworkIdentity GetIdentity(this RaycastHit2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity associated with the root of the transform involved in the collision.
        /// </summary>
        /// <param name="hit">The Collision object from which to extract the NetworkIdentity.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the collision, or null if not found.</returns>
        public static NetworkIdentity GetIdentity(this Collision hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component associated with the given Collider.
        /// </summary>
        /// <param name="hit">The Collider from which to retrieve the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the Collider, or null if none is found.</returns>
        public static NetworkIdentity GetIdentity(this Collider hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component from the root transform of a Collision2D instance.
        /// </summary>
        /// <param name="hit">The Collision2D instance from which to obtain the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component associated with the root transform of the collision, or null if not found.</returns>
        public static NetworkIdentity GetIdentity(this Collision2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Retrieves the NetworkIdentity component associated with the given Collider2D.
        /// </summary>
        /// <param name="hit">The Collider2D from which to obtain the NetworkIdentity component.</param>
        /// <returns>The NetworkIdentity component attached to the root transform of the specified Collider2D, or null if no such component exists.</returns>
        public static NetworkIdentity GetIdentity(this Collider2D hit)
        {
            return hit.transform.root.GetComponent<NetworkIdentity>();
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static float ScaleDelta(this float input, float multiplier)
        {
            return input * multiplier * Time.deltaTime;
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static Vector3 ScaleDelta(this Vector3 input, float multiplier)
        {
            return multiplier * Time.deltaTime * input;
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static Vector2 ScaleDelta(this Vector2 input, float multiplier)
        {
            return multiplier * Time.deltaTime * input;
        }

        /// <summary>
        /// Scales the input value by the specified multiplier and <see cref="Time.deltaTime"/>.
        /// </summary>
        /// <param name="input">The initial value to be scaled.</param>
        /// <param name="multiplier">Multiplier applied to the input.</param>
        /// <returns>The input value scaled over time.</returns>
        /// <remarks>
        /// Useful for making transformations consistent across frame rates.
        /// </remarks>
        public static Vector4 ScaleDelta(this Vector4 input, float multiplier)
        {
            return multiplier * Time.deltaTime * input;
        }

        /// <summary>
        /// Creates a deep copy of an object through serialization and deserialization.
        /// </summary>
        /// <typeparam name="T">The type of object to clone. Must be serializable.</typeparam>
        /// <param name="obj">The source object to clone.</param>
        /// <param name="useBinarySerializer">
        /// When true, uses binary serialization for better performance.
        /// When false (default), uses JSON serialization for better compatibility.
        /// </param>
        /// <returns>A new instance of type T with all properties deeply copied.</returns>
        /// <exception cref="Exception">Thrown when serialization/deserialization fails.</exception>
        /// <remarks>
        /// Binary serialization is faster but less flexible than JSON serialization.
        /// JSON serialization better handles circular references and complex object graphs.
        /// </remarks>
        /// <example>
        /// var player = new PlayerData { Name = "Player1", Score = 100 };
        /// var clone = player.DeepClone(); // JSON serialization
        /// var fastClone = player.DeepClone(useBinarySerializer: true); // Binary serialization
        /// </example>
        public static T DeepClone<T>(this T obj, bool useBinarySerializer = false)
        {
            try
            {
                if (!useBinarySerializer)
                {
                    string json = NetworkManager.ToJson(obj);
                    return NetworkManager.FromJson<T>(json);
                }

                byte[] data = NetworkManager.ToBinary(obj);
                return NetworkManager.FromBinary<T>(data);
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"Serialization failed for {typeof(T).Name}: {ex.Message}. " +
                    $"Method=DeepClone({(useBinarySerializer ? "Binary" : "JSON")}), Exception={ex.GetType().Name}",
                    NetworkLogger.LogType.Error
                );

                throw;
            }
        }

        private static void ResolveType<T>(ref object @ref)
        {
            if (@ref is T)
                return;

            if (@ref is JObject jObject)
            {
                @ref = jObject.ToObject<T>();
                return;
            }

            if (@ref is JArray jArray)
            {
                @ref = jArray.ToObject<T>();
                return;
            }

            @ref = Convert.ChangeType(@ref, typeof(T)); // dont cast to T => (T)Convert.ChangeType() is redundant.....
        }

        /// <summary>
        /// Retrieves the value associated with the specified key from the dictionary and casts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <returns>The value associated with the specified key, casted to type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method retrieves the value associated with the specified key from the dictionary and casts it to the specified type.
        /// If the key is not found in the dictionary, this method will throw a KeyNotFoundException.
        /// </remarks>
        public static T Get<T>(this IDictionary<string, object> @this, string name)
        {
            var @ref = @this[name];
            try
            {
                ResolveType<T>(ref @ref);
                return (T)@ref;
            }
            catch (InvalidCastException)
            {
                NetworkLogger.__Log__(
                    $"Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                    NetworkLogger.LogType.Error
                );
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"Exception while casting value for key '{name}': {ex.Message}",
                    NetworkLogger.LogType.Error
                );
            }

            return default;
        }

        /// <summary>
        /// Retrieves the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// </summary>
        /// <typeparam name="T">The reference type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <returns>The value associated with the specified key, casted to type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method retrieves the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// It is intended for performance-sensitive scenarios where the type is known at compile-time and type safety is ensured by the caller.
        /// If the key is not found in the dictionary, this method will throw a KeyNotFoundException.
        /// </remarks>
        public static T UnsafeGet<T>(this IDictionary<string, object> @this, string name) where T : class
        {
            var @ref = @this[name];
            try
            {
                ResolveType<T>(ref @ref);
#if OMNI_RELEASE
                return Unsafe.As<T>(@ref);
#else
                return (T)@ref;
#endif
            }
            catch (InvalidCastException)
            {
                NetworkLogger.__Log__(
                    $"Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                    NetworkLogger.LogType.Error
                );
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"Exception while casting value for key '{name}': {ex.Message}",
                    NetworkLogger.LogType.Error
                );
            }

            return default;
        }

        /// <summary>
        /// Tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if the key is found; otherwise, the default value for type <typeparamref name="T"/>.</param>
        /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
        /// <remarks>
        /// This method tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified type.
        /// If the key is found in the dictionary, the value is assigned to the <paramref name="value"/> parameter and the method returns true; otherwise, it returns false.
        /// </remarks>
        public static bool TryGet<T>(this IDictionary<string, object> @this, string name, out T value)
        {
            value = default;
            if (@this.TryGetValue(name, out object @ref))
            {
                try
                {
                    ResolveType<T>(ref @ref);
                    value = (T)@ref;
                    return true;
                }
                catch (InvalidCastException)
                {
                    NetworkLogger.__Log__(
                        $"Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                        NetworkLogger.LogType.Error
                    );
                }
                catch (Exception ex)
                {
                    NetworkLogger.__Log__(
                        $"Exception while casting value for key '{name}': {ex.Message}",
                        NetworkLogger.LogType.Error
                    );
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// </summary>
        /// <typeparam name="T">The reference type to which the value should be cast.</typeparam>
        /// <param name="this">The dictionary to retrieve the value from.</param>
        /// <param name="name">The key of the value to retrieve.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key if the key is found; otherwise, the default value for type <typeparamref name="T"/>.</param>
        /// <returns>True if the dictionary contains an element with the specified key; otherwise, false.</returns>
        /// <remarks>
        /// This method tries to retrieve the value associated with the specified key from the dictionary and casts it to the specified reference type without type checking.
        /// It is intended for performance-sensitive scenarios where the type is known at compile-time and type safety is ensured by the caller.
        /// If the key is found in the dictionary, the value is assigned to the <paramref name="value"/> parameter and the method returns true; otherwise, it returns false.
        /// </remarks>
        public static bool TryUnsafeGet<T>(this IDictionary<string, object> @this, string name, out T value)
            where T : class
        {
            value = default;
            if (@this.TryGetValue(name, out object @ref))
            {
                try
                {
                    ResolveType<T>(ref @ref);
#if OMNI_RELEASE
                    value = Unsafe.As<T>(@ref);
#else
                    value = (T)@ref;
#endif
                    return true;
                }
                catch (InvalidCastException)
                {
                    NetworkLogger.__Log__(
                        $"Failed to cast value for key '{name}' from {@ref?.GetType().Name ?? "null"} to {typeof(T).Name}",
                        NetworkLogger.LogType.Error
                    );
                }
                catch (Exception ex)
                {
                    NetworkLogger.__Log__(
                        $"Exception while casting value for key '{name}': {ex.Message}",
                        NetworkLogger.LogType.Error
                    );
                }
            }

            return false;
        }

        public static ObservableDictionary<TKey, TValue> ToObservableDictionary<TKey, TValue, TSource>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) where TKey : notnull
        {
            var dict = new ObservableDictionary<TKey, TValue>();
            foreach (var item in source)
                dict.Add(keySelector(item), valueSelector(item));
            return dict;
        }
    }
}