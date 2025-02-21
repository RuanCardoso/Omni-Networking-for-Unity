## RPC (Remote Procedure Call)

RPCs (Remote Procedure Calls) are a standard software industry concept that allows methods to be called on objects that are not in the same executable. They enable communication between different processes or systems over a network.

With RPCs, a server can invoke functions on a client, and similarly, a client can invoke functions on a server. This bi-directional communication allows for flexible and dynamic interactions between clients and servers, facilitating various operations such as requesting data, executing commands, and synchronizing states across different parts of a distributed system. RPCs provide a powerful mechanism for enabling remote interactions and enhancing the functionality of networked applications.

---

!!! info "RPC Basic Structure"
    <center>
    ``` mermaid
    graph LR
      A[Ruan<br>___Local Player___] ---> | User Input Rpc | B{Server}
      B ---> | Move Rpc | A
      B ---> | Move Rpc | C[Junior<br>___Remote Player___]
      B ---> | Move Rpc | D[Mike<br>___Remote Player___]
    ```
    </center>

     The diagram illustrates the basic flow of an RPC (Remote Procedure Call) in a multiplayer environment.  

    **RPC Flow**

    1. **Local Player (Ruan)**
        - Sends input to server via RPC
        - Receives validated updates back
    
    2. **Server**
        - Validates input
        - Broadcasts updates to all players
    
    3. **Remote Players**
        - Receive server-validated updates
        - Apply changes to maintain sync
---                   

!!! tip "RPC Naming Convention and Base Classes"
    RPC's are also supported in base classes. If you are using a base class for network functionality, ensure that the base class name includes the `Base` prefix.

    **Naming Convention**

    Base classes using RPCs **must** include the `Base` suffix:

    - ✅ `PlayerBase`
    - ✅ `CharacterBase` 
    - ❌ `BasePlayer`
    - ❌ `Player`
    
    ???+ example
        ```csharp
        public class PlayerBase : NetworkBehaviour // Note the "Base" prefix
        {
            const byte FireRpcId = 1;

            // 1. Define virtual RPCs here
            [Client(FireRpcId)]
            protected virtual void FireRpc()
            {
               Debug.Log("Fired from base class");
            }

            void Update()
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (IsServer)
                    {
                        // 2. Server invokes the RPC on the client
                        Server.Rpc(FireRpcId);
                    }
                }
            }
        }
        
        public class Player : PlayerBase
        {
            // 3. Implement the virtual RPC in the derived class
            protected override void FireRpc()
            {
               Debug.Log("Fired from derived class");
            }
        }
        ```
   
