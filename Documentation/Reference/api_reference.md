## Network Manager

The `NetworkManager` class is the core component of the Omni Networking library for Unity. It provides a framework for managing network operations, including client-server communication, synchronization, and data transmission.

The `NetworkManager` class is responsible for handling the initialization, configuration, and management of the network environment. It acts as a central hub for network-related operations, such as connecting to a server, handling client-server communication, and managing network events.

---

### Methods

#### ClearCaches

Clears all global caches, removing all entries from both append and overwrite cache collections.

- **Signature**: `public static void ClearCaches()`

Description

The `ClearCaches` method removes all entries in the global append and overwrite cache collections. This method is useful for freeing up resources by clearing all cached data across the network, ensuring a clean state.

???+ example
    ```csharp
    // Example of clearing all global caches
    NetworkManager.Server.ClearCaches();
    Debug.Log("All global caches have been cleared.");
    ```

---

#### DestroyAllCaches

Destroys all caches associated with the specified `NetworkPeer`, removing both global and overwrite caches marked for auto-destruction.

- **Signature**: `public static void DestroyAllCaches(NetworkPeer peer)`

Description

The `DestroyAllCaches` method removes all cache entries associated with the given `NetworkPeer` that are marked for auto-destruction (`AutoDestroyCache`). This includes caches from both global append and global overwrite collections. If any cache entry fails to be removed, an error is logged.

Parameters

| Parameter | Type         | Description                                            |
|-----------|--------------|--------------------------------------------------------|
| `peer`    | `NetworkPeer`| The network peer whose associated caches are to be destroyed. |

???+ example
    ```csharp
    // Example of destroying all caches for a specific peer
    NetworkPeer clientPeer = GetClientPeer(); // Hypothetical method to get a client peer
    NetworkManager.Server.DestroyAllCaches(clientPeer);
    Debug.Log($"Destroyed all auto-destroy caches for peer {clientPeer.Id}");
    ```

---

#### DeleteCache

Deletes a cache entry based on the provided `DataCache` and optional `groupId`, or a `DataCache` associated with a specific `NetworkPeer`.

- **Signature**:
  - `public static void DeleteCache(DataCache dataCache, int groupId = 0)`
  - `public static void DeleteCache(DataCache dataCache, NetworkPeer peer, int groupId = 0)`

Description

The `DeleteCache` methods remove cache entries based on the provided `DataCache` details. Depending on the cache's `Mode`, this can involve removing global, group-specific, or peer-specific caches, with additional conditions to check if the `DataCache.Id` and `DataCache.Mode` are set correctly. These methods ensure the removal of specified caches, either by targeting a particular group (`groupId`) or by associating it with a specific peer (`NetworkPeer`).

Parameters

| Parameter   | Type         | Description                                                                                   |
|-------------|--------------|-----------------------------------------------------------------------------------------------|
| `dataCache` | `DataCache`  | The data cache to delete, which includes mode and ID for the targeted cache entry.            |
| `groupId`   | `int`        | The ID of the group to which the cache belongs (optional, default is `0`).                    |
| `peer`      | `NetworkPeer`| The peer associated with the cache, for peer-specific cache deletion.                         |

Overloads

=== "DeleteCache (DataCache dataCache, int groupId = 0)"
    Deletes a cache entry in a specific group or globally based on the provided `DataCache` and `groupId`.

    | Parameter   | Type         | Description                                           |
    |-------------|--------------|-------------------------------------------------------|
    | `dataCache` | `DataCache`  | The data cache to delete.                             |
    | `groupId`   | `int`        | The ID of the group to which the cache belongs.       |

    === "Example"
    ???+ example
        ```csharp
        // Example of deleting a cache entry in a specific group
        DataCache cacheToDelete = new DataCache(CachePresets.ServerNew);
        NetworkManager.Server.DeleteCache(cacheToDelete, groupId: 5);
        ```

=== "DeleteCache (DataCache dataCache, NetworkPeer peer, int groupId = 0)"
    Deletes a cache entry for a specific peer or group based on the provided `DataCache`, `NetworkPeer`, and optional `groupId`.

    | Parameter   | Type         | Description                                           |
    |-------------|--------------|-------------------------------------------------------|
    | `dataCache` | `DataCache`  | The data cache to delete.                             |
    | `peer`      | `NetworkPeer`| The peer associated with the cache.                   |
    | `groupId`   | `int`        | The ID of the group to which the cache belongs.       |

    === "Example"
    ???+ example
        ```csharp
        // Example of deleting a peer-specific cache entry
        DataCache cacheToDelete = new DataCache(CachePresets.ServerNew);
        NetworkManager.Server.DeleteCache(cacheToDelete, peer, groupId: 3);
        ```

---

#### Invoke(Server) by Instance

Invokes a Remote Procedure Call (RPC) on clients, targeting a specific network identity and script instance by their IDs, with customizable options for target, delivery mode, grouping, and sequencing.

- **Signature**:
  - `public static void Invoke(byte msgId, NetworkPeer peer, int identityId, byte instanceId, SyncOptions options)`
  - `public static void Invoke(byte msgId, NetworkPeer peer, int identityId, byte instanceId, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)`

Description

The `Invoke` method allows the server to execute a specific RPC on clients by targeting a `NetworkIdentity` and a specific script instance associated with that identity. The `identityId` identifies the object, while `instanceId` specifies the exact script instance on that object that should receive the RPC. This function provides options for message delivery such as buffer, target recipients, delivery reliability, grouping, and sequencing.

Parameters

| Parameter       | Type              | Description                                                                                   |
|-----------------|-------------------|-----------------------------------------------------------------------------------------------|
| `msgId`         | `byte`            | The ID of the RPC message to invoke.                                                          |
| `peer`          | `NetworkPeer`     | The peer for whom the RPC is invoked.                                                         |
| `identityId`    | `int`             | The unique ID of the `NetworkIdentity` that will receive the RPC.                             |
| `instanceId`    | `byte`            | The ID of the script instance on the `NetworkIdentity` that will receive the RPC.             |
| `options`       | `SyncOptions`     | A set of options for synchronization (used in the first overload).                            |
| `buffer`        | `DataBuffer`      | Optional data buffer to send.                                                                 |
| `target`        | `Target`          | Specifies the target for the RPC, such as all clients or a specific peer (default is `All`).  |
| `deliveryMode`  | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered` (default), `Unreliable`, etc.            |
| `groupId`       | `int`             | ID for grouping related messages together (default is `0`).                                   |
| `dataCache`     | `DataCache`       | Cache option for the data being sent (default is `DataCache.None`).                           |
| `sequenceChannel` | `byte`          | Channel for message sequencing to manage order consistency across channels (default is `0`).  |

Overloads

=== "Invoke (byte msgId, NetworkPeer peer, int identityId, byte instanceId, SyncOptions options)"
    Invokes an RPC using `SyncOptions` to specify buffer, target, delivery mode, group ID, data cache, and sequence channel, targeting both a `NetworkIdentity` and script instance.

    | Parameter    | Type            | Description                                           |
    |--------------|-----------------|-------------------------------------------------------|
    | `msgId`      | `byte`          | The ID of the RPC message to invoke.                  |
    | `peer`       | `NetworkPeer`   | The peer for whom the RPC is invoked.                 |
    | `identityId` | `int`           | The ID of the `NetworkIdentity` to target.            |
    | `instanceId` | `byte`          | The ID of the script instance to target.              |
    | `options`    | `SyncOptions`   | Configuration options for synchronization.            |

    === "Example"
    ???+ example
        ```csharp
        // Example of invoking an RPC with SyncOptions targeting a specific identity and script instance
        SyncOptions syncOptions = new SyncOptions(myDataBuffer)
        {
            Target = Target.Self,
            DeliveryMode = DeliveryMode.Unreliable,
            GroupId = 0,
            DataCache = DataCache.None,
            SequenceChannel = 0
        };
        NetworkManager.Server.Invoke(1, clientPeer, identityId: 101, instanceId: 5, syncOptions);
        ```

=== "Invoke (byte msgId, NetworkPeer peer, int identityId, byte instanceId, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)"
    Invokes an RPC with detailed parameters for buffer, target, delivery mode, grouping, and sequencing, targeting both a `NetworkIdentity` and script instance.

    | Parameter         | Type              | Description                                                  |
    |-------------------|-------------------|--------------------------------------------------------------|
    | `msgId`           | `byte`            | The ID of the RPC message to invoke.                         |
    | `peer`            | `NetworkPeer`     | The peer for whom the RPC is invoked.                        |
    | `identityId`      | `int`             | The ID of the `NetworkIdentity` to target.                   |
    | `instanceId`      | `byte`            | The ID of the script instance to target.                     |
    | `buffer`          | `DataBuffer`      | Optional data buffer containing the message content.         |
    | `target`          | `Target`          | The target for the RPC, such as all clients or a specific peer. |
    | `deliveryMode`    | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered`.        |
    | `groupId`         | `int`             | ID for grouping related messages (default is `0`).           |
    | `dataCache`       | `DataCache`       | Specifies how the message data is cached (default is `None`).|
    | `sequenceChannel` | `byte`            | Channel for message sequencing (default is `0`).             |

    === "Example"
    ???+ example
        ```csharp
        // Example of invoking an RPC targeting a specific identity and script instance with detailed parameters(optional)
        NetworkManager.Server.Invoke(
            1,
            clientPeer,
            identityId: 101,
            instanceId: 5,
            buffer: myDataBuffer,
            target: Target.All,
            deliveryMode: DeliveryMode.Unreliable,
            groupId: 0,
            dataCache: DataCache.None,
            sequenceChannel: 0
        );
        ```

Remarks

- **Identity and Instance-Specific Invocation**: Targets a specific `NetworkIdentity` using `identityId` and a specific script instance using `instanceId`, ensuring precise correspondence with the intended script or object.
- **Flexible Targeting**: Supports targeting all clients, server-only, or specific peers, providing control over who receives the RPC.
- **Reliability Options**: Provides reliable, ordered delivery or lightweight, unordered options depending on the use case.
- **Sequencing and Grouping**: Allows message organization with group IDs and sequence channels, ensuring consistency across channels.
- **Usage**: Ideal for invoking RPCs on specific entities and script instances, particularly for commands or updates that are relevant to targeted objects or specific components.

!!! note
    This function is also available on the `client side`, but does not load the `peer` parameter.

---

#### Invoke(Server)

Invokes a Remote Procedure Call (RPC) on clients, targeting a specific network identity by its unique ID, with options for target, delivery mode, grouping, and sequencing.

- **Signature**:
  - `public static void Invoke(byte msgId, NetworkPeer peer, int identityId, SyncOptions options)`
  - `public static void Invoke(byte msgId, NetworkPeer peer, int identityId, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)`

Description

The `Invoke` method allows the server to execute a specific RPC on clients by using the unique `identityId` of a network identity. Unlike global RPCs, this method requires the identity ID to establish correspondence, ensuring that only scripts or objects tied to that identity will respond. This method offers flexibility for customizing message delivery with options such as buffer, target recipients, delivery reliability, grouping, and message sequencing.

Parameters

