using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Omni.Execution;

namespace Omni.Core
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Determines the method used to map the results of database queries.
        /// If set to true, Dapper will be used for object mapping, providing high performance
        /// due to its lightweight nature and direct execution of SQL queries. Note that
        /// Dapper is partially compatible with IL2CPP.
        /// If set to false, Newtonsoft.Json(Works with IL2CPP) will be used, which offers flexibility and
        /// ease of use for complex object mapping through JSON serialization.
        /// Default value is <c>true</c>.
        /// </summary>
        public static bool UseDapper { get; set; } = true;

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
            return (T)@this[name];
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
        public static T FastGet<T>(this IDictionary<string, object> @this, string name)
            where T : class
        {
            var @ref = @this[name];
            return Unsafe.As<T>(@ref);
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
        public static bool TryGet<T>(
            this IDictionary<string, object> @this,
            string name,
            out T value
        )
        {
            if (@this.TryGetValue(name, out object @ref))
            {
                value = (T)@ref;
                return true;
            }

            value = default;
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
        public static bool TryFastGet<T>(
            this IDictionary<string, object> @this,
            string name,
            out T value
        )
            where T : class
        {
            if (@this.TryGetValue(name, out object @ref))
            {
                value = Unsafe.As<T>(@ref);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Maps the given object to the specified type using JSON serialization and deserialization.
        /// </summary>
        /// <typeparam name="T">The type to which the object should be mapped.</typeparam>
        /// <param name="result">The object to be mapped.</param>
        /// <returns>The mapped object of type <typeparamref name="T"/>.</returns>
        public static T MapJson<T>(object result)
        {
            string jsonString = JsonConvert.SerializeObject(result);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// Maps the given object to the specified type using JSON serialization and deserialization.
        /// </summary>
        /// <typeparam name="T">The type to which the object should be mapped.</typeparam>
        /// <param name="result">The object to be mapped.</param>
        /// <returns>The mapped object of type <typeparamref name="T"/>.</returns>
        public static async Task<T> MapJsonAsync<T>(Task<object> task)
        {
            var taskResult = await task;
            return await Task.Run(() =>
            {
                string jsonString = JsonConvert.SerializeObject(taskResult);
                return JsonConvert.DeserializeObject<T>(jsonString);
            });
        }

        /// <summary>
        /// Maps the given object to the specified type using JSON serialization and deserialization.
        /// </summary>
        /// <typeparam name="T">The type to which the object should be mapped.</typeparam>
        /// <param name="result">The object to be mapped.</param>
        /// <returns>The mapped object of type <typeparamref name="T"/>.</returns>
        private static async Task<T> MapJsonAsync<T>(Task<IEnumerable<object>> task)
        {
            var taskResult = await task;
            return await Task.Run(() =>
            {
                string jsonString = JsonConvert.SerializeObject(taskResult);
                return JsonConvert.DeserializeObject<T>(jsonString);
            });
        }

        /// <summary>
        /// Executes the provided query and maps the first result to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the query result should be mapped.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <returns>The first result of the query mapped to the specified type <typeparamref name="T"/>. If no result is found, returns the default value for the type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method executes the given query and retrieves the first result, mapping it to the specified type <typeparamref name="T"/>.
        /// If the query does not return any results, the default value for the specified type <typeparamref name="T"/> is returned.
        /// The method optionally accepts a database transaction and a command timeout value, which can be used to control the execution context of the query.
        /// </remarks>
        public static T First<T>(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (UseDapper)
            {
                return query.FirstOrDefault<T>(transaction, timeout);
            }

            var @ref = query.FirstOrDefault<object>(transaction, timeout);
            return MapJson<T>(@ref);
        }

        /// <summary>
        /// Asynchronously executes the provided query and maps the first result to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the query result should be mapped.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the first result of the query mapped to the specified type <typeparamref name="T"/>. If no result is found, returns the default value for the type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method asynchronously executes the given query and retrieves the first result, mapping it to the specified type <typeparamref name="T"/>.
        /// If the query does not return any results, the default value for the specified type <typeparamref name="T"/> is returned.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token to control the execution context of the query.
        /// </remarks>
        public static Task<T> FirstAsync<T>(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            if (UseDapper)
            {
                return query.FirstOrDefaultAsync<T>(transaction, timeout, token);
            }

            return MapJsonAsync<T>(query.FirstOrDefaultAsync<object>(transaction, timeout, token));
        }

        /// <summary>
        /// Maps all the results of a database query to a collection of objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects to map the results to.</typeparam>
        /// <param name="query">The database query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <returns>A collection of objects of the specified type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method executes the given query and retrieves all the results, mapping them to a collection of objects of the specified type <typeparamref name="T"/>.
        /// If no results are found, an empty collection is returned.
        /// The method optionally accepts a database transaction and a command timeout value, which can be used to control the execution context of the query.
        /// </remarks>
        public static IEnumerable<T> All<T>(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (UseDapper)
            {
                return QueryExtensions.Get<T>(query, transaction, timeout);
            }

            var @ref = QueryExtensions.Get<object>(query, transaction, timeout);
            return MapJson<IEnumerable<T>>(@ref);
        }

        /// <summary>
        /// Asynchronously maps all the results of a database query to a collection of objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects to map the results to.</typeparam>
        /// <param name="query">The database query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains a collection of objects of the specified type <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// This method asynchronously executes the given query and retrieves all the results, mapping them to a collection of objects of the specified type <typeparamref name="T"/>.
        /// If no results are found, an empty collection is returned.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token to control the execution context of the query.
        /// </remarks>
        public static Task<IEnumerable<T>> AllAsync<T>(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            if (UseDapper)
            {
                return QueryExtensions.GetAsync<T>(query, transaction, timeout, token);
            }

            return MapJsonAsync<IEnumerable<T>>(
                QueryExtensions.GetAsync<object>(query, transaction, timeout, token)
            );
        }

        /// <summary>
        /// Maps the paginated results of a query to a collection of objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to map the results to.</typeparam>
        /// <param name="query">The query to paginate.</param>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="perPage">The number of results per page. Default is 25.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <returns>A collection of objects of the specified type <typeparamref name="T"/> corresponding to the specified page.</returns>
        /// <remarks>
        /// This method paginates the results of the given query and maps them to a collection of objects of the specified type <typeparamref name="T"/>.
        /// It retrieves the specified page of results, with the number of results per page determined by the <paramref name="perPage"/> parameter.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the query.
        /// </remarks>
        public static PaginationResult<T> Page<T>(
            this Query query,
            int page,
            int perPage = 25,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (UseDapper)
            {
                return query.Paginate<T>(page, perPage, transaction, timeout);
            }

            PaginationResult<object> result = query.Paginate<object>(
                page,
                perPage,
                transaction,
                timeout
            );

            var pageResult = new PaginationResult<T>
            {
                Query = result.Query,
                Count = result.Count,
                PerPage = result.PerPage,
                Page = result.Page,
                List = MapJson<IEnumerable<T>>(result.List)
            };

            return pageResult;
        }

        /// <summary>
        /// Asynchronously maps the paginated results of a query to a collection of objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to map the results to.</typeparam>
        /// <param name="query">The query to paginate.</param>
        /// <param name="page">The page number to retrieve.</param>
        /// <param name="perPage">The number of results per page. Default is 25.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains a collection of objects of the specified type <typeparamref name="T"/> corresponding to the specified page.</returns>
        /// <remarks>
        /// This method asynchronously paginates the results of the given query and maps them to a collection of objects of the specified type <typeparamref name="T"/>.
        /// It retrieves the specified page of results, with the number of results per page determined by the <paramref name="perPage"/> parameter.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token to control the execution context of the query.
        /// </remarks>
        public static async Task<PaginationResult<T>> PageAsync<T>(
            this Query query,
            int page,
            int perPage = 25,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            if (UseDapper)
            {
                return await query.PaginateAsync<T>(page, perPage, transaction, timeout, token);
            }

            PaginationResult<object> result = await query.PaginateAsync<object>(
                page,
                perPage,
                transaction,
                timeout,
                token
            );

            var pageResult = new PaginationResult<T>
            {
                Query = result.Query,
                Count = result.Count,
                PerPage = result.PerPage,
                Page = result.Page,
                List = MapJson<IEnumerable<T>>(result.List)
            };

            return pageResult;
        }

        /// <summary>
        /// Processes a query in chunks, invoking a function for each chunk of elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The query to process.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="func">The function to invoke for each chunk of elements. The function should return true to continue processing, or false to stop processing.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <remarks>
        /// This method processes the results of the given query in chunks, invoking the specified function for each chunk.
        /// The function should return true to continue processing, or false to stop processing.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the query.
        /// </remarks>
        public static void Chunk<T>(
            this Query query,
            int chunkSize,
            Func<IEnumerable<T>, int, bool> func,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (UseDapper)
            {
                QueryExtensions.Chunk(query, chunkSize, func, transaction, timeout);
            }

            throw new NotSupportedException(
                "Chunking is currently only supported using Dapper. If you require chunking capabilities, please enable Dapper integration by setting the 'UseDapper' property to 'true'."
            );
        }

        /// <summary>
        /// Asynchronously processes a query in chunks, invoking a function for each chunk of elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the query.</typeparam>
        /// <param name="query">The query to process.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="func">The function to invoke for each chunk of elements. The function should return true to continue processing, or false to stop processing.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously processes the results of the given query in chunks, invoking the specified function for each chunk.
        /// The function should return true to continue processing, or false to stop processing.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token to control the execution context of the query.
        /// </remarks>
        public static Task ChunkAsync<T>(
            this Query query,
            int chunkSize,
            Func<IEnumerable<T>, int, bool> func,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            if (UseDapper)
            {
                return QueryExtensions.ChunkAsync<T>(
                    query,
                    chunkSize,
                    func,
                    transaction,
                    timeout,
                    token
                );
            }

            throw new NotSupportedException(
                "Chunking is currently only supported using Dapper. If you require chunking capabilities, please enable Dapper integration by setting the 'UseDapper' property to 'true'."
            );
        }

        /// <summary>
        /// Processes a query in chunks, invoking the specified action for each chunk of items.
        /// </summary>
        /// <typeparam name="T">The type of items in the query.</typeparam>
        /// <param name="query">The query to process.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="action">The action to invoke for each chunk of items.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <remarks>
        /// This method processes the results of the given query in chunks, invoking the specified action for each chunk of items.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the query.
        /// </remarks>
        public static void Chunk<T>(
            this Query query,
            int chunkSize,
            Action<IEnumerable<T>, int> action,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (UseDapper)
            {
                QueryExtensions.Chunk(query, chunkSize, action, transaction, timeout);
            }

            throw new NotSupportedException(
                "Chunking is currently only supported using Dapper. If you require chunking capabilities, please enable Dapper integration by setting the 'UseDapper' property to 'true'."
            );
        }

        /// <summary>
        /// Asynchronously processes a query in chunks, invoking the specified action for each chunk of items.
        /// </summary>
        /// <typeparam name="T">The type of items in the query.</typeparam>
        /// <param name="query">The query to process.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <param name="action">The action to invoke for each chunk of items.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation (optional).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method asynchronously processes the results of the given query in chunks, invoking the specified action for each chunk of items.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token to control the execution context of the query.
        /// </remarks>
        public static Task ChunkAsync<T>(
            this Query query,
            int chunkSize,
            Action<IEnumerable<T>, int> action,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            if (UseDapper)
            {
                return QueryExtensions.ChunkAsync(
                    query,
                    chunkSize,
                    action,
                    transaction,
                    timeout,
                    token
                );
            }

            throw new NotSupportedException(
                "Chunking is currently only supported using Dapper. If you require chunking capabilities, please enable Dapper integration by setting the 'UseDapper' property to 'true'."
            );
        }

        /// <summary>
        /// Maps a Query object to a Row(IDictionary<string, object>) object.
        /// </summary>
        /// <param name="query">The Query object to map.</param>
        /// <param name="transaction">The database transaction (optional).</param>
        /// <param name="timeout">The command timeout (optional).</param>
        /// <returns>The mapped Row(IDictionary<string, object>) object.</returns>
        /// <remarks>
        /// This method maps the first result of the given query to a Row(IDictionary<string, object>) object.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the query.
        /// </remarks>
        public static IDictionary<string, object> First(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (UseDapper)
            {
                return First<object>(query, transaction, timeout) as IDictionary<string, object>;
            }

            return First<IDictionary<string, object>>(query, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously maps a Query object to a Row(IDictionary<string, object>) object.
        /// </summary>
        /// <param name="query">The Query object to map.</param>
        /// <param name="transaction">The database transaction (optional).</param>
        /// <param name="timeout">The command timeout (optional).</param>
        /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the mapped Row(IDictionary<string, object>) object.</returns>
        /// <remarks>
        /// This method asynchronously maps the first result of the given query to a Row(IDictionary<string, object>) object.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token to control the execution context of the query.
        /// </remarks>
        public static async Task<IDictionary<string, object>> FirstAsync(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            if (UseDapper)
            {
                return await FirstAsync<object>(query, transaction, timeout, token)
                    as IDictionary<string, object>;
            }

            return await FirstAsync<IDictionary<string, object>>(
                query,
                transaction,
                timeout,
                token
            );
        }

        /// <summary>
        /// Maps the results of a query to a collection of rows(IDictionary<string, object>).
        /// </summary>
        /// <param name="query">The query to map.</param>
        /// <param name="transaction">The database transaction (optional).</param>
        /// <param name="timeout">The command timeout (optional).</param>
        /// <returns>A collection of rows.</returns>
        /// <remarks>
        /// This method maps all the results of the given query to a collection of rows.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the query.
        /// </remarks>
        public static IEnumerable<IDictionary<string, object>> All(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (UseDapper)
            {
                IEnumerable<object> result = All<object>(query, transaction, timeout);
                return Enumerable.Cast<IDictionary<string, object>>(result);
            }

            return All<IDictionary<string, object>>(query, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously maps the results of a query to a collection of rows(IDictionary<string, object>).
        /// </summary>
        /// <param name="query">The query to map.</param>
        /// <param name="transaction">The database transaction (optional).</param>
        /// <param name="timeout">The command timeout (optional).</param>
        /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains a collection of rows.</returns>
        /// <remarks>
        /// This method asynchronously maps all the results of the given query to a collection of rows.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token to control the execution context of the query.
        /// </remarks>
        public static async Task<IEnumerable<IDictionary<string, object>>> AllAsync(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            if (UseDapper)
            {
                IEnumerable<object> result = await AllAsync<object>(
                    query,
                    transaction,
                    timeout,
                    token
                );

                return Enumerable.Cast<IDictionary<string, object>>(result);
            }

            return await AllAsync<IDictionary<string, object>>(query, transaction, timeout, token);
        }

        /// <summary>
        /// Determines whether the query returns any results.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <returns>True if the query returns any results; otherwise, false.</returns>
        /// <remarks>
        /// This method executes the given query and checks if any results are returned.
        /// The method optionally accepts a database transaction and a command timeout value, which can be used to control the execution context of the query.
        /// </remarks>
        public static bool Exists(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Exists(query, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously determines whether the query returns any results.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the query returns any results; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously executes the given query and checks if any results are returned.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token, which can be used to control the execution context of the query.
        /// </remarks>
        public static Task<bool> ExistsAsync(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.ExistsAsync(query, transaction, timeout, cancellationToken);
        }

        /// <summary>
        /// Determines whether the query does not return any results.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <returns>True if the query does not return any results; otherwise, false.</returns>
        /// <remarks>
        /// This method executes the given query and checks if no results are returned.
        /// The method optionally accepts a database transaction and a command timeout value, which can be used to control the execution context of the query.
        /// </remarks>
        public static bool NotExist(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.NotExist(query, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously determines whether the query does not return any results.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="transaction">The database transaction to use for the command (optional).</param>
        /// <param name="timeout">The command timeout in seconds (optional). If not specified, the default timeout is used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if the query does not return any results; otherwise, false.</returns>
        /// <remarks>
        /// This method asynchronously executes the given query and checks if no results are returned.
        /// The method optionally accepts a database transaction, a command timeout value, and a cancellation token, which can be used to control the execution context of the query.
        /// </remarks>
        public static Task<bool> NotExistAsync(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.NotExistAsync(query, transaction, timeout, cancellationToken);
        }

        /// <summary>
        /// Inserts data into the specified database table using the provided values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="values">A collection of key-value pairs where the key represents the column name and the value represents the corresponding data to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method performs an insert operation into the specified database table using the provided values.
        /// It accepts a collection of key-value pairs, where each key represents a column name and its corresponding value.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static int Insert(
            this Query query,
            IEnumerable<KeyValuePair<string, object>> values,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Insert(query, values, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously inserts data into the specified database table using the provided values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="values">A collection of key-value pairs where the key represents the column name and the value represents the corresponding data to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method asynchronously performs an insert operation into the specified database table using the provided values.
        /// It accepts a collection of key-value pairs, where each key represents a column name and its corresponding value.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static Task<int> InsertAsync(
            this Query query,
            IEnumerable<KeyValuePair<string, object>> values,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.InsertAsync(
                query,
                values,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Inserts data into the specified database table using the provided column names and corresponding values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="columns">The names of the columns into which data will be inserted.</param>
        /// <param name="valuesCollection">A collection of collections representing multiple sets of values to be inserted, where each inner collection corresponds to a set of values for a single row.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method performs an insert operation into the specified database table using the provided column names and corresponding values.
        /// It accepts a collection of column names and a collection of collections, where each inner collection represents a set of values for a single row.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static int Insert(
            this Query query,
            IEnumerable<string> columns,
            IEnumerable<IEnumerable<object>> valuesCollection,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Insert(query, columns, valuesCollection, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously inserts data into the specified database table using the provided column names and corresponding values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="columns">The names of the columns into which data will be inserted.</param>
        /// <param name="valuesCollection">A collection of collections representing multiple sets of values to be inserted, where each inner collection corresponds to a set of values for a single row.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method asynchronously performs an insert operation into the specified database table using the provided column names and corresponding values.
        /// It accepts a collection of column names and a collection of collections, where each inner collection represents a set of values for a single row.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static Task<int> InsertAsync(
            this Query query,
            IEnumerable<string> columns,
            IEnumerable<IEnumerable<object>> valuesCollection,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.InsertAsync(
                query,
                columns,
                valuesCollection,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Inserts data into the specified database table from the result set of the provided Query object.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="columns">The names of the columns into which data will be inserted.</param>
        /// <param name="fromQuery">The Query object representing the source of data to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method performs an insert operation into the specified database table from the result set of the provided Query object.
        /// It accepts a collection of column names and a Query object representing the source of data to be inserted.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static int Insert(
            this Query query,
            IEnumerable<string> columns,
            Query fromQuery,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Insert(query, columns, fromQuery, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously inserts data into the specified database table from the result set of the provided Query object.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="columns">The names of the columns into which data will be inserted.</param>
        /// <param name="fromQuery">The Query object representing the source of data to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method asynchronously performs an insert operation into the specified database table from the result set of the provided Query object.
        /// It accepts a collection of column names and a Query object representing the source of data to be inserted.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static Task<int> InsertAsync(
            this Query query,
            IEnumerable<string> columns,
            Query fromQuery,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.InsertAsync(
                query,
                columns,
                fromQuery,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Inserts data into the specified database table using the provided object's properties as column names and values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">The object whose properties will be used as column names and corresponding values to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method performs an insert operation into the specified database table using the provided object's properties as column names and corresponding values.
        /// It accepts an object whose properties will be used as column names and values to be inserted.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static int Insert(
            this Query query,
            object data,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Insert(query, data, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously inserts data into the specified database table using the provided object's properties as column names and values.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">The object whose properties will be used as column names and corresponding values to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the insert operation.</returns>
        /// <remarks>
        /// This method asynchronously performs an insert operation into the specified database table using the provided object's properties as column names and corresponding values.
        /// It accepts an object whose properties will be used as column names and values to be inserted.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static Task<int> InsertAsync(
            this Query query,
            object data,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.InsertAsync(
                query,
                data,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Inserts data into the specified database table using the provided object's properties as column names and values, and returns the generated primary key value.
        /// </summary>
        /// <typeparam name="T">The type of the primary key value to be returned.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">The object whose properties will be used as column names and corresponding values to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The generated primary key value after the insert operation.</returns>
        /// <remarks>
        /// This method inserts data into the specified database table using the provided object's properties as column names and values.
        /// It returns the generated primary key value of type <typeparamref name="T"/> after the insert operation.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static T InsertGetId<T>(
            this Query query,
            object data,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (!UseDapper)
            {
                throw new Exception(
                    "UseDapper must be set to true in order to use InsertGetIdAsync!"
                );
            }

            return QueryExtensions.InsertGetId<T>(query, data, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously inserts data into the specified database table using the provided object's properties as column names and values, and returns the generated primary key value.
        /// </summary>
        /// <typeparam name="T">The type of the primary key value to be returned.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">The object whose properties will be used as column names and corresponding values to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the generated primary key value after the insert operation.</returns>
        /// <remarks>
        /// This method asynchronously inserts data into the specified database table using the provided object's properties as column names and values.
        /// It returns a Task representing the asynchronous operation, where the Task result contains the generated primary key value of type <typeparamref name="T"/>.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the insert operation.
        /// </remarks>
        public static Task<T> InsertGetIdAsync<T>(
            this Query query,
            object data,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            if (!UseDapper)
            {
                throw new Exception(
                    "UseDapper must be set to true in order to use InsertGetIdAsync!"
                );
            }

            return QueryExtensions.InsertGetIdAsync<T>(
                query,
                data,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Inserts data into the specified database table using the provided key-value pairs and returns the generated primary key value.
        /// </summary>
        /// <typeparam name="T">The type of the primary key value to be returned.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">A collection of key-value pairs where the key represents the column name and the value represents the corresponding data to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The generated primary key value after the insert operation.</returns>
        /// <remarks>
        /// This method inserts data into the specified database table using the provided key-value pairs.
        /// It returns the generated primary key value of type <typeparamref name="T"/> after the insert operation.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the insert operation.
        /// </remarks>
        public static T InsertGetId<T>(
            this Query query,
            IEnumerable<KeyValuePair<string, object>> data,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            if (!UseDapper)
            {
                throw new Exception(
                    "UseDapper must be set to true in order to use InsertGetIdAsync!"
                );
            }

            return QueryExtensions.InsertGetId<T>(query, data, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously inserts data into the specified database table using the provided key-value pairs and returns the generated primary key value.
        /// </summary>
        /// <typeparam name="T">The type of the primary key value to be returned.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">A collection of key-value pairs where the key represents the column name and the value represents the corresponding data to be inserted.</param>
        /// <param name="transaction">Optional. The database transaction within which the insert operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the generated primary key value after the insert operation.</returns>
        /// <remarks>
        /// This method asynchronously inserts data into the specified database table using the provided key-value pairs.
        /// It returns a Task representing the asynchronous operation, where the Task result contains the generated primary key value of type <typeparamref name="T"/>.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the insert operation.
        /// </remarks>
        public static Task<T> InsertGetIdAsync<T>(
            this Query query,
            IEnumerable<KeyValuePair<string, object>> data,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            if (!UseDapper)
            {
                throw new Exception(
                    "UseDapper must be set to true in order to use InsertGetIdAsync!"
                );
            }

            return QueryExtensions.InsertGetIdAsync<T>(
                query,
                data,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Updates data in the specified database table based on the provided key-value pairs.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="values">A collection of key-value pairs where the key represents the column name and the value represents the new data to be updated.</param>
        /// <param name="transaction">Optional. The database transaction within which the update operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the update operation.</returns>
        /// <remarks>
        /// This method updates data in the specified database table based on the provided key-value pairs.
        /// It accepts a collection of key-value pairs, where each key represents a column name and its corresponding new value.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the update operation.
        /// </remarks>
        public static int Update(
            this Query query,
            IEnumerable<KeyValuePair<string, object>> values,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Update(query, values, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously updates data in the specified database table based on the provided key-value pairs.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="values">A collection of key-value pairs where the key represents the column name and the value represents the new data to be updated.</param>
        /// <param name="transaction">Optional. The database transaction within which the update operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the update operation.</returns>
        /// <remarks>
        /// This method asynchronously updates data in the specified database table based on the provided key-value pairs.
        /// It accepts a collection of key-value pairs, where each key represents a column name and its corresponding new value.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the update operation.
        /// </remarks>
        public static Task<int> UpdateAsync(
            this Query query,
            IEnumerable<KeyValuePair<string, object>> values,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.UpdateAsync(
                query,
                values,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Updates data in the specified database table based on the properties of the provided object.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">The object whose properties will be used to update corresponding columns in the database table.</param>
        /// <param name="transaction">Optional. The database transaction within which the update operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the update operation.</returns>
        /// <remarks>
        /// This method updates data in the specified database table based on the properties of the provided object.
        /// It accepts an object whose properties will be used to update corresponding columns in the database table.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the update operation.
        /// </remarks>
        public static int Update(
            this Query query,
            object data,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Update(query, data, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously updates data in the specified database table based on the properties of the provided object.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="data">The object whose properties will be used to update corresponding columns in the database table.</param>
        /// <param name="transaction">Optional. The database transaction within which the update operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the update operation.</returns>
        /// <remarks>
        /// This method asynchronously updates data in the specified database table based on the properties of the provided object.
        /// It accepts an object whose properties will be used to update corresponding columns in the database table.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the update operation.
        /// </remarks>
        public static Task<int> UpdateAsync(
            this Query query,
            object data,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.UpdateAsync(
                query,
                data,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Increments the specified numeric column in the database table by the provided value.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column to be incremented.</param>
        /// <param name="value">Optional. The value by which the column should be incremented. Default is 1.</param>
        /// <param name="transaction">Optional. The database transaction within which the increment operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the increment operation.</returns>
        /// <remarks>
        /// This method increments the specified numeric column in the database table by the provided value.
        /// It accepts the name of the column to be incremented and an optional value to increment by.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the increment operation.
        /// </remarks>
        public static int Increment(
            this Query query,
            string column,
            int value = 1,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Increment(query, column, value, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously increments the specified numeric column in the database table by the provided value.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column to be incremented.</param>
        /// <param name="value">Optional. The value by which the column should be incremented. Default is 1.</param>
        /// <param name="transaction">Optional. The database transaction within which the increment operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the increment operation.</returns>
        /// <remarks>
        /// This method asynchronously increments the specified numeric column in the database table by the provided value.
        /// It accepts the name of the column to be incremented and an optional value to increment by.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the increment operation.
        /// </remarks>
        public static Task<int> IncrementAsync(
            this Query query,
            string column,
            int value = 1,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.IncrementAsync(
                query,
                column,
                value,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Decrements the specified numeric column in the database table by the provided value.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column to be decremented.</param>
        /// <param name="value">Optional. The value by which the column should be decremented. Default is 1.</param>
        /// <param name="transaction">Optional. The database transaction within which the decrement operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the decrement operation.</returns>
        /// <remarks>
        /// This method decrements the specified numeric column in the database table by the provided value.
        /// It accepts the name of the column to be decremented and an optional value to decrement by.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the decrement operation.
        /// </remarks>
        public static int Decrement(
            this Query query,
            string column,
            int value = 1,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Decrement(query, column, value, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously decrements the specified numeric column in the database table by the provided value.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column to be decremented.</param>
        /// <param name="value">Optional. The value by which the column should be decremented. Default is 1.</param>
        /// <param name="transaction">Optional. The database transaction within which the decrement operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the decrement operation.</returns>
        /// <remarks>
        /// This method asynchronously decrements the specified numeric column in the database table by the provided value.
        /// It accepts the name of the column to be decremented and an optional value to decrement by.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the decrement operation.
        /// </remarks>
        public static Task<int> DecrementAsync(
            this Query query,
            string column,
            int value = 1,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.DecrementAsync(
                query,
                column,
                value,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Deletes all rows from the specified database table.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="transaction">Optional. The database transaction within which the delete operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The number of rows affected by the delete operation.</returns>
        /// <remarks>
        /// This method deletes all rows from the specified database table.
        /// It optionally accepts a database transaction and a command timeout value to control the execution context of the delete operation.
        /// </remarks>
        public static int Delete(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Delete(query, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously deletes all rows from the specified database table.
        /// </summary>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="transaction">Optional. The database transaction within which the delete operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the number of rows affected by the delete operation.</returns>
        /// <remarks>
        /// This method asynchronously deletes all rows from the specified database table.
        /// It optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the delete operation.
        /// </remarks>
        public static Task<int> DeleteAsync(
            this Query query,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.DeleteAsync(query, transaction, timeout, cancellationToken);
        }

        /// <summary>
        /// Performs an aggregate operation (e.g., SUM, AVG, COUNT) on the specified columns in the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the aggregate operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="aggregateOperation">The aggregate operation to perform (e.g., "SUM", "AVG", "COUNT").</param>
        /// <param name="columns">The names of the columns to be included in the aggregate operation.</param>
        /// <param name="transaction">Optional. The database transaction within which the aggregate operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The result of the aggregate operation.</returns>
        /// <remarks>
        /// This method performs an aggregate operation (e.g., SUM, AVG, COUNT) on the specified columns in the database table.
        /// It accepts the aggregate operation to perform and the names of the columns to be included in the operation.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the aggregate operation.
        /// </remarks>
        public static T Aggregate<T>(
            this Query query,
            string aggregateOperation,
            string[] columns,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Aggregate<T>(
                query,
                aggregateOperation,
                columns,
                transaction,
                timeout
            );
        }

        /// <summary>
        /// Asynchronously performs an aggregate operation (e.g., SUM, AVG, COUNT) on the specified columns in the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the aggregate operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="aggregateOperation">The aggregate operation to perform (e.g., "SUM", "AVG", "COUNT").</param>
        /// <param name="columns">The names of the columns to be included in the aggregate operation.</param>
        /// <param name="transaction">Optional. The database transaction within which the aggregate operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the result of the aggregate operation.</returns>
        /// <remarks>
        /// This method asynchronously performs an aggregate operation (e.g., SUM, AVG, COUNT) on the specified columns in the database table.
        /// It accepts the aggregate operation to perform and the names of the columns to be included in the operation.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the aggregate operation.
        /// </remarks>
        public static Task<T> AggregateAsync<T>(
            this Query query,
            string aggregateOperation,
            string[] columns,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.AggregateAsync<T>(
                query,
                aggregateOperation,
                columns,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Counts the total number of rows in the specified database table, optionally based on the specified columns.
        /// </summary>
        /// <typeparam name="T">The type of the result of the count operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="columns">Optional. The names of the columns to be included in the count operation. If not specified, all rows are counted.</param>
        /// <param name="transaction">Optional. The database transaction within which the count operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The total number of rows in the specified database table.</returns>
        /// <remarks>
        /// This method counts the total number of rows in the specified database table, optionally based on the specified columns.
        /// It accepts optional column names to be included in the count operation.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the count operation.
        /// </remarks>
        public static T Count<T>(
            this Query query,
            string[] columns = null,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Count<T>(query, columns, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously counts the total number of rows in the specified database table, optionally based on the specified columns.
        /// </summary>
        /// <typeparam name="T">The type of the result of the count operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="columns">Optional. The names of the columns to be included in the count operation. If not specified, all rows are counted.</param>
        /// <param name="transaction">Optional. The database transaction within which the count operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the total number of rows in the specified database table.</returns>
        /// <remarks>
        /// This method asynchronously counts the total number of rows in the specified database table, optionally based on the specified columns.
        /// It accepts optional column names to be included in the count operation.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the count operation.
        /// </remarks>
        public static Task<T> CountAsync<T>(
            this Query query,
            string[] columns = null,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.CountAsync<T>(
                query,
                columns,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Calculates the average value of the specified numeric column in the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the average operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the numeric column for which the average should be calculated.</param>
        /// <param name="transaction">Optional. The database transaction within which the average operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The average value of the specified numeric column in the database table.</returns>
        /// <remarks>
        /// This method calculates the average value of the specified numeric column in the database table.
        /// It accepts the name of the numeric column for which the average should be calculated.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the average operation.
        /// </remarks>
        public static T Average<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Average<T>(query, column, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously calculates the average value of the specified numeric column in the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the average operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the numeric column for which the average should be calculated.</param>
        /// <param name="transaction">Optional. The database transaction within which the average operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the average value of the specified numeric column in the database table.</returns>
        /// <remarks>
        /// This method asynchronously calculates the average value of the specified numeric column in the database table.
        /// It accepts the name of the numeric column for which the average should be calculated.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the average operation.
        /// </remarks>
        public static Task<T> AverageAsync<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.AverageAsync<T>(
                query,
                column,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Calculates the sum of values in the specified numeric column in the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the sum operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the numeric column for which the sum should be calculated.</param>
        /// <param name="transaction">Optional. The database transaction within which the sum operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The sum of values in the specified numeric column in the database table.</returns>
        /// <remarks>
        /// This method calculates the sum of values in the specified numeric column in the database table.
        /// It accepts the name of the numeric column for which the sum should be calculated.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the sum operation.
        /// </remarks>
        public static T Sum<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Sum<T>(query, column, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously calculates the sum of values in the specified numeric column in the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the sum operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the numeric column for which the sum should be calculated.</param>
        /// <param name="transaction">Optional. The database transaction within which the sum operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the sum of values in the specified numeric column in the database table.</returns>
        /// <remarks>
        /// This method asynchronously calculates the sum of values in the specified numeric column in the database table.
        /// It accepts the name of the numeric column for which the sum should be calculated.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the sum operation.
        /// </remarks>
        public static Task<T> SumAsync<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.SumAsync<T>(
                query,
                column,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Finds the minimum value in the specified column of the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the minimum operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column for which the minimum value should be found.</param>
        /// <param name="transaction">Optional. The database transaction within which the minimum operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The minimum value in the specified column of the database table.</returns>
        /// <remarks>
        /// This method finds the minimum value in the specified column of the database table.
        /// It accepts the name of the column for which the minimum value should be found.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the minimum operation.
        /// </remarks>
        public static T Min<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Min<T>(query, column, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously finds the minimum value in the specified column of the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the minimum operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column for which the minimum value should be found.</param>
        /// <param name="transaction">Optional. The database transaction within which the minimum operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the minimum value in the specified column of the database table.</returns>
        /// <remarks>
        /// This method asynchronously finds the minimum value in the specified column of the database table.
        /// It accepts the name of the column for which the minimum value should be found.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the minimum operation.
        /// </remarks>
        public static Task<T> MinAsync<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.MinAsync<T>(
                query,
                column,
                transaction,
                timeout,
                cancellationToken
            );
        }

        /// <summary>
        /// Finds the maximum value in the specified column of the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the maximum operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column for which the maximum value should be found.</param>
        /// <param name="transaction">Optional. The database transaction within which the maximum operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <returns>The maximum value in the specified column of the database table.</returns>
        /// <remarks>
        /// This method finds the maximum value in the specified column of the database table.
        /// It accepts the name of the column for which the maximum value should be found.
        /// The method optionally accepts a database transaction and a command timeout value to control the execution context of the maximum operation.
        /// </remarks>
        public static T Max<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            return QueryExtensions.Max<T>(query, column, transaction, timeout);
        }

        /// <summary>
        /// Asynchronously finds the maximum value in the specified column of the database table.
        /// </summary>
        /// <typeparam name="T">The type of the result of the maximum operation.</typeparam>
        /// <param name="query">The Query object representing the database query.</param>
        /// <param name="column">The name of the column for which the maximum value should be found.</param>
        /// <param name="transaction">Optional. The database transaction within which the maximum operation should be performed.</param>
        /// <param name="timeout">Optional. The timeout period for the database operation, in seconds.</param>
        /// <param name="cancellationToken">Optional. A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A Task representing the asynchronous operation. The Task result contains the maximum value in the specified column of the database table.</returns>
        /// <remarks>
        /// This method asynchronously finds the maximum value in the specified column of the database table.
        /// It accepts the name of the column for which the maximum value should be found.
        /// The method optionally accepts a database transaction, a command timeout value, and a CancellationToken to control the execution context of the maximum operation.
        /// </remarks>
        public static Task<T> MaxAsync<T>(
            this Query query,
            string column,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken cancellationToken = default
        )
        {
            return QueryExtensions.MaxAsync<T>(
                query,
                column,
                transaction,
                timeout,
                cancellationToken
            );
        }
    }
}