!!! note
    Before proceeding, refer to the [Communication Structure](./index.md#communication-structure) and [Service Locator Pattern](./index.md#service-locator-pattern) pages for essential background information.

---

### Method Signature

Remote Procedure Calls (RPCs) in our networking system allow communication between clients and servers. Each RPC method can be configured with different parameters to handle various networking scenarios.

RPC methods can accept up to three parameters:

| Parameter | Description | Availability |
|-----------|-------------|--------------|
| `DataBuffer message` | Contains the transmitted data | Client & Server |
| `NetworkPeer peer` | Information about the calling client | Server Only |
| `int seqChannel` | Controls message ordering and priority | Client & Server |

Remote Procedure Calls (RPCs) require proper method decoration with either:

- `[Server]` - Marks a method that executes on the server when called by a client
- `[Client]` - Marks a method that executes on clients when called by the server 

Each RPC method must specify a unique numeric ID between 1-230 within its class:

```csharp
[Server(1)] // Method executes on server when invoked from client, using ID 1
void MyServerMethod() { }

[Client(2)] // Method executes on client when invoked from server, using ID 2  
void MyClientMethod() { }
```

Here are all valid server RPC signatures, from simplest to most complex:

#### Server-Side Signatures

=== "Signature 1"
    ???+ example "Basic RPC - No parameters"
        ```csharp
        public class Player : NetworkBehaviour
        {
            [Server(1)]
            void OnServerMethod()
            {
               // Handles simple server-side logic
               // Use when no data transfer is needed
            }
        }
        ```
=== "Signature 2"
    ???+ example "Data-only RPC"
        ```csharp
        public class Player : NetworkBehaviour
        {
            [Server(1)]
            void OnServerMethod(DataBuffer message)
            {
                // Handles incoming client data
                // Example: Reading player input
                var input = message.Read<Vector2>();
            }
        }
        ```
=== "Signature 3"
    ???+ example "Data + Peer Info"
        ```csharp
        public class Player : NetworkBehaviour
        {
            [Server(1)]
            void OnServerMethod(DataBuffer message, NetworkPeer peer)
            {
               // Handles data with client identification
               // Example: Processing authenticated requests
               if ((bool)peer.Data["IsAuthenticated"]) {
                   ProcessRequest(message);
               }
            }
        }
        ```
=== "Signature 4"
    ???+ example "Full Configuration"
        ```csharp
        public class Player : NetworkBehaviour
        {
            [Server(1)]
            void OnServerMethod(DataBuffer message, NetworkPeer peer, int seqChannel)
            {
               // Complete control over message handling
               // Example: Priority-based game events
            }
        }
        ```

Valid client RPC signatures with common use cases:

#### Client-Side Signatures

=== "Signature 1"
    ???+ example "Basic RPC - No parameters"
        ```csharp
        public class Player : NetworkBehaviour
        {
            [Client(1)]
            void OnClientMethod()
            {
                // Simple client-side updates
                // Example: UI refreshes
                UpdatePlayerUI();
            }
        }
        ```
=== "Signature 2"
    ???+ example "Data-only RPC"
        ```csharp
        public class Player : NetworkBehaviour
        {
            [Client(1)]
            void OnClientMethod(DataBuffer message)
            {
                // Handle server data
                // Example: Receiving game state
                var position = message.Read<Vector3>();
                UpdatePlayerPosition(position);
            }
        }
        ```
=== "Signature 3"
    ???+ example "Data + Channel"
        ```csharp
        public class Player : NetworkBehaviour
        {
            [Client(1)]
            void OnClientMethod(DataBuffer message, int seqChannel)
            {
                // Ordered message processing
                // Example: Animation sequences
                ProcessAnimationSequence(message, seqChannel);
            }
        }
        ```

---

!!! info "RPC ID System"
    📝 Each RPC method requires a unique numeric identifier (ID) within its class:

    - IDs are used for message routing between client and server
    - Only needs to be unique within the same class
    - Different classes can reuse the same IDs
    ```csharp
    // Valid - Different classes can use same ID
    public class PlayerSystem {
        [Server(1)] void Method1() { }
        [Server(2)] void Method2() { }  // ✅ Unique within class
    }
    
    public class InventorySystem {
        [Server(1)] void Method1() { }  // ✅ OK to reuse ID in different class
    }
    ```

!!! danger "ID Range Requirements"
    ⚠️ RPC IDs must follow these rules:

    - Valid range: `1` to `230`
    - Cannot be zero or negative
    - Cannot exceed 230
    ```csharp
    // Invalid ID examples
    [Server(0)]    void Invalid1() { }  // ❌ Zero not allowed
    [Server(-1)]   void Invalid2() { }  // ❌ Negative not allowed
    [Server(231)]  void Invalid3() { }  // ❌ Exceeds maximum
    
    // Valid ID example
    [Server(1)]    void Valid() { }     // ✅ Correct usage
    ```
    💥 **Runtime Exception** will be thrown if these rules are violated

!!! tip "Use Constants for RPC IDs"
    It's recommended to use constants for RPC IDs to improve code maintainability and prevent duplicate IDs. This makes it easier to manage and refactor RPC calls across your codebase.
    ???+ example
        ```csharp
        public class Player : NetworkBehaviour
        {
            private const byte MOVE_RPC_ID = 1;
            private const byte ATTACK_RPC_ID = 2;
            private const byte HEAL_RPC_ID = 3;

            [Server(MOVE_RPC_ID)]
            void MoveRpc() { }

            [Server(ATTACK_RPC_ID)] 
            void AttackRpc() { }

            [Server(HEAL_RPC_ID)]
            void HealRpc() { }
        }
        ```

---

### Implementation Examples

=== "Example 1 (NetworkBehaviour)"
    ???+ example "Example 1 (NetworkBehaviour)"
        ```csharp
        public class Player : NetworkBehaviour
        {
            private const byte MOVEMENT_RPC = 1;

            [Server(MOVEMENT_RPC)]
            void UpdateMovementServer(DataBuffer message)
            {
               // server reads the client's movement data and broadcasts it to all clients
                Vector3 position = message.Read<Vector3>();
                Quaternion rotation = message.Read<Quaternion>();

                // validate the data and send it to all clients

                Server.Rpc(MOVEMENT_RPC, position, rotation);
            }
    
            [Client(MOVEMENT_RPC)]
            void UpdateMovementClient(DataBuffer message)
            {
                // client receives the movement data from the server
                Vector3 position = message.Read<Vector3>();
                Quaternion rotation = message.Read<Quaternion>();

                // update the player's position and rotation
                transform.position = position;
                transform.rotation = rotation;
            }

            void Update()
            {
                // Client authoritatively sends movement data to the server
                if (IsLocalPlayer)
                {
                    Client.Rpc(MOVEMENT_RPC, transform.position, transform.rotation);
                }
            }
        }
        ```

=== "Example 2 (ServerBehaviour & ClientBehaviour)"
    ???+ example "Example 2 (ServerBehaviour & ClientBehaviour)"
        !!! warning "Script ID Configuration"
            Script IDs act as a bridge between client and server components, ensuring they can communicate properly. Each pair of corresponding client/server scripts must share the same ID in their Unity Inspector.

            **Key Points:**

            - The ID links matching client and server components
            - Must be unique across your project
            - Set in Unity Inspector for both scripts
            - Mismatched IDs will break communication

            ???+ example "Example"
                ```text
                LoginServer.cs    -> Script ID: 1  ✓
                LoginClient.cs    -> Script ID: 1  ✓

                PlayerServer.cs   -> Script ID: 2  ✓ 
                PlayerClient.cs   -> Script ID: 2  ✓
                ```
        ```csharp
        // Server-side class for handling login requests
        public class LoginServer : ServerBehaviour
        {
            private const byte LOGIN_RPC = 1;

            [Server(LOGIN_RPC)]
            void LoginServer(DataBuffer message, NetworkPeer peer)
            {
                string username = message.ReadString();
                string password = message.ReadString();

                // Server-side login logic

                // Send a response to the client
                Server.Rpc(LOGIN_RPC, peer, Target.SelfOnly);
            }
        }
    
        // Client-side class for handling login requests
        public class LoginClient : ClientBehaviour
        {
            private const byte LOGIN_RPC = 1;

            [Client(LOGIN_RPC)]
            void LoginClient()
            {
               print("Wow! you are logged in!");
            }

            void Update()
            {
                // Client sends a login request to the server
                if (Input.GetKeyDown(KeyCode.L))
                {
                    using var message = Rent();
                    message.WriteString("username");
                    message.WriteString("password");
                    Client.Rpc(LOGIN_RPC, message);
                }
            }
        }
        ```

=== "Example 3 (DualBehaviour)"
    ???+ example "Example 3 (DualBehaviour)"
        ```csharp
        public class Player : DualBehaviour
        {
            [Server(1)]
            void ExampleOnServer() // Signature 1
            {
               print("Wow! This works on the server!");
            }
    
            [Client(1)]
            void ExampleOnClient() // Signature 1
            {
               print("Wow! This works on the client!");
            }
        }
        ```

---

### How to Invoke a RPC

The `Client` and `Server` properties are part of the public API inherited from `NetworkBehaviour`, `ClientBehaviour`, `ServerBehaviour`, and `DualBehaviour`. 

They are designed to facilitate communication between client and server in the network environment. Each property enforces specific usage restrictions to ensure proper client-server interactions.

The network components provide two key properties for managing client-server communication:

**Client Property**

- **Purpose**: Enables clients to send RPCs to the server
- **Access**: Client-side only (throws exception if accessed on server)
- **Main Method**: `Rpc()` with multiple overloads
- **Example Usage**:
```csharp 
// On client:
using var someData = Rent();
someData.WriteString("Hello World!");

Client.Rpc(rpcId);              // Basic RPC
Client.Rpc(rpcId, someData);    // RPC with data
Client.Rpc(rpcId, someData, DeliveryMode.ReliableOrdered); // RPC with data and delivery mode
Client.Rpc(rpcId, someData, DeliveryMode.ReliableOrdered, 10); // RPC with data, delivery mode, and sequence channel
Client.Rpc(rpcId, transform.position, transform.rotation, new ClientOptions() {}); // RPC with unmanaged types
```

**Server Property**

- **Purpose**: Enables server to send RPCs to clients
- **Access**: Server-side only (throws exception if accessed on client) 
- **Main Method**: `Rpc()` with multiple overloads
- **Example Usage**:
```csharp
// On server:
using var data = Rent();
data.WriteString("Hello World!");

Server.Rpc(rpcId);                    // Broadcast to all(default)
Server.Rpc(rpcId, data, Target.All);  // Targeted broadcast
Server.Rpc(rpcId, data, Target.AllExceptSelf, DeliveryMode.ReliableOrdered); // Targeted broadcast with delivery mode
Server.Rpc(rpcId, data, Target.GroupOnly, DeliveryMode.ReliableOrdered, 10); // Targeted broadcast with delivery mode and sequence channel
Server.Rpc(rpcId, transform.position, transform.rotation, new ServerOptions() { 
    Target = Target.SelfOnly 
}); // RPC with unmanaged types
```

Both properties enforce proper client-server architecture by restricting access to the appropriate side. For detailed API information including all overloads, see the [API Reference](./index.md).

---

=== "Example 1 (NetworkBehaviour) - Send an RPC from the client to the server"
    ???+ example "Example 1 (NetworkBehaviour) - Send an RPC from the client to the server"
        ```csharp
        public class Player : NetworkBehaviour
        {
            void Update()
            {
               // Send an RPC from the client to the server
               if (Input.GetKeyUp(KeyCode.S) && IsLocalPlayer)
               {
                   Client.Rpc(1);
               }
            }
    
            [Server(1)]
            void Example() // Signature 1
            {
               print("Wow! This works on the server!");
            }
        }
        ```

=== "Example 2 (NetworkBehaviour) - Send an RPC from the server to the client"
    ???+ example "Example 2 (NetworkBehaviour) - Send an RPC from the server to the client"
        ```csharp
        public class Player : NetworkBehaviour
        {
            void Update()
            {
               // Send an RPC from the server to the client
               if (Input.GetKeyUp(KeyCode.A) && IsServer)
               {
                   Server.Rpc(1);
               }
            }
    
            [Client(1)]
            void Example() // Signature 1
            {
               print("Wow! This works on the client!");
            }
        }
        ```

`Rpc()` with ***arguments***:

!!! tip
    The `Client.Rpc()` and `Server.Rpc()` methods has 8 overloads and optional arguments. However, the overloads available can vary depending on the network base class used(i.e. `NetworkBehaviour`, `ClientBehaviour`, `ServerBehaviour`, and `DualBehaviour`). 
    
    For details on the available overloads, please refer to the [API Reference](./index.md).

=== "Client-Side"
    ???+ example "Client-Side"
        ```csharp
        public class Player : NetworkBehaviour
        {
            void Update()
            {
               // Send an RPC from the client to the server
               if (Input.GetKeyUp(KeyCode.S) && IsLocalPlayer)
               {
                   using DataBuffer message = Rent();
                   message.WriteString("Hello World!");
                   message.Write(123f);
                   Client.Rpc(1, message, DeliveryMode.ReliableOrdered);
               }
            }
    
            [Server(1)]
            void Example(DataBuffer message) // Signature 2
            {
               string str = message.ReadString();
               float num = message.Read<float>();
               Debug.Log(str + " " + num);
            }
        }
        ```

=== "Server-Side"
    ???+ example "Server-Side"
        ```csharp
        public class Player : NetworkBehaviour
        {
            void Update()
            {
               // Send an RPC from the server to the client
               if (Input.GetKeyUp(KeyCode.A) && IsServer)
               {
                   using DataBuffer message = Rent();
                   message.WriteString("Hello World!");
                   message.Write(123f);
                   Server.Rpc(1, message, Target.All, DeliveryMode.ReliableOrdered);
               }
            }
    
            [Client(1)]
            void Example(DataBuffer message) // Signature 2
            {
               string str = message.ReadString();
               float num = message.Read<float>();
               Debug.Log(str + " " + num);
            }
        }
        ```

!!! tip "Direct Value Transmission"
    RPCs support direct sending of primitive and unmanaged types without manual `DataBuffer` creation.

    **Supported Types**

    - Primitives (`int`, `float`, `bool`, etc)
    - Unity types (`Vector3`, `Quaternion`, etc)
    - Blittable structs
    
    **Examples**
    ```csharp
    // Client-side examples
    Client.Rpc(rpcId, 42);                     // Single int
    Client.Rpc(rpcId, true, 3.14f, 23.3d);           // Multiple primitives
    Client.Rpc(rpcId, transform.position, transform.rotation, Vector3.Zero, new ClientOptions() {});     // Unity type
    
    // Server-side examples with options
    Server.Rpc(rpcId, Vector3.zero, new ServerOptions {
        DeliveryMode = DeliveryMode.Unreliable
    });
    
    Server.Rpc(rpcId, transform.position, transform.rotation, 100f, new ServerOptions {
        Target = Target.AllExceptSelf
        DeliveryMode = DeliveryMode.Unreliable,
    });
    ```

    !!! warning "Type Restrictions"
        ❌ **Not Allowed Without a `DataBuffer`**:

        - Reference types
        - Classes
        - Arrays
        - Strings
    
        ✅ **Allowed**:

        - Primitive types
        - Unmanaged structs
        - Unity value types

---

## Network Variables

A `[NetworkVariable]` is a powerful attribute that automatically synchronizes state between server and clients without manual RPC implementation. When a network variable's value changes on the server, the framework automatically propagates these changes to all connected clients, ensuring state consistency across the network.

Key benefits:

- Automatic synchronization without manual networking code
- Significantly reduces boilerplate compared to RPCs
- Change detection and validation out of the box

This provides an efficient way to maintain synchronized game state with minimal code overhead.

!!! info "Network Variable Structure"
    <center>
    ``` mermaid
    graph LR
      Ref{Game Object<br>___Server Side___} --> | Health Change | A{RPC}
      A ---> | Health Update | B[Mike<br>___Client Side___]
      A ---> | Health Update | C[Ruan<br>___Client Side___]
    ```
    </center>

    The diagram illustrates how a **Network Variable** operates in a multiplayer environment:

    - A **server-side game object** modifies a variable (e.g., `Health`).
    - This change is processed by the **server**, which acts as the authoritative source.
    - The **server** then sends updates to all connected clients (e.g., Mike and Ruan), ensuring that each client reflects the latest value of the variable.
    - These updates allow all players to have a synchronized and consistent view of the variable's state, regardless of who initiated the change or their connection latency.

    This structure highlights the server's role in maintaining authority and consistency across the network.
---          

### Base Class Support

Network Variables support inheritance through base classes, allowing you to define shared networked state that derived classes can access and modify.

!!! warning "Base Class Naming Convention"
    When using Network Variables in base classes:

    - Base class names **must** end with the `Base` suffix
    - The suffix is required for proper code generation
    - Incorrect naming will prevent network synchronization

    ???+ example "Valid Base Class Names"
        ```csharp
        ✅ PlayerBase
        ✅ CharacterBase
        ✅ VehicleBase
        ❌ BasePlayer    // Incorrect - 'Base' must be suffix
        ❌ Player       // Missing 'Base' suffix
        ```

???+ example "Base Class Implementation"
    ```csharp
    public partial class CharacterBase : NetworkBehaviour 
    {
        [NetworkVariable]
        private float m_Health = 100f;  // Base class network variable
        
        protected virtual void OnHealthChanged(float prev, float next, bool isWriting)
        {
            Debug.Log($"Health changed from {prev} to {next}");
        }
    }
    
    public partial class Player : CharacterBase  // Inherits network variable
    {
        protected override void OnHealthChanged(float prev, float next, bool isWriting)
        {
            base.OnHealthChanged(prev, next, isWriting);
            UpdateHealthUI(next);  // Add custom behavior
        }
    }
    ```

Network Variables defined in base classes are automatically available to all derived classes, maintaining synchronization across the inheritance chain while allowing customization through virtual hooks.


!!! note
    Before proceeding, refer to the [Communication Structure](./index.md#communication-structure) and [Service Locator Pattern](./index.md#service-locator-pattern) pages for essential background information.

### How to Use

!!! warning "Network Variable Inspector"
    Network variables are automatically displayed in the Unity Inspector even without the `[SerializeField]` attribute. However, without this attribute they are read-only and not serialized.

    To make network variables both visible and editable in the Inspector:

    - Add both `[NetworkVariable]` and `[SerializeField]` attributes
    - This enables full serialization and editing capabilities
    - Without `[SerializeField]`, values reset on scene reload

    ???+ example
        ```csharp
        public partial class Player : NetworkBehaviour 
        {
            [NetworkVariable]
            [SerializeField] // Required for Inspector editing
            private float m_Health = 100f;

            [NetworkVariable] // Displayed, but Read-only in Inspector
            private float m_Stamina = 100f; 
        }
        ```

!!! warning "Network Variable Naming Requirements"
    **Field Naming Convention**

    Network variable fields must follow these rules:

    1. Fields must be prefixed with `m_`
    2. First letter after prefix must be capitalized
    3. Class must be marked as `partial`

    ✅ **Valid Examples:**
    ```csharp
    public partial class Player : NetworkBehaviour 
    {
        [NetworkVariable]
        private float m_Health = 100f;  // Correct prefix and capitalization
        
        [NetworkVariable]
        private Vector3 m_Position;      // Correct format
    }
    ```

    ❌ **Invalid Examples:**
    ```csharp
    public class Player : NetworkBehaviour // Missing partial
    {
        [NetworkVariable]
        private float health;      // Missing m_ prefix
        
        [NetworkVariable]
        private float m_mana;      // Lowercase after prefix
    }
    ```

    !!! note "Why partial?"
        The `partial` keyword is required because the source generator needs to extend the class with additional generated code for network variable functionality.

---

!!! note "Network Variable Source Generation"
    The `Omni Source Generator` automatically generates several elements for each `[NetworkVariable]`:

    **Properties**:

       - Public property for accessing the variable
       - Getter/setter with network synchronization
    
    **Hooks**:

       - `OnVariableChanged` method for value change detection
       - `partial void` hooks for custom change handling
       - Base class override hooks with `protected virtual` methods
    
    **Options**:

       - Variable-specific network options (e.g., `HealthOptions`)
       - Customizable delivery modes and target options
       - Serialization and synchronization settings
    
    **Methods**:

       - Manual sync methods (e.g., `SyncHealth()`)
       - Value validation methods
       - Networking utility methods

    Example of generated elements for a health variable:
    ```csharp 
    // Generated property
    public float Health { get; set; }
    
    // Generated hook
    partial void OnHealthChanged(float prev, float next, bool isWriting);
    virtual void OnHealthChanged(float prev, float next, bool isWriting);
    
    // Generated options
    public NetworkVariableOptions HealthOptions { get; set; }
    
    // Generated sync method
    public void SyncHealth(NetworkVariableOptions options);
    ```

#### Generated Properties

Generated properties in Omni are designed to automatically synchronize their values across the server and all connected clients each time the property is modified. This ensures that all instances of the property remain consistent throughout the networked environment, maintaining real-time accuracy.

!!! warning
    Omni does not perform checks to determine if the new value is different from the current value. Each time the property’s `setter` is invoked, the value is synchronized across the network, regardless of whether it has changed. This can lead to unnecessary network updates if the property is set to the same value repeatedly, so it is recommended to manage calls to the setter carefully to optimize performance.

???+ example "Automatically Synchronized"
    ```csharp
    public partial class Player : NetworkBehaviour
    {
        [NetworkVariable] 
        private float m_Health = 100f;
        [NetworkVariable] 
        private float m_Mana = 100f;

        void Update()
        {
           if(IsServer && Input.GetKeyUp(KeyCode.N))
           {
              // Automatically synchronized
              Health -= 10f;
              Mana += 10f;
           }
        }
    }
    ```

!!! tip
    You can modify the underlying **field** directly instead of the property if you don’t want automatic synchronization. To manually synchronize the modified field, simply call:
    
    - `SyncHealth(DefaultNetworkVariableOptions)`
    - `SyncMana(DefaultNetworkVariableOptions)`

    for immediate network updates.

    ???+ example "Manually Synchronized"
        ```csharp
        public partial class Player : NetworkBehaviour
        {
            [NetworkVariable] 
            private float m_Health = 100f;

            void Update()
            {
                if(IsServer && Input.GetKeyUp(KeyCode.N))
                {
                    // Manually synchronized
                    m_Health -= 10f;
                    SyncHealth();
                }
            }
        }
        ```

!!! warning
    If you modify a **field** immediately after instantiating a networked object or within `Awake()` or `Start()`, the variable will synchronize correctly. This is because, during object initialization, the server automatically sends updates for network variables to clients. However, if you modify the **property** instead of the **field** at these early stages, synchronization may fail. Property changes trigger an update message, but if the object has not yet been instantiated on the client side, the update will not be applied.

!!! bug
    Occasionally, generated code may not be recognized by the IDE’s IntelliSense (e.g., in Visual Studio). If this occurs, a simple restart of the IDE should resolve the issue.

##### Default Behaviour

!!! tip
    Use `DefaultNetworkVariableSettings` to adjust how network variables are transmitted across the network. This allows for configuring default behaviors for all network variables. For more specific control, you can use individual settings like `HealthOptions` and `ManaOptions` to customize the transmission behavior of specific variables.

    ???+ example
        ```csharp
        public partial class Player : NetworkBehaviour
        {
            [NetworkVariable] 
            private float m_Health = 100f;

            [NetworkVariable] 
            private float m_Mana = 100f;

            protected override void OnAwake()
            {
                // Change the default settings for all network variables
                DefaultNetworkVariableOptions = new()
                {
                    DeliveryMode = DeliveryMode.ReliableOrdered,
                    Target = Target.AllExceptSelf
                };

                // Change specific settings for specific network variables
                HealthOptions = new()
                {
                    DeliveryMode = DeliveryMode.ReliableOrdered,
                    Target = Target.All
                };

                ManaOptions = new()
                {
                    DeliveryMode = DeliveryMode.ReliableOrdered,
                    Target = Target.All
                };
            }
        }
        ```

---

#### Generated Methods

The `[NetworkVariable]` attribute will generate methods for each network variable, such as:

=== "Health Hook"
    ???+ example "Health Hook"
        ```csharp
        // Hook in the same script.
        partial void OnHealthChanged(float prevHealth, float nextHealth, bool isWriting)
        {
            // The isWriting parameter indicates whether the operation is writing the value to the network or reading it from the network.
        }
        
        // Hook in the derived class.
        protected override void OnBaseHealthChanged(float prevHealth, float nextHealth, bool isWriting)
        {
            // The isWriting parameter indicates whether the operation is writing the value to the network or reading it from the network.
        }
        ```

=== "Mana Hook"
    ???+ example "Mana Hook"
        ```csharp
        // Hook in the same script.
        partial void OnManaChanged(float prevMana, float nextMana, bool isWriting)
        {
            // The isWriting parameter indicates whether the operation is writing the value to the network or reading it from the network.
        }    

        // Hook in the derived class.
        protected override void OnBaseManaChanged(float prevMana, float nextMana, bool isWriting)
        {
            // The isWriting parameter indicates whether the operation is writing the value to the network or reading it from the network.
        }
        ```

- **`void SyncHealth(NetworkVariableOptions options);`**

Manually synchronizes the `m_Health` field, allowing control over when and how this field is updated across the network.

- **`void SyncMana(NetworkVariableOptions options);`** 

Manually synchronizes the `m_Mana` field.

---

## RouteX

**RouteX** is a simple simulation of `Express.js` and is one of the most useful features of the API. It can be easily used to request a route and receive a response from the server. Routes can also send responses to multiple clients beyond the one that originally requested the route.

### Registering Routes

1. Import the `RouteX` module with `using static Omni.Core.RouteX;` and `Omni` with `using Omni.Core;`
2. Register the routes on the `Awake` method or on the `Start` method, eg:

!!! Note
    `RouteX` supports both asynchronous and synchronous operations, providing flexibility for various use cases. All functions include asynchronous versions workflows. For additional overloads, detailed explanations, and further information on synchronous and asynchronous versions, consult the [`API Reference`]().

=== "Registering a Get Route"
    ???+ example
        ```csharp
        public class LoginControllerInServer : ServerBehaviour
        {
           protected override void OnAwake()
           {
              XServer.GetAsync("/login", (res) =>
              {
                  res.WriteString("Wow! You are logged in!");
                  res.Send();
              });

              XServer.GetAsync("/register", (res, peer) => // Peer argument is optional
              {
              	  res.WriteString("Ok! You are registered!");
              	  res.Send();
              });
           }
        }
        ```

=== "Registering a Post Route"
    ???+ example
        ```csharp
        public class LoginControllerInServer : ServerBehaviour
        {
           protected override void OnAwake()
           {
              XServer.PostAsync("/login", (req, res) =>
              {
                  // Read the username sent in the request
                  string username = req.ReadString();

                  // Send a response
                  res.WriteString("Wow! You are logged in as " + username);
                  res.Send();
              });

              XServer.PostAsync("/register", (req, res, peer) => // Peer argument is optional
              {
                  // Read the username sent in the request
                  string username = req.ReadString();

                  // Send a response
                  res.WriteString("Ok! You are registered!");
                  res.Send();
              });
           }
        }
        ```

### Requesting Routes

=== "Requesting a Get Route"
    ???+ example
        ```csharp
        public class LoginControllerInClient : ClientBehaviour
        {
           async void Update()
           {
              if (Input.GetKeyDown(KeyCode.R))
              {
                  using DataBuffer res = await XClient.GetAsync("/login");
                  string message = res.ReadString();
                  print(message);
              }
           }
        }
        ```  

=== "Requesting a Post Route"
    ???+ example
        ```csharp
        public class LoginControllerInClient : ClientBehaviour
        {
           async void Update()
           {
              if (Input.GetKeyDown(KeyCode.R))
              {
                  using DataBuffer res = await XClient.PostAsync("/login", req =>
                  {
                      req.WriteString("John Doe");
                  });

                  string message = res.ReadString();
                  print(message);
              }
           }
        }
        ```     

!!! info
    Omni provides the `HttpResponse` and `HttpResponse<T>` objects to streamline the process of sending responses. These objects allow you to include a status code, a message, and optionally, a payload (via the generic version). This approach offers a more organized and structured way to handle and send responses in your application.

=== "Registering a Post Route with HttpResponse"
    ???+ example
        ```csharp
        public class LoginControllerInServer : ServerBehaviour
        {
           protected override void OnAwake()
           {
              XServer.PostAsync("/login", (req, res) =>
              {
                  // Read the username sent in the request
                  string username = req.ReadString();

                  // Send a response with HttpResponse
                  res.WriteHttpResponse(new HttpResponse()
                  {
                  	 StatusCode = StatusCode.Success,
                  	 StatusMessage = $"Login successful, Hello {username}!",
                  });

                  res.Send();
              });

              XServer.PostAsync("/getinfo", (req, res) =>
              {
                  // Read the username sent in the request
                  string username = req.ReadString();

                  // Send a response with HttpResponse and payload
                  res.WriteHttpResponse(new HttpResponse<Player>()
                  {
                  	 StatusCode = StatusCode.Success,
                  	 StatusMessage = $"Login successful, Hello {username}!",
                     Result = new Player()
                  });

                  res.Send();
              });
           }
        }
        ```

=== "Requesting a Post Route with HttpResponse"
    ???+ example
        ```csharp
        public class LoginControllerInClient : ClientBehaviour
        {
           async void Update()
           {
              if (Input.GetKeyDown(KeyCode.R))
              {
                  using DataBuffer res = await XClient.PostAsync("/login", req =>
                  {
                      req.WriteString("John Doe");
                  });

                  var response = res.ReadHttpResponse();
                  print(response.StatusMessage);
              }

              if (Input.GetKeyDown(KeyCode.G))
              {
                  using DataBuffer res = await XClient.PostAsync("/getinfo", req =>
                  {
                      req.WriteString("John Doe");
                  });

                  var response = res.ReadHttpResponse<Player>();
	              print("Player name: " + response.Result.Name);
              }
           }
        }
        ``` 

For more details, refer to the [API Reference](#api-reference).

---

## Serialization and Deserialization

Omni supports serialization of a wide range of data types, including primitives, complex classes, structs, dictionaries, and more, providing unmatched flexibility for networked data structures. Omni offers two serialization methods: JSON-based serialization for readability and compatibility, and binary-based serialization for optimized performance and minimized data size.

With Omni, **everything is serializable**. All network operations utilize the `DataBuffer` object, a dedicated data buffer that efficiently handles data preparation and transmission across the network, ensuring seamless and effective communication.

!!! info
    The `DataBuffer` is the core of all Omni operations. It is used universally across RPCs, RouteX, custom messages, and other network features. Understanding how to manage and utilize `DataBuffer` is essential for working effectively with Omni.

!!! danger
    As a binary serializer, `DataBuffer` requires that the order of reading matches the order of writing precisely. Any discrepancy in the read/write sequence can lead to corrupted or unexpected data. Developers should ensure consistency and adherence to the defined structure when serializing and deserializing data with `DataBuffer`.

!!! info
    The `DataBuffer` functions similarly to a combination of [`MemoryStream`](https://learn.microsoft.com/en-us/dotnet/api/system.io.memorystream?view=net-8.0) and [`BinaryWriter`](https://learn.microsoft.com/en-us/dotnet/api/system.io.binarywriter?view=net-8.0) and [`BinaryReader`](https://learn.microsoft.com/en-us/dotnet/api/system.io.binaryreader?view=net-8.0). It includes comparable properties and features, such as `Position`, enabling developers to efficiently manage and navigate the buffer while performing read and write operations.

---

### Primitives

Omni’s `DataBuffer` provides efficient support for primitive types, allowing direct serialization of commonly used data types such as integers, floats, and booleans. This simplifies network data handling by enabling fast read and write operations for foundational data types.

Using these primitives, Omni ensures minimal overhead in data serialization, making it suitable for high-performance networking where lightweight data handling is essential. Primitive types can be written to or read from the `DataBuffer` in a straightforward manner, supporting rapid data transmission across client-server boundaries.

=== "Writing Primitives"
    ???+ example "Writing Primitives"
        ```csharp
        void Example()
        {
           DataBuffer message = new DataBuffer();
           message.Write(10); // Writes an integer value to the buffer
           message.Write(3.14f); // Writes a floating-point value to the buffer
           message.Write(true); // Writes a boolean value to the buffer
        }
        ```

=== "Reading Primitives"
    ???+ example "Reading Primitives"
        ```csharp
        void Example()
        {
           DataBuffer message = GetHypotheticalValidDataBuffer();
           int num = message.Read<int>(); // Reads an integer value from the buffer
           float f = message.Read<float>(); // Reads a floating-point value from the buffer
           bool b = message.Read<bool>(); // Reads a boolean value from the buffer
        }
        ```

---

### Complex Types

Omni supports the serialization of complex types using [`Newtonsoft.JSON`](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) or [`MemoryPack`](https://github.com/Cysharp/MemoryPack). For objects and data structures that go beyond primitive types, JSON serialization provides a readable, flexible format ideal for compatibility with third-party systems, while `MemoryPack` enables efficient binary serialization for high-performance network transfers.

Using these serialization methods, Omni can seamlessly handle complex data types, such as custom `structs`, `classes`, `dictionaries`, and `nested structures`, ensuring that all necessary data is transmitted effectively and accurately across the network.

=== "JSON Serialization"
    ???+ example "JSON Serialization"
        ```csharp
        public class Player
        {
            public string name;
            public int score;
            public Dictionary<string, int> inventory;
        }

        void Example()
        {
           Player player = new Player();
   
           // Serialize the player object
           DataBuffer message = new DataBuffer();
           message.WriteAsJson(player);
        }
        ```

=== "MemoryPack Serialization"
    ???+ example "MemoryPack Serialization"
        ```csharp
        [MemoryPackable]
        public partial class Player
        {
            public string name;
            public int score;
            public Dictionary<string, int> inventory;
        }

        void Example()
        {
           Player player = new Player();
   
           // Serialize the player object
           DataBuffer message = new DataBuffer();
           message.WriteAsBinary(player);
        }
        ```

=== "JSON Deserialization"
    ???+ example "JSON Deserialization"
        ```csharp
        public class Player
        {
            public string name;
            public int score;
            public Dictionary<string, int> inventory;
        }

        void Example()
        {
           DataBuffer message = GetHypotheticalValidDataBuffer();
           Player player = message.ReadAsJson<Player>(); // Deserializes to a Player object
        }
        ```

=== "MemoryPack Deserialization"
    ???+ example "MemoryPack Deserialization"
        ```csharp
        [MemoryPackable]
        public partial class Player
        {
            public string name;
            public int score;
            public Dictionary<string, int> inventory;
        }

        void Example()
        {
           DataBuffer message = GetHypotheticalValidDataBuffer();
           Player player = message.ReadAsBinary<Player>(); // Deserializes to a Player object
        }
        ```

!!! note
    See the [`Newtonsoft.JSON`](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) or [`MemoryPack`](https://github.com/Cysharp/MemoryPack) documentation for more information about using **annotation attributes** to customize serialization and deserialization.

!!! info
    When sending a `DataBuffer`, you will always receive a `DataBuffer` in response; it is not possible to send and receive data in any other way without using a `DataBuffer`, as all operations utilize it internally. **You must also ensure that the reading and writing occur in the same order.**

---

### Compression

The `DataBuffer` object in Omni supports efficient data compression, utilizing the `Brotli` and `LZ4` algorithms. These algorithms are designed to optimize network performance by reducing data size without significant overhead, ensuring faster transmission and lower bandwidth usage.

- **Brotli**: A highly efficient compression algorithm ideal for scenarios where maximum compression is needed, offering significant size reduction for complex or large datasets.
- **LZ4**: Focused on speed, `LZ4` provides fast compression and decompression, making it suitable for real-time applications that prioritize performance over compression ratio.

With these options, Omni allows developers to tailor data compression to their specific needs, balancing speed and efficiency for various network scenarios.

=== "Compression"
    ???+ example "Compression"
        ```csharp
        void Example()
        {
           DataBuffer message = new DataBuffer();
           message.WriteAsJson(GetHypotheticalLargePlayerObject());

           // Compress the data
           message.CompressRaw(); // Compress the current buffer
        }
        ```

=== "Decompression"
    ???+ example "Decompression"
        ```csharp
        void Example()
        {
           DataBuffer message = GetHypotheticalCompressedDataBuffer();

           // Decompress the data
           message.DecompressRaw(); // Decompress the current buffer

           // Read the decompressed data
           Player player = message.ReadAsJson<Player>();
        }
        ```

---

### Cryptography

Omni employs `AES` encryption to secure data buffers, ensuring that sensitive information remains protected during network transmission. The cryptographic system is designed with flexibility and security in mind, offering both peer-specific and global encryption keys to handle various scenarios.

Peer-Specific Encryption

Each `peer` in the network is assigned its own unique encryption key. When a client (e.g., Client A) sends a message using its key, only that client can decrypt the message. This ensures a high level of security, as no other client can access the encrypted data. Peer-specific encryption is ideal for situations where private communication or data integrity is paramount.

!!! info
    Encryption keys are exchanged between the client and server using `RSA`, a robust public-key cryptography algorithm. This ensures that the `AES` keys used for data encryption remain secure during transmission, as only the intended recipient can decrypt the exchanged keys. By combining `RSA` for key exchange with `AES` for data encryption, Omni provides a highly secure and efficient cryptographic system for multiplayer environments.

Global Server Key

In addition to peer-specific keys, Omni provides a global server encryption key. Unlike peer-specific keys, the global key can be used to encrypt and decrypt any data, including messages originating from other clients. This global key is managed by the server and allows for seamless handling of shared data, such as broadcasted messages or server-wide updates. It provides a flexible option for scenarios where universal decryption is required without compromising security.

Key Features

- **AES Encryption**: Omni uses the `Advanced Encryption Standard (AES)` to ensure robust protection against unauthorized access.
- **Peer-Specific Keys**: Restrict decryption to the originating peer, enhancing data privacy.
- **Global Server Key**: Enable decryption of any data within the network, facilitating shared communication and server-driven operations.
- **Flexibility**: The dual-key system allows developers to tailor encryption strategies to the needs of their application, balancing security and convenience.

Omni's cryptography framework ensures that all data transmitted across the network is secure, whether it's private peer-to-peer communication or broadcasted messages. By combining peer-specific encryption with a global server key, Omni provides a powerful and flexible system for managing encrypted data in multiplayer environments.

=== "Encryption"
    ???+ example "Encryption"
        ```csharp
        void Example()
        {
           DataBuffer message = new DataBuffer();
           message.WriteAsJson(GetHypotheticalPlayerObject());

           // Encrypt the data
           message.EncryptRaw(NetworkManager.SharedPeer); // Use `ServerPeer` - Global Encryption Key

           // e.g. Encrypt the data with a peer-specific key in client side...
           // message.EncryptRaw(NetworkManager.LocalPeer);
        }
        ```
 
=== "Decryption"
    ???+ example "Decryption"
        ```csharp
        void Example()
        {
           DataBuffer message = GetHypotheticalEncryptedDataBuffer();

           // Decrypt the data
           message.DecryptRaw(NetworkManager.SharedPeer); // Use `ServerPeer` - Global Encryption Key

           // e.g. Decrypt the data with a peer-specific key in server side...
           // message.DecryptRaw(peer);

           // Read the decrypted data
           Player player = message.ReadAsJson<Player>();
        }
        ```

See the [`API Reference`]() for more information about the `DataBuffer` and its usage.

---

## IMessage Interface

This interface, `IMessage`, can be implemented to customize the serialization and deserialization of a type when used within an RPC or `NetworkVariable`. By implementing `IMessage`, you define how data is written to and read from a `DataBuffer`, enabling greater control over data structure and format.

The `IMessageWithPeer` interface extends `IMessage` to include additional properties, such as `SharedPeer` and `IsServer`, which are useful for managing encryption and authentication in networked communications. This extension provides enhanced flexibility for handling secure and authenticated messaging between server and client.

???+ example
    ```csharp
       public class PlayerStruct : IMessage
       {
       	  private string m_Name;
       	  private Vector3 m_Position;
       	  private int m_Health;
       	  private int m_Mana;

       	  public void Serialize(DataBuffer writer)
       	  {
       	  	writer.WriteString(m_Name);
       	  	writer.Write(m_Position);
       	  	writer.Write(m_Health);
       	  	writer.Write(m_Mana);
       	  }
         
       	  public void Deserialize(DataBuffer reader)
       	  {
       	  	m_Name = reader.ReadString();
       	  	m_Position = reader.Read<Vector3>();
       	  	m_Health = reader.Read<int>();
       	  	m_Mana = reader.Read<int>();
       	  }
       }
    ```

=== "Network Variable"
    ???+ example "Network Variable"
        ```csharp
           public partial class Player : NetworkBehaviour
           {
           	  [NetworkVariable] 
           	  private PlayerStruct m_PlayerStruct = new PlayerStruct();
           }
        ```

=== "RPC"
    ???+ example "RPC"
        ```csharp
           public class Player : NetworkBehaviour
           {
               private PlayerStruct m_PlayerStruct = new PlayerStruct();
    
               void Update()
               {
                  if(IsServer)
                  {
                    // Send an RPC from the server to the client
                    Server.Rpc(1, m_PlayerStruct, new()
                    {
                    	DeliveryMode = DeliveryMode.Unreliable
                    });
                  }
               }
    
               [Client(1)]
               void Example(DataBuffer message) // Signature 2
               {
               	  m_PlayerStruct = message.Deserialize<PlayerStruct>();
    
                   // Alternative:
                  // Populate an existing object
                  // message.Populate(m_PlayerStruct);
               }
           }
        ```