| Parameter       | Type              | Description                                                                                   |
|-----------------|-------------------|-----------------------------------------------------------------------------------------------|
| `msgId`         | `byte`            | The ID of the RPC message to invoke.                                                          |
| `peer`          | `NetworkPeer`     | The peer for whom the RPC is invoked.                                                         |
| `identityId`    | `int`             | The unique ID of the `NetworkIdentity` that will receive the RPC.                             |
| `options`       | `SyncOptions`     | A set of options for synchronization (used in the first overload).                            |
| `buffer`        | `DataBuffer`      | Optional data buffer to send.                                                                 |
| `target`        | `Target`          | Specifies the target for the RPC, such as all clients or a specific peer (default is `All`).  |
| `deliveryMode`  | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered` (default), `Unreliable`, etc.            |
| `groupId`       | `int`             | ID for grouping related messages together (default is `0`).                                   |
| `dataCache`     | `DataCache`       | Cache option for the data being sent (default is `DataCache.None`).                           |
| `sequenceChannel` | `byte`          | Channel for message sequencing to manage order consistency across channels (default is `0`).  |

Overloads

=== "Invoke (byte msgId, NetworkPeer peer, int identityId, SyncOptions options)"
    Invokes an RPC using a `SyncOptions` instance to specify buffer, target, delivery mode, group ID, data cache, and sequence channel.

    | Parameter    | Type            | Description                                           |
    |--------------|-----------------|-------------------------------------------------------|
    | `msgId`      | `byte`          | The ID of the RPC message to invoke.                  |
    | `peer`       | `NetworkPeer`   | The peer for whom the RPC is invoked.                 |
    | `identityId` | `int`           | The ID of the `NetworkIdentity` to target.            |
    | `options`    | `SyncOptions`   | Configuration options for synchronization.            |

    === "Example"
    ???+ example
        ```csharp
        // Example of invoking an RPC with SyncOptions targeting a specific identity
        SyncOptions syncOptions = new SyncOptions(myDataBuffer)
        {
            Target = Target.Self,
            DeliveryMode = DeliveryMode.Unreliable,
            GroupId = 0,
            DataCache = DataCache.None,
            SequenceChannel = 0
        };
        NetworkManager.Server.Invoke(1, clientPeer, identityId: 101, syncOptions);
        ```

=== "Invoke (byte msgId, NetworkPeer peer, int identityId, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)"
    Invokes an RPC with detailed parameters for buffer, target, delivery mode, grouping, and sequencing.

    | Parameter         | Type              | Description                                                  |
    |-------------------|-------------------|--------------------------------------------------------------|
    | `msgId`           | `byte`            | The ID of the RPC message to invoke.                         |
    | `peer`            | `NetworkPeer`     | The peer for whom the RPC is invoked.                        |
    | `identityId`      | `int`             | The ID of the `NetworkIdentity` to target.                   |
    | `buffer`          | `DataBuffer`      | Optional data buffer containing the message content.         |
    | `target`          | `Target`          | The target for the RPC, such as all clients or a specific peer. |
    | `deliveryMode`    | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered`.        |
    | `groupId`         | `int`             | ID for grouping related messages (default is `0`).           |
    | `dataCache`       | `DataCache`       | Specifies how the message data is cached (default is `None`).|
    | `sequenceChannel` | `byte`            | Channel for message sequencing (default is `0`).             |

    === "Example"
    ???+ example
        ```csharp
        // Example of invoking an RPC targeting a specific identity with detailed parameters(optional)
        NetworkManager.Server.Invoke(
            1,
            clientPeer,
            identityId: 101,
            buffer: myDataBuffer,
            target: Target.All,
            deliveryMode: DeliveryMode.Unreliable,
            groupId: 0,
            dataCache: DataCache.None,
            sequenceChannel: 0
        );
        ```

Remarks

- **Identity-Specific Invocation**: Targets a specific `NetworkIdentity` using `identityId`, ensuring the RPC is directed only to matching identities.
- **Flexible Targeting**: Supports targeting all clients, server-only, or specific peers, allowing control over who receives the RPC.
- **Reliability Options**: Provides reliable, ordered delivery or lightweight, unordered options depending on the use case.
- **Sequencing and Grouping**: Allows message organization with group IDs and sequence channels, ensuring consistency across channels.
- **Usage**: Ideal for invoking RPCs on specific entities across clients, especially for commands that are only relevant to targeted objects or scripts.

!!! note
    This function is also available on the `client side`, but does not load the `peer` parameter.

---

#### GlobalInvoke(Server)

Invokes a global Remote Procedure Call (RPC) on clients, independent of script instance or identity, using customizable options for target, delivery mode, grouping, and sequencing.

- **Signature**:
  - `public static void GlobalInvoke(byte msgId, NetworkPeer peer, SyncOptions options)`
  - `public static void GlobalInvoke(byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)`

Description

The `GlobalInvoke` method allows the server to invoke a global RPC on connected clients. Unlike instance-based RPCs, `GlobalInvoke` does not require a specific script instance or identity, allowing any client-side script with the matching RPC to be called directly. This is ideal for general-purpose commands and broadcasts. The method offers flexible parameters for controlling the target recipients, delivery reliability, grouping, and message sequencing.

Parameters

| Parameter       | Type              | Description                                                                                   |
|-----------------|-------------------|-----------------------------------------------------------------------------------------------|
| `msgId`         | `byte`            | The ID of the RPC message to invoke.                                                          |
| `peer`          | `NetworkPeer`     | The peer for whom the RPC is invoked.                                                         |
| `options`       | `SyncOptions`     | A set of options for synchronization (used in the first overload).                            |
| `buffer`        | `DataBuffer`      | Optional data buffer to send.                                                                 |
| `target`        | `Target`          | Specifies the target for the RPC, such as all clients or a specific peer (default is `All`).  |
| `deliveryMode`  | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered` (default), `Unreliable`, etc.            |
| `groupId`       | `int`             | ID for grouping related messages together (default is `0`).                                   |
| `dataCache`     | `DataCache`       | Cache option for the data being sent (default is `DataCache.None`).                           |
| `sequenceChannel` | `byte`          | Channel for message sequencing to manage order consistency across channels (default is `0`).  |

Overloads

=== "GlobalInvoke (byte msgId, NetworkPeer peer, SyncOptions options)"
    Invokes an RPC on clients using a `SyncOptions` instance to specify buffer, target, delivery mode, group ID, data cache, and sequence channel.

    | Parameter  | Type            | Description                                            |
    |------------|-----------------|--------------------------------------------------------|
    | `msgId`    | `byte`          | The ID of the RPC message to invoke.                   |
    | `peer`     | `NetworkPeer`   | The peer to whom the RPC is invoked.                   |
    | `options`  | `SyncOptions`   | Configuration options for synchronization.             |

    === "Example"
    ???+ example
        ```csharp
        // Example of invoking a global RPC with SyncOptions
        SyncOptions syncOptions = new SyncOptions(myDataBuffer)
        {
            Target = Target.All,
            DeliveryMode = DeliveryMode.Reliable,
            GroupId = 0,
            DataCache = DataCache.None,
            SequenceChannel = 0
        };
        NetworkManager.Server.GlobalInvoke(1, clientPeer, syncOptions);
        ```

=== "GlobalInvoke (byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)"
    Invokes an RPC on clients with detailed parameters for buffer, target, delivery mode, grouping, and sequencing.

    | Parameter         | Type              | Description                                                  |
    |-------------------|-------------------|--------------------------------------------------------------|
    | `msgId`           | `byte`            | The ID of the RPC message to invoke.                         |
    | `peer`            | `NetworkPeer`     | The peer to whom the RPC is invoked.                         |
    | `buffer`          | `DataBuffer`      | Optional data buffer containing the message content.         |
    | `target`          | `Target`          | The target for the RPC, such as all clients or a specific peer. |
    | `deliveryMode`    | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered`.        |
    | `groupId`         | `int`             | ID for grouping related messages (default is `0`).           |
    | `dataCache`       | `DataCache`       | Specifies how the message data is cached (default is `None`).|
    | `sequenceChannel` | `byte`            | Channel for message sequencing (default is `0`).             |

    === "Example"
    ???+ example
        ```csharp
        // Example of invoking a global RPC with detailed parameters(optional)
        NetworkManager.Server.GlobalInvoke(
            1,
            clientPeer,
            buffer: myDataBuffer,
            target: Target.Self,
            deliveryMode: DeliveryMode.Reliable,
            groupId: 0,
            dataCache: DataCache.None,
            sequenceChannel: 0
        );
        ```

Remarks

- **Global Invocation**: Unlike instance-specific RPCs, `GlobalInvoke` targets clients globally, without needing script identity or instance details.
- **Flexible Targeting**: Supports targeting all clients, server-only, or specific peers, allowing flexible control over who receives the RPC.
- **Reliability Options**: Provides reliable, ordered delivery or lightweight, unordered options depending on the use case.
- **Sequencing and Grouping**: Allows message organization with group IDs and sequence channels, ensuring consistency across channels.
- **Usage**: Ideal for broadcasting general-purpose commands or notifications across clients without binding to specific instances.

!!! note
    This function is also available on the `client side`, but does not load the `peer` parameter.

---

#### TryGetIdentity

Attempts to retrieve a `NetworkIdentity` instance by its unique identity ID.

- **Signature**: `public static bool TryGetIdentity(int identityId, out NetworkIdentity identity)`

Description

The `TryGetIdentity` method searches for a `NetworkIdentity` in the networked identities collection using a unique `identityId`. If the `NetworkIdentity` is found, it is returned via the `out` parameter; otherwise, `null` is returned. This method provides a safe and efficient way to check if an identity exists before attempting to interact with it.

Parameters

| Parameter     | Type             | Description                                                              |
|---------------|------------------|--------------------------------------------------------------------------|
| `identityId`  | `int`            | The unique ID of the `NetworkIdentity` to retrieve.                     |
| `identity`    | `NetworkIdentity`| The retrieved `NetworkIdentity` instance, or `null` if not found.       |

Returns

- **`bool`**: Returns `true` if the `NetworkIdentity` was found; otherwise, `false`.

???+ example
    ```csharp
    // Example of retrieving a NetworkIdentity by its unique ID on the server
    int identityId = 101;
    if (NetworkManager.Server.TryGetIdentity(identityId, out NetworkIdentity networkIdentity))
    {
        Debug.Log($"NetworkIdentity with ID {identityId} found: {networkIdentity.name}");
    }
    else
    {
        Debug.Log($"NetworkIdentity with ID {identityId} not found.");
    }
    
    // Example of retrieving a NetworkIdentity by its unique ID on the client
    if (NetworkManager.Client.TryGetIdentity(identityId, out NetworkIdentity networkIdentity))
    {
        Debug.Log($"NetworkIdentity with ID {identityId} found: {networkIdentity.name}");
    }
    else
    {
        Debug.Log($"NetworkIdentity with ID {identityId} not found.");
    }
    ```

---

#### AddPrefab

Adds a prefab to the NetworkManager's registration list if it hasn’t been registered already.

- **Signature**: `public static void AddPrefab(NetworkIdentity prefab)`

Parameters

| Parameter | Type             | Description                                           |
|-----------|------------------|-------------------------------------------------------|
| `prefab`  | `NetworkIdentity` | The prefab to add to the registration list.           |

Description

Registers a `NetworkIdentity` prefab for network spawning. This method checks if a prefab with the same name already exists in the list, and if so, it will not add it again. This ensures that each prefab is unique in the registration list.

???+ example
    ```csharp
       // Example of adding a prefab to the registration list
       NetworkManager.AddPrefab(myPrefabIdentity);
    ```

---

#### GetPrefab

Retrieves a prefab from the NetworkManager's registered list by either its name or index.

- **Signature**: 
  - `public static NetworkIdentity GetPrefab(string prefabName)`
  - `public static NetworkIdentity GetPrefab(int index)`

=== "GetPrefab (string prefabName)"
    
    Retrieves a prefab by its name.

    | Parameter     | Type     | Description                         |
    |---------------|----------|-------------------------------------|
    | `prefabName`  | `string` | The name of the prefab to retrieve. |

    - **Returns**: `NetworkIdentity` — The prefab with the specified name.
    - **Exceptions**: Throws an `Exception` if the prefab with the specified name is not found.

    === "Example"
    ???+ example
        ```csharp
           // Example of retrieving a prefab by name
           try
           {
               NetworkIdentity playerPrefab = NetworkManager.GetPrefab("Player");
               Debug.Log("Prefab retrieved successfully by name.");
           }
           catch (Exception ex)
           {
               Debug.LogError(ex.Message);
           }
        ```

=== "GetPrefab (int index)"
    
    Retrieves a prefab by its index in the registered list.

    | Parameter | Type     | Description                                           |
    |-----------|----------|-------------------------------------------------------|
    | `index`   | `int`    | The index of the prefab to retrieve in the list.      |

    - **Returns**: `NetworkIdentity` — The prefab at the specified index.
    - **Exceptions**: Throws an `IndexOutOfRangeException` if the index is out of bounds.

    === "Example"
    ???+ example
        ```csharp
           // Example of retrieving a prefab by index
           try
           {
               NetworkIdentity enemyPrefab = NetworkManager.GetPrefab(0);
               Debug.Log("Prefab retrieved successfully by index.");
           }
           catch (IndexOutOfRangeException ex)
           {
               Debug.LogError(ex.Message);
           }
        ```

Description

This method provides two overloads for retrieving a `NetworkIdentity` prefab from the NetworkManager’s registration list. You can either search by the name of the prefab or by its index within the list. If a matching prefab is found, it is returned; otherwise, an appropriate exception is thrown, ensuring that only registered prefabs are used for network spawning.

Remarks

- **By Name**: Throws an `Exception` if the prefab with the specified name is not found in the registration list.
- **By Index**: Throws an `IndexOutOfRangeException` if the index is out of bounds.
- Useful for scenarios where dynamic instantiation of networked objects is required, either by specific name or by predefined order in the list.

---

#### Connect

Establishes a connection to a specified server address and port. There are two overloads for this method, allowing you to specify a client listening port if needed.

- **Signature**:
  - `public static void Connect(string address, int port)`
  - `public static void Connect(string address, int port, int listenPort)`

=== "Connect (string address, int port)"

    Connects to the server using the specified `address` and `port`. The client will use the default listening port defined in `Manager.m_ClientListenPort`.

    | Parameter | Type     | Description                          |
    |-----------|----------|--------------------------------------|
    | `address` | `string` | The IP address of the server.        |
    | `port`    | `int`    | The port number on the server.       |

    - **Exceptions**: Throws an `Exception` if the client is already active, instructing to stop the client before reconnecting.

    === "Example"
    ???+ example
        ```csharp
        // Connect to a server with default client listening port
        NetworkManager.Connect("192.168.1.1", 7777);
        ```

=== "Connect (string address, int port, int listenPort)"

    Connects to the server using the specified `address` and `port`, with the client listening on the specified `listenPort`.

    | Parameter    | Type     | Description                                   |
    |--------------|----------|-----------------------------------------------|
    | `address`    | `string` | The IP address of the server.                 |
    | `port`       | `int`    | The port number on the server.                |
    | `listenPort` | `int`    | The port number on which the client listens.  |

    - **Exceptions**: Throws an `Exception` if the client is already active, instructing to stop the client before reconnecting.

    === "Example"
    ???+ example
        ```csharp
        // Connect to a server with a custom client listening port
        NetworkManager.Connect("192.168.1.1", 7777, 8888);
        ```

Description

The `Connect` method initiates a connection to the server at a given IP address and port. If the client is already active, an exception is thrown to prevent multiple connections. In the server build configuration (`UNITY_SERVER`), client connections are disabled.

Remarks

- **Server Build**: In a server build (`UNITY_SERVER`), client connections are not permitted and will log a message instead.
- **Exception Handling**: Ensure to call `StopClient()` before reconnecting if the client is already active to avoid exceptions.
- **Listening Port**: Use the overload with `listenPort` if a custom listening port is required for the client.

---

#### DisconnectPeer

Disconnects a specified peer from the server.

- **Signature**: `public static void DisconnectPeer(NetworkPeer peer)`

Parameters

| Parameter | Type         | Description                        |
|-----------|--------------|------------------------------------|
| `peer`    | `NetworkPeer` | The network peer to disconnect.   |

Description

The `DisconnectPeer` method removes the specified `NetworkPeer` from the server if the server is currently active. If the server has not been initialized, an exception is thrown to prompt server startup before disconnection.

???+ example
    ```csharp
       // Example of disconnecting a peer
       try
       {
           NetworkPeer somePeer = GetPeer(); // Assume this retrieves a valid NetworkPeer
           NetworkManager.DisconnectPeer(somePeer);
           Debug.Log("Peer disconnected successfully.");
       }
       catch (Exception ex)
       {
           Debug.LogError(ex.Message);
       }
    ```

---

#### Disconnect

Disconnects the local client from the server.

- **Signature**: `public static void Disconnect()`

Description

The `Disconnect` method terminates the connection between the local client and the server, if the client is currently active. If the client has not been initialized, an exception is thrown to prompt a connection attempt before disconnection.

???+ example
    ```csharp
       // Example of disconnecting the client from the server
       try
       {
           NetworkManager.Disconnect();
           Debug.Log("Client disconnected successfully.");
       }
       catch (Exception ex)
       {
           Debug.LogError(ex.Message);
       }
    ```

---

#### StopClient

Stops the local client and ends its connection to the server.

- **Signature**: `public static void StopClient()`

Description

The `StopClient` method halts the local client’s network operations, fully disconnecting it from the server if it is currently active. If the client has not been initialized, an exception is thrown to prompt a connection attempt before stopping.

???+ example
    ```csharp
       // Example of stopping the client
       try
       {
           NetworkManager.StopClient();
           Debug.Log("Client stopped successfully.");
       }
       catch (Exception ex)
       {
           Debug.LogError(ex.Message);
       }
    ```

---

#### FastWrite

Writes one or more primitive values to a `DataBuffer`, utilizing `stackalloc` to avoid allocations and ensure high performance. This method is available in multiple overloads, allowing for writing up to six primitive values in a single call.

- **Signature**:
  - `public static DataBuffer FastWrite<T1>(T1 t1) where T1 : unmanaged`
  - `public static DataBuffer FastWrite<T1, T2>(T1 t1, T2 t2) where T1 : unmanaged where T2 : unmanaged`
  - `public static DataBuffer FastWrite<T1, T2, T3>(T1 t1, T2 t2, T3 t3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged`
  - `public static DataBuffer FastWrite<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged`
  - `public static DataBuffer FastWrite<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged`
  - `public static DataBuffer FastWrite<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged`

Description

Each overload of `FastWrite` allows for writing up to six unmanaged primitive values to a `DataBuffer`. By using `stackalloc`, these methods avoid memory allocations, making them highly efficient for network messaging. The returned `DataBuffer` must be disposed or used within a `using` statement to ensure proper memory management.

Returns

- **`DataBuffer`**: The network message buffer containing the written values.

=== "FastWrite (T1 t1)"
    Writes a single unmanaged value to the buffer.

    | Parameter | Type           | Description                               |
    |-----------|----------------|-------------------------------------------|
    | `t1`      | `T1`           | The first value to write to the buffer.   |

    === "Example"
    ???+ example
        ```csharp
        using (var message = NetworkManager.FastWrite(42))
        {
            // Use the message buffer
        }
        ```

=== "FastWrite (T1 t1, T2 t2)"
    Writes two unmanaged values to the buffer.

    | Parameter | Type           | Description                               |
    |-----------|----------------|-------------------------------------------|
    | `t1`      | `T1`           | The first value to write to the buffer.   |
    | `t2`      | `T2`           | The second value to write to the buffer.  |

    === "Example"
    ???+ example
        ```csharp
        using (var message = NetworkManager.FastWrite(42, 3.14f))
        {
            // Use the message buffer
        }
        ```

=== "FastWrite (T1 t1, T2 t2, T3 t3)"
    Writes three unmanaged values to the buffer.

    | Parameter | Type           | Description                               |
    |-----------|----------------|-------------------------------------------|
    | `t1`      | `T1`           | The first value to write to the buffer.   |
    | `t2`      | `T2`           | The second value to write to the buffer.  |
    | `t3`      | `T3`           | The third value to write to the buffer.   |

    === "Example"
    ???+ example
        ```csharp
        using (var message = NetworkManager.FastWrite(42, 3.14f, 'A'))
        {
            // Use the message buffer
        }
        ```

=== "FastWrite (T1 t1, T2 t2, T3 t3, T4 t4)"
    Writes four unmanaged values to the buffer.

    | Parameter | Type           | Description                               |
    |-----------|----------------|-------------------------------------------|
    | `t1`      | `T1`           | The first value to write to the buffer.   |
    | `t2`      | `T2`           | The second value to write to the buffer.  |
    | `t3`      | `T3`           | The third value to write to the buffer.   |
    | `t4`      | `T4`           | The fourth value to write to the buffer.  |

    === "Example"
    ???+ example
        ```csharp
        using (var message = NetworkManager.FastWrite(42, 3.14f, 'A', true))
        {
            // Use the message buffer
        }
        ```

=== "FastWrite (T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)"
    Writes five unmanaged values to the buffer.

    | Parameter | Type           | Description                               |
    |-----------|----------------|-------------------------------------------|
    | `t1`      | `T1`           | The first value to write to the buffer.   |
    | `t2`      | `T2`           | The second value to write to the buffer.  |
    | `t3`      | `T3`           | The third value to write to the buffer.   |
    | `t4`      | `T4`           | The fourth value to write to the buffer.  |
    | `t5`      | `T5`           | The fifth value to write to the buffer.   |

    === "Example"
    ???+ example
        ```csharp
        using (var message = NetworkManager.FastWrite(42, 3.14f, 'A', true, 99))
        {
            // Use the message buffer
        }
        ```

=== "FastWrite (T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)"
    Writes six unmanaged values to the buffer.

    | Parameter | Type           | Description                               |
    |-----------|----------------|-------------------------------------------|
    | `t1`      | `T1`           | The first value to write to the buffer.   |
    | `t2`      | `T2`           | The second value to write to the buffer.  |
    | `t3`      | `T3`           | The third value to write to the buffer.   |
    | `t4`      | `T4`           | The fourth value to write to the buffer.  |
    | `t5`      | `T5`           | The fifth value to write to the buffer.   |
    | `t6`      | `T6`           | The sixth value to write to the buffer.   |

    === "Example"
    ???+ example
        ```csharp
        using (var message = NetworkManager.FastWrite(42, 3.14f, 'A', true, 99, 2.718))
        {
            // Use the message buffer
        }
        ```

Remarks

- **Disposal**: The caller must ensure that the returned `DataBuffer` is disposed or used within a `using` statement to prevent memory leaks.
- **Performance**: By using `stackalloc`, this method avoids heap allocations, offering high performance for network messaging.
- **Generic Constraints**: Only unmanaged types are allowed, ensuring compatibility with `DataBuffer` for efficient serialization.

---

#### FromBinary

Deserializes an object from binary format using `MemoryPackSerializer`.

- **Signature**: `public static T FromBinary<T>(byte[] data, MemoryPackSerializerOptions settings = null)`

Type Parameters

| Type Parameter | Description                              |
|----------------|------------------------------------------|
| `T`            | The type of the object to deserialize.   |

Parameters

| Parameter   | Type                     | Description                                                                                     |
|-------------|--------------------------|-------------------------------------------------------------------------------------------------|
| `data`      | `byte[]`                 | The byte array containing the binary data to deserialize.                                       |
| `settings`  | `MemoryPackSerializerOptions` | Optional settings for deserialization (default is `null`). If `null`, default settings are used.|

Description

The `FromBinary` method deserializes an object of type `T` from a binary byte array using the `MemoryPackSerializer`. It allows specifying optional deserialization settings through `MemoryPackSerializerOptions`. If no settings are provided, the method uses the default settings defined in `BufferWriterExtensions.DefaultMemoryPackSettings`.

Returns

- **`T`**: The deserialized object of type `T`.

???+ example
    ```csharp
    // Example of deserializing an object from binary data
    byte[] binaryData = GetBinaryData(); // Assume this retrieves a valid byte array
    MyObject deserializedObject = NetworkManager.FromBinary<MyObject>(binaryData);
    ```

---

#### ToBinary

Converts an object to binary format using `MemoryPackSerializer`.

- **Signature**: `public static byte[] ToBinary<T>(T obj, MemoryPackSerializerOptions settings = null)`

Type Parameters

| Type Parameter | Description                           |
|----------------|---------------------------------------|
| `T`            | The type of the object to serialize.  |

Parameters

| Parameter   | Type                     | Description                                                                                        |
|-------------|--------------------------|----------------------------------------------------------------------------------------------------|
| `obj`       | `T`                      | The object to be converted to binary format.                                                       |
| `settings`  | `MemoryPackSerializerOptions` | Optional settings for serialization (default is `null`). If `null`, default settings are used.        |

Description

The `ToBinary` method serializes an object of type `T` into a binary byte array using `MemoryPackSerializer`. It allows specifying optional serialization settings via `MemoryPackSerializerOptions`. If no settings are provided, the method uses the default settings from `BufferWriterExtensions.DefaultMemoryPackSettings`.

Returns

- **`byte[]`**: A byte array representing the binary serialization of the object.

???+ example
    ```csharp
    // Example of serializing an object to binary data
    MyObject obj = new MyObject();
    byte[] binaryData = NetworkManager.ToBinary(obj);
    
    // Example with custom settings
    MemoryPackSerializerOptions customSettings = new MemoryPackSerializerOptions
    {
        // Custom settings configuration
    };
    byte[] binaryData = NetworkManager.ToBinary(obj, customSettings);
    ```

---

#### FromJson

Deserializes an object from JSON format.

- **Signature**: `public static T FromJson<T>(string json, JsonSerializerSettings settings = null)`

Type Parameters

| Type Parameter | Description                              |
|----------------|------------------------------------------|
| `T`            | The type of the object to deserialize.   |

Parameters

| Parameter   | Type                     | Description                                                                                  |
|-------------|--------------------------|----------------------------------------------------------------------------------------------|
| `json`      | `string`                 | The JSON string to deserialize.                                                              |
| `settings`  | `JsonSerializerSettings` | Optional settings for JSON deserialization (default is `null`). If `null`, default settings are used. |

Description

The `FromJson` method deserializes an object of type `T` from a JSON string. This method allows specifying optional deserialization settings via `JsonSerializerSettings`. If no settings are provided, the default settings from `BufferWriterExtensions.DefaultJsonSettings` are used.

Returns

- **`T`**: The deserialized object of type `T`.

???+ example
    ```csharp
    // Example of deserializing an object from a JSON string
    string jsonString = "{\"Name\":\"John\", \"Age\":30}";
    Person person = NetworkManager.FromJson<Person>(jsonString);
    
    // Example with custom settings
    JsonSerializerSettings customSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented
    };
    Person person = NetworkManager.FromJson<Person>(jsonString, customSettings);
    ```

---

#### ToJson

Converts an object to JSON format.

- **Signature**: `public static string ToJson<T>(T obj, JsonSerializerSettings settings = null)`

Type Parameters

| Type Parameter | Description                           |
|----------------|---------------------------------------|
| `T`            | The type of the object to serialize.  |

Parameters

| Parameter   | Type                     | Description                                                                                   |
|-------------|--------------------------|-----------------------------------------------------------------------------------------------|
| `obj`       | `T`                      | The object to be converted to JSON format.                                                    |
| `settings`  | `JsonSerializerSettings` | Optional settings for JSON serialization (default is `null`). If `null`, default settings are used. |

Description

The `ToJson` method serializes an object of type `T` into a JSON string using `JsonConvert`. This method allows for optional JSON serialization settings via `JsonSerializerSettings`. If no settings are specified, it uses the default settings defined in `BufferWriterExtensions.DefaultJsonSettings`.

Returns

- **`string`**: A JSON string representation of the serialized object.

???+ example
    ```csharp
    // Example of serializing an object to JSON format
    Person person = new Person { Name = "John", Age = 30 };
    string jsonString = NetworkManager.ToJson(person);
    
    // Example with custom settings
    JsonSerializerSettings customSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented
    };
    string jsonString = NetworkManager.ToJson(person, customSettings);
    ```

---

#### InitializeModule

Initializes a specified network module based on the provided `Module` type.

- **Signature**: `public static void InitializeModule(Module module)`

Parameters

| Parameter | Type    | Description                                     |
|-----------|---------|-------------------------------------------------|
| `module`  | `Module`| The type of module to initialize.               |

Description

The `InitializeModule` method initializes a network module based on the specified `Module` type. This includes setting up components for ticking, network time, console, connections, and matchmaking as required. The method ensures that the initialization occurs on the main thread and applies appropriate configurations based on each module’s unique requirements.

Module Types

- **`TickSystem`**: Initializes a network tick system if one doesn’t already exist. Sets up the tick rate using `Manager.m_TickRate`.
- **`NtpClock`**: Initializes an SNTP clock based on `NetworkClock` settings, configuring interval and tick timing.
- **`Console`**: Initializes a `NetworkConsole` instance.
- **`Connection`**: Sets up the client and server transporters, checks for a transporter component, and configures network connection. Includes logic for auto-starting the server and client based on specific address conditions and the current build configuration.
- **`Matchmaking`**: Initializes the network matchmaking module.

???+ example
    ```csharp
    // Example of initializing the TickSystem module
    NetworkManager.InitializeModule(Module.TickSystem);
    
    // Example of initializing the Connection module
    try
    {
        NetworkManager.InitializeModule(Module.Connection);
    }
    catch (Exception ex)
    {
        Debug.LogError(ex.Message);
    }
    ```

Remarks

- **Thread Safety**: The method enforces that initialization occurs on the main thread to prevent multithreading issues.
- **Connection Transporter**: Throws an Exception if no transporter is found on NetworkManager when initializing the Connection module.
- **Auto-Start Logic**: In the Connection module, auto-start behavior is configured based on the client’s address. The server will only auto-start if the address is recognized as localhost or a public IP address.
- **Build Configuration**: Auto-starting behavior may vary based on build configuration. For instance, in OMNI_RELEASE builds, both server and client auto-start are enabled by default.

---

#### LoadScene / LoadSceneAsync

Loads a scene by name or index, with options for synchronous or asynchronous loading. These methods also provide optional parameters to destroy the current scene before loading a new one.

- **Signature**:
  - `public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)`
  - `public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)`
  - `public static void LoadScene(int index, LoadSceneMode mode = LoadSceneMode.Single)`
  - `public static AsyncOperation LoadSceneAsync(int index, LoadSceneMode mode = LoadSceneMode.Single)`

Parameters

| Parameter    | Type               | Description                                                                         |
|--------------|--------------------|-------------------------------------------------------------------------------------|
| `sceneName`  | `string`           | The name of the scene to load.                                                      |
| `index`      | `int`              | The build index of the scene to load.                                               |
| `mode`       | `LoadSceneMode`    | Specifies whether to load the scene in `Single` or `Additive` mode (default is `Single`). |

Description

Each overload of `LoadScene` and `LoadSceneAsync` allows loading scenes either by name or by build index, with the option to specify loading in single or additive mode. The methods also include a call to `DestroyScene` to clear the current scene before loading the new one, supporting smooth transitions and memory management.

Overloads

=== "LoadScene (string sceneName, LoadSceneMode mode = LoadSceneMode.Single)"
    Loads a scene by its name in synchronous mode.

    | Parameter   | Type             | Description                                           |
    |-------------|------------------|-------------------------------------------------------|
    | `sceneName` | `string`         | The name of the scene to load.                        |
    | `mode`      | `LoadSceneMode`  | Load mode (Single or Additive). Defaults to Single.   |

    === "Example"
    ???+ example
        ```csharp
        // Load a scene by name
        NetworkManager.LoadScene("MainScene");
        ```

=== "LoadSceneAsync (string sceneName, LoadSceneMode mode = LoadSceneMode.Single)"
    Loads a scene by its name in asynchronous mode, returning an `AsyncOperation`.

    | Parameter   | Type             | Description                                           |
    |-------------|------------------|-------------------------------------------------------|
    | `sceneName` | `string`         | The name of the scene to load.                        |
    | `mode`      | `LoadSceneMode`  | Load mode (Single or Additive). Defaults to Single.   |

    === "Example"
    ???+ example
        ```csharp
        // Load a scene asynchronously by name
        AsyncOperation loadOperation = NetworkManager.LoadSceneAsync("MainScene");
        ```

=== "LoadScene (int index, LoadSceneMode mode = LoadSceneMode.Single)"
    Loads a scene by its build index in synchronous mode.

    | Parameter | Type             | Description                                            |
    |-----------|------------------|--------------------------------------------------------|
    | `index`   | `int`            | The build index of the scene to load.                  |
    | `mode`    | `LoadSceneMode`  | Load mode (Single or Additive). Defaults to Single.    |

    === "Example"
    ???+ example
        ```csharp
        // Load a scene by build index
        NetworkManager.LoadScene(1);
        ```

=== "LoadSceneAsync (int index, LoadSceneMode mode = LoadSceneMode.Single)"
    Loads a scene by its build index in asynchronous mode, returning an `AsyncOperation`.

    | Parameter | Type             | Description                                            |
    |-----------|------------------|--------------------------------------------------------|
    | `index`   | `int`            | The build index of the scene to load.                  |
    | `mode`    | `LoadSceneMode`  | Load mode (Single or Additive). Defaults to Single.    |

    === "Example"
    ???+ example
        ```csharp
        // Load a scene asynchronously by build index
        AsyncOperation loadOperation = NetworkManager.LoadSceneAsync(1);
        ```

Remarks

- **DestroyScene**: Each method calls `DestroyScene` before loading to remove the current scene and free resources.
- **LoadSceneMode**: Allows loading in `Single` mode, which replaces the current scene, or `Additive` mode, which loads the new scene on top of the existing one.
- **AsyncOperation**: In asynchronous methods, an `AsyncOperation` is returned, which can be used to track the progress of scene loading.
- **Error Handling**: Ensure the specified scene name or index is valid to avoid loading errors.

---

#### UnloadSceneAsync

Asynchronously unloads a scene by its name or index with options for unloading behavior.

- **Signature**:
  - `public static AsyncOperation UnloadSceneAsync(string sceneName, UnloadSceneOptions options = UnloadSceneOptions.None)`
  - `public static AsyncOperation UnloadSceneAsync(int index, bool useBuildIndex = false, UnloadSceneOptions options = UnloadSceneOptions.None)`

Parameters

| Parameter       | Type                 | Description                                                                                   |
|-----------------|----------------------|-----------------------------------------------------------------------------------------------|
| `sceneName`     | `string`             | The name of the scene to unload.                                                              |
| `index`         | `int`                | The index of the scene to unload, which can refer to the build index or scene load order.     |
| `useBuildIndex` | `bool`               | Indicates if the `index` parameter should be interpreted as the build index (default is `false`). |
| `options`       | `UnloadSceneOptions` | Options for unloading the scene (default is `None`).                                          |

Description

The `UnloadSceneAsync` method provides asynchronous scene unloading capabilities. You can specify the scene to unload either by its name or index, with the option to interpret the index as the build index. Each method first calls `DestroyScene` to handle any necessary scene cleanup before initiating the asynchronous unloading operation using `SceneManager.UnloadSceneAsync`.

Overloads

=== "UnloadSceneAsync (string sceneName, UnloadSceneOptions options = UnloadSceneOptions.None)"
    Unloads a scene asynchronously by its name.

    | Parameter   | Type                 | Description                                                     |
    |-------------|----------------------|-----------------------------------------------------------------|
    | `sceneName` | `string`             | The name of the scene to unload.                                |
    | `options`   | `UnloadSceneOptions` | Options for unloading the scene (default is `None`).            |

    === "Example"
    ???+ example
        ```csharp
        // Unload a scene asynchronously by name
        AsyncOperation unloadOperation = NetworkManager.UnloadSceneAsync("MainScene");
        ```

=== "UnloadSceneAsync (int index, bool useBuildIndex = false, UnloadSceneOptions options = UnloadSceneOptions.None)"
    Unloads a scene asynchronously by its index, with the option to interpret the index as the build index.

    | Parameter       | Type                 | Description                                                        |
    |-----------------|----------------------|--------------------------------------------------------------------|
    | `index`         | `int`                | The index of the scene to unload.                                  |
    | `useBuildIndex` | `bool`               | Whether the index refers to the build index (default is `false`).  |
    | `options`       | `UnloadSceneOptions` | Options for unloading the scene (default is `None`).               |

    === "Example"
    ???+ example
        ```csharp
        // Unload a scene asynchronously by build index
        AsyncOperation unloadOperation = NetworkManager.UnloadSceneAsync(1, useBuildIndex: true);
        ```

Remarks

- **DestroyScene**: Each method calls `DestroyScene` before unloading to handle any necessary cleanup operations and prevent memory leaks.
- **UnloadSceneOptions**: Options can be provided for customizing the unloading process, such as removing dependencies.
- **AsyncOperation**: Returns an `AsyncOperation` that can be used to track the progress of the unloading process.
- **Usage**: Ideal for offloading scenes when they are no longer needed, helping to manage memory and resources efficiently.

---

#### SpawnOnClient

Instantiates a network identity on the client.

- **Signature**: `public static NetworkIdentity SpawnOnClient(NetworkIdentity prefab, int peerId, int identityId)`

Parameters

| Parameter      | Type             | Description                                                                 |
|----------------|------------------|-----------------------------------------------------------------------------|
| `prefab`       | `NetworkIdentity` | The prefab to instantiate.                                                 |
| `peerId`       | `int`             | The ID of the peer who owns the instantiated object.                       |
| `identityId`   | `int`             | The ID of the instantiated object.                                         |

Description

The `SpawnOnClient` method instantiates a `NetworkIdentity` object on the client. It checks if the instantiated object is owned by the local player and updates the `LocalPlayer` reference if appropriate. After instantiation, it notifies the server that this identity has been spawned on the client side.

Returns

- **`NetworkIdentity`**: The instantiated network identity object.

???+ example
    ```csharp
    // Example of spawning a network identity on the client
    NetworkIdentity playerPrefab = GetPlayerPrefab(); // Assume this retrieves a valid NetworkIdentity prefab
    NetworkIdentity newPlayer = NetworkManager.SpawnOnClient(playerPrefab, peerId: 1, identityId: 1001);
    Debug.Log("Player instantiated on client: " + newPlayer.name);
    ```

---

#### SpawnOnServer

Instantiates a network identity on the server for a specific peer or with a specified ID.

- **Signature**:
  - `public static NetworkIdentity SpawnOnServer(NetworkIdentity prefab, NetworkPeer peer)`
  - `public static NetworkIdentity SpawnOnServer(NetworkIdentity prefab, NetworkPeer peer, int identityId)`
  - `public static NetworkIdentity SpawnOnServer(NetworkIdentity prefab, int peerId, int identityId = 0)`

Parameters

| Parameter      | Type             | Description                                                                                   |
|----------------|------------------|-----------------------------------------------------------------------------------------------|
| `prefab`       | `NetworkIdentity` | The prefab to instantiate.                                                                   |
| `peer`         | `NetworkPeer`     | The peer who will receive the instantiated object.                                           |
| `peerId`       | `int`             | The ID of the peer who will receive the instantiated object (for overloads without `peer`).  |
| `identityId`   | `int`             | The ID of the instantiated object. If not provided, a unique ID will be dynamically generated. |

Description

The `SpawnOnServer` method provides multiple overloads to instantiate a `NetworkIdentity` object on the server. The instantiation can be performed for a specific peer, identified either by a `NetworkPeer` object or a `peerId`. If an `identityId` is not provided, a unique ID is generated dynamically.

Overloads

=== "SpawnOnServer (NetworkIdentity prefab, NetworkPeer peer)"
    Instantiates a network identity on the server for a specific peer.

    | Parameter | Type             | Description                            |
    |-----------|------------------|----------------------------------------|
    | `prefab`  | `NetworkIdentity`| The prefab to instantiate.             |
    | `peer`    | `NetworkPeer`    | The peer who will receive the object.  |

    === "Example"
    ???+ example
        ```csharp
        // Example of spawning a network identity on the server for a specific peer
        NetworkIdentity playerPrefab = GetPlayerPrefab();
        NetworkIdentity newPlayer = NetworkManager.SpawnOnServer(playerPrefab, somePeer);
        ```

=== "SpawnOnServer (NetworkIdentity prefab, NetworkPeer peer, int identityId)"
    Instantiates a network identity on the server for a specific peer with a specified identity ID.

    | Parameter    | Type             | Description                               |
    |--------------|------------------|-------------------------------------------|
    | `prefab`     | `NetworkIdentity`| The prefab to instantiate.                |
    | `peer`       | `NetworkPeer`    | The peer who will receive the object.     |
    | `identityId` | `int`            | The ID of the instantiated object.        |

    === "Example"
    ???+ example
        ```csharp
        // Example of spawning a network identity with a specific ID on the server for a peer
        NetworkIdentity newPlayer = NetworkManager.SpawnOnServer(playerPrefab, somePeer, 1001);
        ```

=== "SpawnOnServer (NetworkIdentity prefab, int peerId, int identityId = 0)"
    Instantiates a network identity on the server using a peer ID and an optional identity ID. If `identityId` is not provided, a unique ID is generated dynamically.

    | Parameter    | Type             | Description                                                 |
    |--------------|------------------|-------------------------------------------------------------|
    | `prefab`     | `NetworkIdentity`| The prefab to instantiate.                                  |
    | `peerId`     | `int`            | The ID of the peer who will receive the object.             |
    | `identityId` | `int`            | The ID of the instantiated object, or `0` for a unique ID.  |

    === "Example"
    ???+ example
        ```csharp
        // Example of spawning a network identity on the server by peer ID, with auto-generated ID
        NetworkIdentity newPlayer = NetworkManager.SpawnOnServer(playerPrefab, peerId: 1);
        ```

Remarks

- **Dynamic ID Generation**: If `identityId` is `0`, the method generates a unique ID for the instantiated object using `NetworkHelper.GenerateDynamicUniqueId()`.
- **Peer Association**: The instantiated object is associated with the specified peer, allowing for ownership and network synchronization based on peer ID.
- **Usage**: Suitable for spawning networked objects on the server side, with flexibility for assigning specific identity IDs or generating them dynamically.

---

#### SendMessage(Client)

Sends a message from the client to the server, with options for specifying message content, delivery mode, and sequence channel.

- **Signature**:
  - `public static void SendMessage(byte msgId, SyncOptions options)`
  - `public static void SendMessage(byte msgId, DataBuffer buffer = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)`

Description

The `SendMessage` method allows the client to send a message to the server, either by using `SyncOptions` or by specifying detailed parameters such as `DataBuffer`, `DeliveryMode`, and `SequenceChannel`. This function provides flexible control over how messages are delivered, including options for reliable, ordered delivery and custom sequencing to maintain message order.

Parameters

| Parameter       | Type              | Description                                                                                  |
|-----------------|-------------------|----------------------------------------------------------------------------------------------|
| `msgId`         | `byte`            | The ID of the message to send.                                                               |
| `options`       | `SyncOptions`     | Configuration options for synchronization, including buffer, delivery mode, and channel.     |
| `buffer`        | `DataBuffer`      | Optional data buffer containing the message content.                                         |
| `deliveryMode`  | `DeliveryMode`    | Specifies the delivery mode, such as `ReliableOrdered` or `Unreliable` (default is `ReliableOrdered`). |
| `sequenceChannel` | `byte`          | Channel for message sequencing, to control message order consistency across channels (default is `0`). |

Overloads

=== "SendMessage (byte msgId, SyncOptions options)"
    Sends a message to the server using `SyncOptions` to define the buffer, delivery mode, and sequence channel.

    | Parameter  | Type            | Description                                            |
    |------------|-----------------|--------------------------------------------------------|
    | `msgId`    | `byte`          | The ID of the message to send.                         |
    | `options`  | `SyncOptions`   | Configuration options for synchronization.             |

    === "Example"
    ???+ example
        ```csharp
        // Example of sending a message using SyncOptions
        SyncOptions syncOptions = new SyncOptions(myDataBuffer)
        {
            DeliveryMode = DeliveryMode.Unreliable,
            SequenceChannel = 0
        };
        NetworkManager.Client.SendMessage(1, syncOptions);
        ```

=== "SendMessage (byte msgId, DataBuffer buffer = null, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, byte sequenceChannel = 0)"
    Sends a message to the server with detailed parameters for buffer, delivery mode, and sequencing.

    | Parameter         | Type              | Description                                                  |
    |-------------------|-------------------|--------------------------------------------------------------|
    | `msgId`           | `byte`            | The ID of the message to send.                               |
    | `buffer`          | `DataBuffer`      | Optional data buffer containing the message content.         |
    | `deliveryMode`    | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered`.        |
    | `sequenceChannel` | `byte`            | Channel for message sequencing (default is `0`).             |

    === "Example"
    ???+ example
        ```csharp
        // Example of sending a message with detailed parameters
        NetworkManager.Client.SendMessage(
            1,
            buffer: myDataBuffer,
            deliveryMode: DeliveryMode.Unreliable,
            sequenceChannel: 0
        );
        ```

Remarks

- **Client-to-Server Communication**: Allows the client to send messages to the server, ideal for client-initiated requests or updates.
- **Flexible Delivery Options**: Supports both reliable and unreliable delivery modes, as well as ordered or unordered options, depending on the use case.
- **Sequencing Control**: Use the `sequenceChannel` to maintain message order across multiple channels, ensuring consistency in message flow.
- **Usage**: Commonly used for sending custom data, requests, or status updates from the client to the server.

---

#### SendMessage(Server)

Sends a message from the server to a client or from a client to other networked peers on the server side, with optional configurable options for target, delivery mode, grouping, and sequencing.

- **Signature**:
  - `public static void SendMessage(byte msgId, NetworkPeer peer, SyncOptions options)`
  - `public static void SendMessage(byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)`

Description

The `SendMessage` method provides multiple overloads to send a message through the network with flexible settings for various parameters. The method allows specifying the message ID, target peer, and delivery options like delivery mode, group ID, and sequence channel. This enables fine-grained control over how messages are delivered, such as targeting specific peers, ensuring message ordering, and managing data cache. `SendMessage` is typically used to send data from the server to clients or between clients.

Parameters

| Parameter       | Type              | Description                                                                                   |
|-----------------|-------------------|-----------------------------------------------------------------------------------------------|
| `msgId`         | `byte`            | The ID of the message to send.                                                                |
| `peer`          | `NetworkPeer`     | The peer to send the message to.                                                              |
| `options`       | `SyncOptions`     | A set of options for message synchronization (used in the first overload).                    |
| `buffer`        | `DataBuffer`      | Optional data buffer to send.                                                                 |
| `target`        | `Target`          | Specifies the target for the message (default is `All`).                                      |
| `deliveryMode`  | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered` (default), `Unreliable`, etc.            |
| `groupId`       | `int`             | ID for grouping related messages together (default is `0`).                                   |
| `dataCache`     | `DataCache`       | Cache option for the data being sent (default is `DataCache.None`).                           |
| `sequenceChannel` | `byte`          | Channel for message sequencing to manage order consistency across channels (default is `0`).  |

Overloads

=== "SendMessage (byte msgId, NetworkPeer peer, SyncOptions options)"
    Sends a message using a `SyncOptions` instance to specify buffer, target, delivery mode, group ID, data cache, and sequence channel.

    | Parameter  | Type            | Description                                            |
    |------------|-----------------|--------------------------------------------------------|
    | `msgId`    | `byte`          | The ID of the message to send.                         |
    | `peer`     | `NetworkPeer`   | The peer to send the message to.                       |
    | `options`  | `SyncOptions`   | Configuration options for synchronization.             |

    === "Example"
    ???+ example
        ```csharp
        // Example of sending a message with SyncOptions(optional)
        SyncOptions syncOptions = new SyncOptions(myDataBuffer)
        {
            Target = Target.All,
            DeliveryMode = DeliveryMode.Unreliable,
            GroupId = 0,
            DataCache = DataCache.None,
            SequenceChannel = 0
        };
        NetworkManager.Server.SendMessage(1, clientPeer, syncOptions);
        ```

=== "SendMessage (byte msgId, NetworkPeer peer, DataBuffer buffer = null, Target target = Target.All, DeliveryMode deliveryMode = DeliveryMode.ReliableOrdered, int groupId = 0, DataCache dataCache = default, byte sequenceChannel = 0)"
    Sends a message with detailed parameters for target, delivery mode, grouping, and sequence channel.

    | Parameter         | Type              | Description                                                  |
    |-------------------|-------------------|--------------------------------------------------------------|
    | `msgId`           | `byte`            | The ID of the message to send.                               |
    | `peer`            | `NetworkPeer`     | The peer to send the message to.                             |
    | `buffer`          | `DataBuffer`      | Optional data buffer containing the message content.         |
    | `target`          | `Target`          | The target for the message, such as `All`.   |
    | `deliveryMode`    | `DeliveryMode`    | Defines the delivery mode, such as `ReliableOrdered`.        |
    | `groupId`         | `int`             | ID for grouping related messages (default is `0`).           |
    | `dataCache`       | `DataCache`       | Specifies how the message data is cached (default is `None`).|
    | `sequenceChannel` | `byte`            | Channel for message sequencing (default is `0`).             |

    === "Example"
    ???+ example
        ```csharp
        // Example of sending a message with detailed parameters(optional)
        NetworkManager.Server.SendMessage(
            1,
            clientPeer,
            buffer: myDataBuffer,
            target: Target.All,
            deliveryMode: DeliveryMode.Unreliable,
            groupId: 0,
            dataCache: DataCache.None,
            sequenceChannel: 0
        );
        ```

Remarks

- **Targeting**: Allows flexible message targeting, such as sending to all peers, server-only, or specific groups.
- **Delivery Mode**: Supports reliable and ordered delivery, or unreliable options for lightweight

---

### Properties

#### ReceivedBandwidth

Gets the `BandwidthMonitor` for monitoring the server's or client incoming bandwidth.

- **Signature**: `public static BandwidthMonitor ReceivedBandwidth => Connection.Server.ReceivedBandwidth`

Description

The `ReceivedBandwidth` property provides access to the `BandwidthMonitor` instance that tracks the server’s or client incoming bandwidth usage. This monitor measures the rate of data being received by the server from clients and vice versa, allowing developers to analyze and optimize network performance.

Returns

- **`BandwidthMonitor`**: An instance that tracks and measures the server's or client incoming bandwidth usage.

???+ example
    ```csharp
    // Example of monitoring changes in the server's average received bandwidth
    NetworkManager.Server.ReceivedBandwidth.OnAverageChanged += (avg) =>
    {
        Debug.Log($"Average received bandwidth: {avg} bytes per second");
    };
    
    // Client side
    NetworkManager.Client.ReceivedBandwidth.OnAverageChanged += (avg) =>
    {
        Debug.Log($"Average received bandwidth: {avg} bytes per second");
    };
    ```

---

#### SentBandwidth

Gets the `BandwidthMonitor` for monitoring the server's or client outgoing bandwidth.

- **Signature**: `public static BandwidthMonitor SentBandwidth => Connection.Server.SentBandwidth`

Description

The `SentBandwidth` property provides access to the `BandwidthMonitor` instance that tracks the server’s or client outgoing bandwidth usage. This monitor measures the rate of data being sent from the server to clients and vice versa, allowing developers to observe and manage network performance.

Returns

- **`BandwidthMonitor`**: An instance that tracks and measures the server's or client outgoing bandwidth usage.

???+ example
    ```csharp
    // Example of accessing SentBandwidth
    NetworkManager.Server.SentBandwidth.OnAverageChanged += (avg) =>
    {
        Debug.Log($"Average sent bandwidth: {avg} bytes per second");
    };
    
    // Client side
    NetworkManager.Client.SentBandwidth.OnAverageChanged += (avg) =>
    {
        Debug.Log($"Average sent bandwidth: {avg} bytes per second");
    };
    ```

---

#### ServerPeer

Gets the server peer, which represents the server within the network as a special `NetworkPeer` instance.

- **Signature**: `public static NetworkPeer ServerPeer { get; }`

Description

The `ServerPeer` property provides a `NetworkPeer` instance specifically designated to represent the server. This instance can be used to identify the server in network operations, facilitating communication, control, and synchronization between the server and clients.

Remarks

- **Server Representation**: `ServerPeer` acts as a unique identifier for the server in the network, making it distinct from other peers.
- **Usage**: Useful for operations where the server needs to be addressed specifically, such as broadcasting messages to all clients or handling server-specific logic.
- **Initialization**: The `ServerPeer` is instantiated with a default IP endpoint (`IPAddress.None`) and a port of `0`, signifying that it is used solely for internal identification rather than active communication.

???+ example
    ```csharp
    // Example of using ServerPeer to check if a peer is the server
    if (NetworkManager.Server.ServerPeer.Equals(somePeer))
    {
        Debug.Log("This peer is the server.");
    }
    
    // Example of using ServerPeer for a server-specific operation
    NetworkPeer serverPeer = NetworkManager.Server.ServerPeer;
    Debug.Log("Server peer address: " + serverPeer.EndPoint);
    ```
Remarks

**Client-Side**: When `NetworkManager.Client.ServerPeer` is used, it represents the server peer on the client side, exclusively for encryption keys.

#### Peers

A dictionary that maps peer IDs to `NetworkPeer` instances, providing access to all connected peers by their unique IDs.

- **Signature**: `public static Dictionary<int, NetworkPeer> Peers => PeersById`

Description

The `Peers` property provides a dictionary of all active `NetworkPeer` instances, keyed by their unique integer IDs. This dictionary allows efficient management and retrieval of network peers, making it easy to access specific clients or other networked entities by their ID.

Returns

- **`Dictionary<int, NetworkPeer>`**: A dictionary containing all active `NetworkPeer` instances, indexed by peer IDs.

???+ example
    ```csharp
    // Example of iterating over all connected peers on the server
    foreach (var kvp in NetworkManager.Server.Peers)
    {
        int peerId = kvp.Key;
        NetworkPeer peer = kvp.Value;
        Debug.Log($"Peer ID: {peerId}");
    }
    
    // Example of checking if a specific peer ID exists
    int targetPeerId = 42;
    if (NetworkManager.Server.Peers.ContainsKey(targetPeerId))
    {
        NetworkPeer peer = NetworkManager.Peers[targetPeerId];
        Debug.Log($"Found peer with ID {targetPeerId}");
    }
    else
    {
        Debug.Log($"No peer found with ID {targetPeerId}");
    }
    
    // Client side example
    foreach (var kvp in NetworkManager.Client.Peers)
    {
        int peerId = kvp.Key;
        NetworkPeer peer = kvp.Value;
        Debug.Log($"Peer ID: {peerId}");
    }
    ```

---

#### Identities

A dictionary that stores all `NetworkIdentity` instances, mapped by their unique identity IDs.

- **Signature**: `public static Dictionary<int, NetworkIdentity> Identities { get; }`

Description

The `Identities` property provides access to a dictionary containing all `NetworkIdentity` instances, where each entry is keyed by a unique integer ID (`identityId`). This dictionary allows efficient retrieval and management of networked identities, enabling easy access to any networked object within the application.

Returns

- **`Dictionary<int, NetworkIdentity>`**: A dictionary of `NetworkIdentity` instances, keyed by unique identity IDs.

???+ example
    ```csharp
    // Example of iterating over all NetworkIdentity instances on the server
    foreach (var kvp in NetworkManager.Server.Identities)
    {
        int id = kvp.Key;
        NetworkIdentity identity = kvp.Value;
        Debug.Log($"NetworkIdentity ID: {id}, Name: {identity.name}");
    }
    
    // Example of checking if a specific ID exists
    int identityId = 101;
    if (NetworkManager.Server.Identities.ContainsKey(identityId))
    {
        NetworkIdentity identity = NetworkManager.Identities[identityId];
        Debug.Log($"Found NetworkIdentity with ID {identityId}: {identity.name}");
    }
    else
    {
        Debug.Log($"No NetworkIdentity found with ID {identityId}");
    }
    
    /////////////////////// Client side
    // Example of iterating over all NetworkIdentity instances
    foreach (var kvp in NetworkManager.Client.Identities)
    {
        int id = kvp.Key;
        NetworkIdentity identity = kvp.Value;
        Debug.Log($"NetworkIdentity ID: {id}, Name: {identity.name}");
    }
    
    // Example of checking if a specific ID exists
    if (NetworkManager.Client.Identities.ContainsKey(identityId))
    {
        NetworkIdentity identity = NetworkManager.Identities[identityId];
        Debug.Log($"Found NetworkIdentity with ID {identityId}: {identity.name}");
    }
    ```

---

#### ClientListenPort

Gets the port on which the client listens for incoming connections.

- **Signature**: `public static int ClientListenPort`

Description

The `ClientListenPort` property provides access to the client’s listening port, as configured in `Manager.m_ClientListenPort`. This port is used for network communications on the client side, enabling it to receive messages from the server.

Returns

- **`int`**: The port number on which the client listens.

???+ example
    ```csharp
    // Example of retrieving the client's listening port
    int clientPort = NetworkManager.ClientListenPort;
    Debug.Log("Client listening on port: " + clientPort);
    ```

---

#### ServerListenPort

Gets the port on which the server listens for incoming connections.

- **Signature**: `public static int ServerListenPort`

Description

The `ServerListenPort` property provides access to the server’s listening port, as configured in `Manager.m_ServerListenPort`. This port is used for network communications on the server side, enabling it to accept incoming connections from clients.

Returns

- **`int`**: The port number on which the server listens.

???+ example
    ```csharp
    // Example of retrieving the server's listening port
    int serverPort = NetworkManager.ServerListenPort;
    Debug.Log("Server listening on port: " + serverPort);
    ```

---

#### ConnectPort

Gets the port on which the client connects to the server.

- **Signature**: `public static int ConnectPort`

Description

The `ConnectPort` property provides access to the port number that the client uses to connect to the server, as configured in `Manager.m_ConnectPort`. This port is the endpoint on the server to which the client establishes a connection.

Returns

- **`int`**: The port number that the client uses to connect to the server.

???+ example
    ```csharp
    // Example of retrieving the server's connection port
    int serverConnectionPort = NetworkManager.ConnectPort;
    Debug.Log("Client will connect to server on port: " + serverConnectionPort);
    ```

---

#### ConnectAddress

Gets the IP address or hostname that the client uses to connect to the server.

- **Signature**: `public static string ConnectAddress`

Description

The `ConnectAddress` property provides access to the IP address or hostname of the server that the client connects to, as configured in `Manager.m_ConnectAddress`. This address is used as the endpoint for the client’s connection to the server.

Returns

- **`string`**: The IP address or hostname of the server that the client will connect to.

???+ example
    ```csharp
    // Example of retrieving the server's connection address
    string serverAddress = NetworkManager.ConnectAddress;
    Debug.Log("Client will connect to server at address: " + serverAddress);
    ```

---

#### Framerate

Gets the current framerate of the application.

- **Signature**: `public static float Framerate { get; private set; }`

Description

The `Framerate` property provides access to the application's current framerate. This value can be used to monitor the performance of the application in real-time, allowing developers to adjust settings or configurations based on performance metrics.

Returns

- **`float`**: The current framerate of the application.

???+ example
    ```csharp
    // Example of retrieving the current application framerate
    float currentFramerate = NetworkManager.Framerate;
    Debug.Log("Current application framerate: " + currentFramerate + " FPS");
    ```

---

#### CpuTimeMs

Gets the CPU time in milliseconds per frame.

- **Signature**: `public static float CpuTimeMs { get; private set; }`

Description

The `CpuTimeMs` property provides access to the CPU time taken per frame, measured in milliseconds. This metric indicates the amount of time the CPU spends processing each frame, which is useful for monitoring and optimizing application performance.

Returns

- **`float`**: The CPU time per frame, in milliseconds.

???+ example
    ```csharp
    // Example of retrieving the CPU time per frame in milliseconds
    float cpuTimePerFrame = NetworkManager.CpuTimeMs;
    Debug.Log("CPU time per frame: " + cpuTimePerFrame + " ms");
    ```

---

#### ClockTime

Gets the current clock time in seconds, based on tick timing or elapsed stopwatch time.

- **Signature**: `public static double ClockTime`

Description

The `ClockTime` property provides the current time in seconds, which is independent of the frame rate. The time source depends on the `UseTickTiming` setting:
- **Tick Timing**: If `UseTickTiming` is enabled, the time is based on the `TickSystem.ElapsedTicks`.
- **Stopwatch**: If `UseTickTiming` is disabled, the time is derived from `_stopwatch.Elapsed.TotalSeconds`.

This setup allows for precise timing control, whether using fixed ticks or continuous stopwatch measurements.

Returns

- **`double`**: The current clock time in seconds.

???+ example
    ```csharp
    // Example of retrieving the current clock time
    double currentTime = NetworkManager.ClockTime;
    Debug.Log("Current clock time: " + currentTime + " seconds");
    ```

---

#### UseTickTiming

Indicates whether the application is using tick-based timing for the `ClockTime` property.

- **Signature**: `public static bool UseTickTiming { get; private set; } = false`

Description

The `UseTickTiming` property determines whether the application relies on tick-based timing or real-time stopwatch timing for the `ClockTime` property. When `UseTickTiming` is set to `true`, `ClockTime` is based on `TickSystem.ElapsedTicks`; otherwise, it uses `_stopwatch.Elapsed.TotalSeconds`.

Returns

- **`bool`**: `true` if tick-based timing is enabled; `false` if stopwatch timing is used.

???+ example
    ```csharp
    // Example of checking the timing mode
    if (NetworkManager.UseTickTiming)
    {
        Debug.Log("Using tick-based timing for clock time.");
    }
    else
    {
        Debug.Log("Using real-time stopwatch timing for clock time.");
    }
    ```

---

#### LocalEndPoint

Gets the local network endpoint, represented by an `IPEndPoint` instance.

- **Signature**: `public static IPEndPoint LocalEndPoint { get; private set; }`

Description

The `LocalEndPoint` property provides the IP address and port number of the local peer in the form of an `IPEndPoint` object. This endpoint represents the local network location, allowing other peers to identify and communicate with the local instance.

Returns

- **`IPEndPoint`**: The IP address and port of the local peer.

???+ example
    ```csharp
    // Example of retrieving the local network endpoint
    IPEndPoint localEndpoint = NetworkManager.LocalEndPoint;
    Debug.Log("Local IP Address: " + localEndpoint.Address + ", Port: " + localEndpoint.Port);
    ```

---

#### IsClientActive

Indicates whether the client is currently active, authenticated, and connected.

- **Signature**: `public static bool IsClientActive { get; private set; }`

Description

The `IsClientActive` property returns `true` if the client is currently active, authenticated, and connected to the network; otherwise, it returns `false`. This property is used to determine the client’s connection status, allowing the application to handle client-specific logic based on whether the client is connected.

Returns

- **`bool`**: `true` if the client is active, authenticated, and connected; `false` otherwise.

???+ example
    ```csharp
    // Example of checking if the client is active
    if (NetworkManager.IsClientActive)
    {
        Debug.Log("Client is active and connected to the network.");
    }
    else
    {
        Debug.Log("Client is not active.");
    }
    ```

---

#### IsServerActive

Indicates whether the server is currently active.

- **Signature**: `public static bool IsServerActive { get; private set; }`

Description

The `IsServerActive` property returns `true` if the server is currently active and ready to accept connections; otherwise, it returns `false`. This property is used to determine the server’s operational status within the network, allowing the application to enable or disable server-dependent features accordingly.

Returns

- **`bool`**: `true` if the server is active; `false` otherwise.

???+ example
    ```csharp
    // Example of checking if the server is active
    if (NetworkManager.IsServerActive)
    {
        Debug.Log("Server is active and accepting connections.");
    }
    else
    {
        Debug.Log("Server is not active.");
    }
    ```

---

#### SharedPeer

Gets the shared peer, which is used to secure communication between peers and the server. This peer is useful for handling encryption and authentication in network communications.

- **Signature**: `public static NetworkPeer SharedPeer`

Description

The `SharedPeer` property provides access to the network peer responsible for securing communication between the client and server. Depending on the network state, it returns the appropriate peer for encryption and authentication:
  - **Client-Server Mode**: When both the client and server are active, it returns `Server.ServerPeer`.
  - **Client Only**: When only the client is active, it returns `Client.ServerPeer`.
  - **Server Only**: When only the server is active, it defaults to `Server.ServerPeer`.

This flexibility allows the property to provide the correct peer for communication in different network configurations.

Returns

- **`NetworkPeer`**: The peer used for secure communication.

???+ example
    ```csharp
    // Example of retrieving the shared peer for secure communication
    NetworkPeer securePeer = NetworkManager.SharedPeer;
    Debug.Log("Shared peer for secure communication: " + securePeer);
    ```

---

#### LocalPeer

Gets the local network peer, representing the client in the network.

- **Signature**: `public static NetworkPeer LocalPeer`

Description

The `LocalPeer` property provides access to an instance of the `NetworkPeer` class that represents the local peer in the network. This property is used to identify and interact with the local peer in networked operations. If the client is not active or authenticated, attempting to access this property throws an exception, ensuring that only active and verified clients can reference `LocalPeer`.

Returns

- **`NetworkPeer`**: The local peer in the network.

???+ example
    ```csharp
    // Example of retrieving the local network peer
    try
    {
        NetworkPeer localPeer = NetworkManager.LocalPeer;
        Debug.Log("Local network peer: " + localPeer);
    }
    catch (Exception ex)
    {
        Debug.LogError(ex.Message);
    }
    ```

---

#### MainThreadId

Gets the ID of the main thread on which the application is running.

- **Signature**: `public static int MainThreadId { get; private set; }`

Description

The `MainThreadId` property provides the thread ID of the main thread, allowing the application to distinguish between the main thread and any background or worker threads. This is useful for ensuring that certain operations, particularly those involving UI updates or Unity API calls, are executed on the main thread.

Returns

- **`int`**: The ID of the main thread.

???+ example
    ```csharp
    // Example of retrieving the main thread ID
    int mainThreadId = NetworkManager.MainThreadId;
    Debug.Log("Main thread ID: " + mainThreadId);
    ```

---

#### Pool

Gets the pool of `DataBuffer` instances, used to allocate and deallocate `DataBuffer` objects efficiently.

- **Signature**: `public static IObjectPooling<DataBuffer> Pool { get; private set; }`

Description

The `Pool` property provides access to an instance of `IObjectPooling<DataBuffer>`, which manages the allocation and deallocation of `DataBuffer` instances. By pooling `DataBuffer` objects, the application reduces memory allocation overhead and enhances performance, especially in networked applications where buffers are frequently used.

Returns

- **`IObjectPooling<DataBuffer>`**: The object pool for managing `DataBuffer` instances.

???+ example
    ```csharp
    // Example 1 of using the DataBuffer pool
    DataBuffer buffer = NetworkManager.Pool.Rent();
    NetworkManager.Pool.Return(buffer); // Return the buffer to the pool
    
    // Example 2(Recommended)
    using DataBuffer buffer = NetworkManager.Pool.Rent(); // Using return the buffer to the pool
    
    // Example 3
    DataBuffer buffer = NetworkManager.Pool.Rent();
    buffer.Dispose(); // Return the buffer to the pool
    ```

---

#### Console

Gets the `NetworkConsole` instance, which provides access to the network console module for sending and receiving commands.

- **Signature**: `public static NetworkConsole Console`

Description

The `Console` property provides access to the `NetworkConsole` instance, which enables sending and receiving commands within the network terminal. This console module is used to execute commands, facilitate remote debugging, and interact with networked systems. Attempting to access this property before initializing the console module will throw an exception, ensuring that `InitializeModule(Module.Console)` is called prior to usage. Additionally, trying to set the console module more than once will result in an exception.

Returns

- **`NetworkConsole`**: The network console instance used for terminal commands.

???+ example
    ```csharp
    try
    {
        NetworkConsole networkConsole = NetworkManager.Console;
        networkConsole.OnInput += OnConsoleInput;
    
        private void OnConsoleInput(string input)
        {
            // Handle console input
        }
    }
    catch (Exception ex)
    {
        Debug.LogError(ex.Message);
    }
    ```

---

#### Matchmaking

Gets the `NetworkMatchmaking` instance, which provides access to the matchmaking module for managing groups and connections.

- **Signature**: `public static NetworkMatchmaking Matchmaking`

Description

The `Matchmaking` property provides access to the `NetworkMatchmaking` instance, enabling functionality for creating, deleting, and joining groups in a networked environment. This module is essential for managing player groups and facilitating connections between peers for cooperative or competitive play. Attempting to access this property before initializing the matchmaking module will throw an exception, ensuring that `InitializeModule(Module.Matchmaking)` is called prior to usage. Additionally, trying to set the matchmaking module more than once will result in an exception.

Returns

- **`NetworkMatchmaking`**: The matchmaking instance used for group and connection management.

???+ example
    ```csharp
    try
    {
       var matchmaking = NetworkManager.Matchmaking.Server;
       NetworkGroup group = matchmaking.AddGroup("Team A");
    }
    catch (Exception ex)
    {
        Debug.LogError(ex.Message);
    }
    ```

---

#### Sntp

Gets the `SimpleNtp` instance, which provides access to the NTP (Network Time Protocol) clock module for synchronized time and network latency metrics.

- **Signature**: `public static SimpleNtp Sntp`

Description

The `Sntp` property provides access to the `SimpleNtp` instance, allowing for synchronized time between client and server. This module provides functionality for obtaining synchronized ticks or real time, as well as metrics such as round-trip time (RTT) and ping. Attempting to access this property before initializing the NTP clock module will throw an exception, ensuring that `InitializeModule(Module.NtpClock)` is called prior to usage. Additionally, attempting to set the NTP clock module more than once will result in an exception.

Returns

- **`SimpleNtp`**: The NTP clock instance used for synchronized time and latency measurements.

???+ example
    ```csharp
    // Example of initializing and accessing the NTP clock module
    NetworkManager.InitializeModule(Module.NtpClock); // Initialize the NTP clock module
    
    try
    {
        var ntpClock = NetworkManager.Sntp;
        double synchronizedTime = ntpClock.Client.Time; // Example of retrieving synchronized time
        print(synchronizedTime);
    }
    catch (Exception ex)
    {
        Debug.LogError(ex.Message);
    }
    ```

---

#### TickSystem

Gets the `NetworkTickSystem` instance, which manages tick-based events that execute at a fixed rate for consistent network timing.

- **Signature**: `public static NetworkTickSystem TickSystem`

Description

The `TickSystem` property provides access to the `NetworkTickSystem` instance, responsible for executing events at a fixed rate (defined by `TickRate`) to ensure consistent timing across the network. This system is used to send and process information at a steady frequency, facilitating synchronized interactions between clients and servers. Attempting to access this property before initializing the tick system will throw an exception, ensuring that `InitializeModule(Module.TickSystem)` is called prior to usage.

Returns

- **`NetworkTickSystem`**: The network tick system instance, used for managing tick-based events and timing synchronization.

---

### Events

#### OnSceneLoaded

An event that is triggered when a scene is loaded, providing access to the scene and the load mode.

- **Signature**: `public static event Action<Scene, LoadSceneMode> OnSceneLoaded`

Description

The `OnSceneLoaded` event is invoked whenever a scene is loaded, passing the loaded `Scene` and the `LoadSceneMode` as parameters. This event allows for actions to be performed in response to a scene loading, such as initializing objects or updating the UI. Developers can subscribe to `OnSceneLoaded` to execute custom logic each time a new scene is loaded.

Event Parameters

| Parameter      | Type           | Description                                                       |
|----------------|----------------|-------------------------------------------------------------------|
| `Scene`        | `Scene`        | The scene that has been loaded.                                   |
| `LoadSceneMode`| `LoadSceneMode`| The mode in which the scene was loaded (`Single` or `Additive`).  |

???+ example
    ```csharp
    // Example of subscribing to the OnSceneLoaded event
    NetworkManager.OnSceneLoaded += (scene, mode) =>
    {
        Debug.Log($"Scene '{scene.name}' loaded with mode: {mode}");
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnSceneLoaded -= (scene, mode) =>
    {
        Debug.Log($"Scene '{scene.name}' loaded with mode: {mode}");
    };
    ```

---

#### OnSceneUnloaded

An event that is triggered when a scene is unloaded, providing access to the scene that was removed.

- **Signature**: `public static event Action<Scene> OnSceneUnloaded`

Description

The `OnSceneUnloaded` event is invoked whenever a scene is unloaded, passing the `Scene` that was unloaded as a parameter. This event allows developers to execute custom logic in response to scene unloading, such as cleaning up resources, stopping specific services, or updating the user interface.

Event Parameters

| Parameter | Type   | Description                         |
|-----------|--------|-------------------------------------|
| `Scene`   | `Scene`| The scene that has been unloaded.   |

???+ example
    ```csharp
    // Example of subscribing to the OnSceneUnloaded event
    NetworkManager.OnSceneUnloaded += (scene) =>
    {
        Debug.Log($"Scene '{scene.name}' has been unloaded.");
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnSceneUnloaded -= (scene) =>
    {
        Debug.Log($"Scene '{scene.name}' has been unloaded.");
    };
    ```

---

#### OnBeforeSceneLoad

An event that is triggered just before a scene is loaded or unloaded, providing the scene and the operation mode (load or unload).

- **Signature**: `public static event Action<Scene, SceneOperationMode> OnBeforeSceneLoad`

Description

The `OnBeforeSceneLoad` event is invoked right before a scene is either loaded or unloaded, passing the target `Scene` and the `SceneOperationMode` to indicate the type of operation. This event allows developers to execute preparatory logic, such as pausing certain activities or preparing resources, cleaning up resources, based on whether a scene is about to load or unload.

Event Parameters

| Parameter          | Type               | Description                                                          |
|--------------------|--------------------|----------------------------------------------------------------------|
| `Scene`            | `Scene`            | The scene that is about to be loaded or unloaded.                    |
| `SceneOperationMode` | `SceneOperationMode` | Specifies whether the scene operation is a `Load` or `Unload`.       |

SceneOperationMode Enum

The `SceneOperationMode` enum defines the operation type for a scene:

- **`Load`**: The scene is about to be loaded.
- **`Unload`**: The scene is about to be unloaded.

???+ example
    ```csharp
    // Example of subscribing to the OnBeforeSceneLoad event
    NetworkManager.OnBeforeSceneLoad += (scene, operationMode) =>
    {
        if (operationMode == SceneOperationMode.Load)
        {
            Debug.Log($"Preparing to load scene '{scene.name}'.");
        }
        else if (operationMode == SceneOperationMode.Unload)
        {
            Debug.Log($"Preparing to unload scene '{scene.name}'.");
        }
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnBeforeSceneLoad -= (scene, operationMode) =>
    {
        Debug.Log($"Operation {operationMode} for scene '{scene.name}' is about to begin.");
    };
    ```

---

#### OnServerInitialized

An event that is triggered when the server has been successfully initialized.

- **Signature**: `public static event Action OnServerInitialized`

Description

The `OnServerInitialized` event is invoked once the server has been fully initialized and is ready to accept connections. This event allows developers to execute custom logic or set up necessary resources immediately after the server is initialized, such as configuring game settings, starting background tasks.

???+ example
    ```csharp
    // Example of subscribing to the OnServerInitialized event
    NetworkManager.OnServerInitialized += () =>
    {
        Debug.Log("Server has been successfully initialized and is ready for connections.");
        // Additional server setup code here
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnServerInitialized -= () =>
    {
        Debug.Log("Server initialization event listener removed.");
    };
    ```

---

#### OnServerPeerConnected

An event that is triggered when a peer (client) connects to the server, providing access to the connected peer and the connection phase.

- **Signature**: `public static event Action<NetworkPeer, Phase> OnServerPeerConnected`

Description

The `OnServerPeerConnected` event is invoked when a peer connects to the server, with the connection process divided into three phases (`Begin`, `Normal`, and `End`). This event provides developers with the connected `NetworkPeer` instance and the current `Phase`, allowing for custom logic to be executed at each stage of the connection.

Event Parameters

| Parameter      | Type          | Description                                              |
|----------------|---------------|----------------------------------------------------------|
| `NetworkPeer`  | `NetworkPeer` | The peer (client) that has connected to the server.      |
| `Phase`        | `Phase`       | The phase of the connection process (Begin, Normal, End).|

Phase Enum

The `Phase` enum defines the phases of the peer connection process:

- **`Begin`**: Indicates the start of the connection process.
- **`Normal`**: Represents the main connection phase, during which the primary actions are performed.
- **`End`**: Marks the completion of the connection process, the peer is connected and authenticated, ready to use.

???+ example
    ```csharp
    // Example of subscribing to the OnServerPeerConnected event
    NetworkManager.OnServerPeerConnected += (peer, phase) =>
    {
        switch (phase)
        {
            case Phase.Begin:
                Debug.Log($"Peer {peer.Id} is starting the connection process.");
                break;
            case Phase.Normal:
                Debug.Log($"Peer {peer.Id} is now in the main connection phase.");
                break;
            case Phase.End:
                Debug.Log($"Peer {peer.Id} has completed the connection process.");
                break;
        }
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnServerPeerConnected -= (peer, phase) =>
    {
        Debug.Log($"Listener for peer {peer.Id} connection at phase {phase} removed.");
    };
    ```

---

#### OnServerPeerDisconnected

An event that is triggered when a peer (client) disconnects from the server, providing access to the disconnected peer and the disconnection phase.

- **Signature**: `public static event Action<NetworkPeer, Phase> OnServerPeerDisconnected`

Description

The `OnServerPeerDisconnected` event is invoked when a peer disconnects from the server, with the disconnection process divided into three phases (`Begin`, `Normal`, and `End`). This event provides the `NetworkPeer` instance that is disconnecting and the current `Phase`, allowing for custom logic to be executed at each stage of the disconnection.

Event Parameters

| Parameter      | Type          | Description                                                 |
|----------------|---------------|-------------------------------------------------------------|
| `NetworkPeer`  | `NetworkPeer` | The peer (client) that is disconnecting from the server.    |
| `Phase`        | `Phase`       | The phase of the disconnection process (Begin, Normal, End).|

Phase Enum

The `Phase` enum defines the phases of the peer disconnection process:

- **`Begin`**: Indicates the start of the disconnection process.
- **`Normal`**: Represents the main phase of disconnection, during which the primary cleanup or disconnection actions are performed.
- **`End`**: Marks the completion of the disconnection process, the peer has fully disconnected from the server and all resources have been cleaned up.

???+ example
    ```csharp
    // Example of subscribing to the OnServerPeerDisconnected event
    NetworkManager.OnServerPeerDisconnected += (peer, phase) =>
    {
        switch (phase)
        {
            case Phase.Begin:
                Debug.Log($"Peer {peer.Id} is beginning the disconnection process.");
                break;
            case Phase.Normal:
                Debug.Log($"Peer {peer.Id} is in the main disconnection phase.");
                break;
            case Phase.End:
                Debug.Log($"Peer {peer.Id} has fully disconnected from the server.");
                break;
        }
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnServerPeerDisconnected -= (peer, phase) =>
    {
        Debug.Log($"Listener for peer {peer.Id} disconnection at phase {phase} removed.");
    };
    ```

---

#### OnClientConnected

An event that is triggered when the client successfully connects to the server.

- **Signature**: `public static event Action OnClientConnected`

Description

The `OnClientConnected` event is invoked when the client establishes a successful connection with the server. This event allows developers to execute custom logic upon client connection, such as initializing UI elements, loading player data, or notifying the user of the connection status.

???+ example
    ```csharp
    // Example of subscribing to the OnClientConnected event
    NetworkManager.OnClientConnected += () =>
    {
        Debug.Log("Client successfully connected to the server.");
        // Additional connection setup code here
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnClientConnected -= () =>
    {
        Debug.Log("Client connection listener removed.");
    };
    ```

---

#### OnClientDisconnected

An event that is triggered when the client disconnects from the server, providing a message with the disconnection reason.

- **Signature**: `public static event Action<string> OnClientDisconnected`

Description

The `OnClientDisconnected` event is invoked when the client disconnects from the server. This event provides a string message detailing the reason for disconnection, allowing developers to display informative messages to users, handle cleanup tasks, or attempt reconnection based on the disconnection reason.

Event Parameters

| Parameter | Type     | Description                            |
|-----------|----------|----------------------------------------|
| `string`  | `string` | A message explaining the disconnection reason. |

???+ example
    ```csharp
    // Example of subscribing to the OnClientDisconnected event
    NetworkManager.OnClientDisconnected += (reason) =>
    {
        Debug.Log($"Client disconnected: {reason}");
        // Additional disconnection handling code here
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnClientDisconnected -= (reason) =>
    {
        Debug.Log("Client disconnection listener removed.");
    };
    ```

---

#### OnClientIdentitySpawned

An event that is triggered when a `NetworkIdentity` is spawned on the client, providing access to the spawned identity.

- **Signature**: `public static event Action<NetworkIdentity> OnClientIdentitySpawned`

Description

The `OnClientIdentitySpawned` event is invoked whenever a `NetworkIdentity` is successfully spawned on the client. This event provides the spawned `NetworkIdentity` instance, allowing developers to perform setup, initialize components, or trigger gameplay elements related to the spawned entity.

Event Parameters

| Parameter       | Type            | Description                              |
|-----------------|-----------------|------------------------------------------|
| `NetworkIdentity` | `NetworkIdentity` | The network identity that was spawned. |

???+ example
    ```csharp
    // Example of subscribing to the OnClientIdentitySpawned event
    NetworkManager.OnClientIdentitySpawned += (identity) =>
    {
        Debug.Log($"Network identity spawned on client: {identity.name}");
        // Additional setup or initialization for the spawned identity
    };
    
    // Example of unsubscribing from the event
    NetworkManager.OnClientIdentitySpawned -= (identity) =>
    {
        Debug.Log("Client identity spawn listener removed.");
    };
    ```

---

#### OnMessage(Client)

Represents an event that is triggered when a custom message is received by the client, providing the message ID, data buffer, and sequence channel.

- **Signature**: `public static event Action<byte, DataBuffer, int> OnMessage`

Description

The `OnMessage` event is invoked whenever the client receives a custom message. This event provides access to the message ID, the data buffer containing the message content, and the sequence channel. Developers can subscribe to `OnMessage` to handle incoming messages, enabling custom message processing and response handling on the client side.

Event Parameters

| Parameter       | Type         | Description                                                 |
|-----------------|--------------|-------------------------------------------------------------|
| `byte`          | `byte`       | The ID of the received message.                             |
| `DataBuffer`    | `DataBuffer` | The data buffer containing the message content.             |
| `int`           | `int`        | The sequence channel used for message ordering consistency. |

???+ example
    ```csharp
    // Example of subscribing to the OnMessage event to handle incoming messages
    NetworkManager.Client.OnMessage += (msgId, dataBuffer, sequenceChannel) =>
    {
        Debug.Log($"Received message {msgId} on sequence channel {sequenceChannel}");
        // Process dataBuffer as needed
    };
    
    // Example of unsubscribing from the event
    NetworkManager.Client.OnMessage -= (msgId, dataBuffer, sequenceChannel) =>
    {
        Debug.Log($"Unsubscribed from message {msgId}");
    };
    ```

---

#### OnMessage(Server)

An event that is triggered when a custom message is received by the server, providing access to the message ID, data buffer, originating peer, and sequence channel.

- **Signature**: `public static event Action<byte, DataBuffer, NetworkPeer, int> OnMessage`

Description

The `OnMessage` event is invoked when the server receives a custom message, allowing developers to handle the incoming message data. This event provides the message ID, data content, the peer who sent the message, and the sequence channel used to manage message ordering. It serves as an interface for handling various types of client-server communication in a flexible manner.

Event Parameters

| Parameter       | Type         | Description                                                     |
|-----------------|--------------|-----------------------------------------------------------------|
| `byte`          | `byte`       | The ID of the received message.                                 |
| `DataBuffer`    | `DataBuffer` | The data buffer containing the message content.                 |
| `NetworkPeer`   | `NetworkPeer`| The peer who sent the message.                                  |
| `int`           | `int`        | The sequence channel used for message ordering and consistency. |

???+ example
    ```csharp
    // Example of subscribing to the OnMessage event
    NetworkManager.Server.OnMessage += (msgId, dataBuffer, peer, sequenceChannel) =>
    {
        Debug.Log($"Received message {msgId} from peer {peer.Id} on sequence channel {sequenceChannel}");
        // Process dataBuffer as needed
    };
    
    // Example of unsubscribing from the event
    NetworkManager.Server.OnMessage -= (msgId, dataBuffer, peer, sequenceChannel) =>
    {
        Debug.Log($"Unsubscribed from custom message {msgId}");
    };
    ```