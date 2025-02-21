# Overview

!!! tip
    If you plan to translate this documentation, it is recommended to use **Google Chrome** to avoid translation bugs and ensure accurate rendering of the content.

**Omni Networking** is a powerful multiplayer networking solution for Unity games, offering high performance and extensive features. Key capabilities include:

- **Efficient Data Management**: Advanced synchronization and state management
- **Flexible Communication**: Multiple transport protocols and messaging systems
- **Robust Security**: Built-in encryption and authentication
- **Optimized Performance**: Binary serialization and data compression
- **Developer-Friendly**: Easy-to-use API and comprehensive tooling

Whether you're creating competitive multiplayer games, co-op experiences, or massive multiplayer worlds, Omni provides the essential networking infrastructure you need.

---

**Data Storage**

| Feature | Description |
|---------|-------------|
| **Database Management** | Supports multiple relational databases through a built-in ORM (Object-Relational Mapping) system, enabling scalable, efficient, and secure data operations tailored for multiplayer environments. Compatible databases include:<br><br>• Microsoft SQL Server<br>• MySQL<br>• MariaDB<br>• PostgreSQL<br>• Oracle<br>• SQLite<br>• Firebird<br><br>Features advanced querying capabilities, connection pooling, transaction management, and automatic migration support. The ORM provides a type-safe way to work with database entities while maintaining optimal performance for multiplayer scenarios. |

**Network Communication**

| Feature | Description |
|---------|-------------|
| **Network Variables** | Provides automated property synchronization between server and clients, eliminating the need for manual message handling. Ensures real-time data consistency across all networked instances with minimal code overhead. |
| **RPC (Remote Procedure Calls)** | Facilitates direct method invocation between clients and server, supporting both reliable and unreliable transmission modes. Enables synchronized execution of functions across the network with automatic parameter serialization. |
| **gRPC (Global RPC)** | Implements network-wide event broadcasting system for executing procedures across all connected clients simultaneously. Ideal for game-wide announcements, state updates, and synchronized events. |
| **Route Management** | Express.js-inspired routing system for organizing and handling network communications. Supports route parameters, middleware, and structured request/response patterns, enabling clean and maintainable network code architecture. |
| **Custom Messages** | Enables creation of custom message types for specialized network communication, supporting user-defined data structures and transmission formats. Offers flexibility for implementing unique network features and interactions. |

**Data Serialization & Compression**

| Feature | Description |
|---------|-------------|
| **Binary Serialization** | Utilizes the `MemoryPack` library for efficient binary data serialization, minimizing data size for faster transmission and optimized network performance. |
| **JSON Serialization** | Uses the `Json.NET (Newtonsoft)` library for flexible and reliable JSON data serialization, ensuring compatibility with third-party services. |
| **Compression** | Leverages `Brotli` and `LZ4` algorithms to reduce data size, optimizing bandwidth usage and improving transmission speed. |

**Infrastructure & Security**

| Feature | Description |
|---------|-------------|
| **Port Forwarding** | Facilitates NAT traversal and opens network ports using protocols like `PMP` and `UPnP`, ensuring seamless connectivity for multiplayer sessions. |
| **Cryptography** | Utilizes `AES` and `RSA` encryption algorithms to secure data transmission, ensuring protection of sensitive information across the network. |
| **Service Locator** | Implements a centralized registry for managing dependencies and services across networked objects, enabling efficient access to shared resources and components. Supports global and local service locators for flexible dependency management. |
| **Web Sockets** | Implements a lightweight and efficient WebSocket server for real-time communication between clients and server, supporting bidirectional data exchange and event-driven messaging. |
| **Http Server** | Implements a lightweight HTTP server for handling web requests, enabling web-based services and integrations within the multiplayer environment. Supports RESTful API design(Express.js-inspired) and custom route handling for versatile network communication. |

---

`Omni` requires Unity version `2021.3 or higher`, as it leverages the latest .NET Standard 2.1+ APIs to deliver optimal performance.

Compatibility Table:

| Unity Version           | Status               |
|------------------------|---------------------|
| Unity 2021.3 *(LTS)*   | ✅ Fully Supported  |
| Unity 2022.3 *(LTS)*   | ✅ Fully Supported  |
| Unity 2023.2 *(Beta)*   | ✅ Fully Supported  |
| Unity 6000.0 *(LTS)*   | ✅ Fully Supported |

!!! warning
    Using versions not listed in the compatibility table may result in unexpected behavior or functionality issues. For the most reliable experience, please check the [Releases](https://github.com/RuanCardoso/Omni-Networking-for-Unity/releases) page for up-to-date version compatibility information.

---

## Installation via Package Manager

### Requirements
- Unity 2021.3 or higher
- .NET Standard 2.1+ API Compatibility

### Quick Install
1. Open Unity Package Manager (Window > Package Manager)
2. Click the `+` button in the top-left corner
3. Select "Add package from git URL"
4. Paste: `https://github.com/RuanCardoso/Omni-Networking-for-Unity.git`
5. Click "Add"

??? info "📦 Included Dependencies"

    **Core Libraries**

    - ✨ [Newtonsoft Json](https://www.newtonsoft.com/json) - Industry-standard JSON framework for .NET, providing robust serialization and deserialization capabilities
    - 🚀 [MemoryPack](https://github.com/Cysharp/MemoryPack) - High-performance zero-allocation binary serializer optimized for gaming and real-time applications
    - ⚡ [UniTask](https://github.com/Cysharp/UniTask) - Zero allocation async/await solution for Unity, delivering superior performance over standard coroutines
    - 🎯 [DOTween](http://dotween.demigiant.com/) - Fast and efficient animation engine for Unity with a fluent API and extensive feature set
    
    **Database Connectors**

    - 📁 [SQLite](https://www.sqlite.org/) - Self-contained, serverless, zero-configuration database engine perfect for local data storage
    - 🔋 [MySqlConnector](https://mysqlconnector.net/) - High-performance, asynchronous MySQL database connector with connection pooling
    - 🐘 [Npgsql](https://www.npgsql.org/) - Open-source PostgreSQL database connector with full async support and advanced features
    - 📊 [SQLKata](https://sqlkata.com/) - Elegant SQL query builder with support for multiple databases and complex queries
    - and others depending on the database you choose.
    
    **Networking**

    - 🌐 [LiteNetLib](https://github.com/RevenantX/LiteNetLib) - Lightweight and fast UDP networking library with reliability, ordering, and connection management
    - 🔄 [kcp2k](https://github.com/vis2k/kcp2k) - Reliable UDP communication protocol implementation offering low latency and congestion control
    - 🔒 [BCrypt.Net](https://github.com/BcryptNet/bcrypt.net) - Modern cryptographic hashing for passwords with salt generation and verification
    - 🚪 [Open.NAT](https://github.com/lontivero/Open.NAT) - Port forwarding library supporting both UPnP and NAT-PMP for seamless multiplayer connectivity
    
    **Development Tools**

    - 🛠️ [Humanizer](https://github.com/Humanizr/Humanizer) - Developer utility for manipulating and formatting strings, enums, dates, times, and more
    - 🎨 [TriiInspector](https://github.com/codewriter-packages/Tri-Inspector) -  Advanced Unity inspector extension providing enhanced editor customization
    - 🔄 [ParrelSync](https://github.com/VeriorPies/ParrelSync) - Unity editor extension for testing multiplayer gameplay with multiple game instances locally
    - ⚡ [Dapper](https://github.com/DapperLib/Dapper) - High-performance micro-ORM supporting SQL queries with strong typing and object mapping
    - and others...

---

## Setup

!!! note "🚀 Introduction"

    **Build Defines**

    Omni automatically configures the following build defines after package import:
    
    | Define | Description |
    |--------|-------------|
    | `OMNI_RELEASE` | Optimizes code for production. Disables logging, debugging features. Used when building the final game or server. |
    | `OMNI_DEBUG` | Enables development detailed error information and runtime checks. Default for Unity Editor and Build. |
    | `OMNI_SERVER` | Indicates a dedicated server build. Removes client-specific code, UI elements, and graphics rendering. |

    **Common Imports**

    Add these using directives to your scripts:
    ```csharp
    using Omni.Core;      // Core functionality
    using Omni.Inspector; // (Optional) - Unity inspector extensions
    using Omni.Core.Web;  // (Optional) - Web and networking features
    ```

    **Conditional Compilation**
    ```csharp
    // Debug vs Release mode
    #if OMNI_DEBUG
        Debug.Log("Development build");
    #else
        Debug.Log("Release build");
    #endif
    
    // Client vs Server code
    #if OMNI_SERVER
        // Server-only code
        Debug.Log("Running on dedicated server");
    #else
        // Client-only code
        Debug.Log("Running on client");
    #endif
    ```


1. Go to the Unity Navigation Bar and select `Omni Networking`.
2. Click the `Setup` menu item.

A game object named `Network Manager` will be created in the scene. This object is responsible for the connection and contains all the network settings.

!!! warning "Network Manager Requirements"
    **Required Configuration**

    - The `Network Manager` object **must** exist in your scene
    - Do not destroy this object during runtime
    - Do not rename this object
    - Keep the default name: `Network Manager`

    **Common Issues**
    
    If the `Network Manager` is missing or renamed:

    - Network connections will fail
    - Multiplayer features won't work
    - Runtime errors will occur

!!! tip "Network Manager Structure(***Optional***)"
    **Object Hierarchy**

    ```
    Network Manager (Main Object)
    ├── Client (Child Object)
    └── Server (Child Object)
    ```

    **How It Works**

    - The `Network Manager` comes with two child objects: `Client` and `Server`
    - You can attach your networking scripts to these objects(***Optional***)
    - During build:
        - Client build: Server scripts are removed
        - Server build: Client scripts are removed

    !!! warning
        This system only **removes objects** from the scene. The code still exists in the build.
        
        For complete code removal, use conditional compilation:
        ```csharp
        #if OMNI_SERVER
            // This code will be completely removed from client builds
            void ServerOnlyMethod() {
                // Server-specific code here
            }
        #endif
        ```

3. Select the `Network Manager` object in the scene to view the network settings.

| Option        | Description                           |
| ------------- | ------------------------------------- |
| `Current Version` | Displays the installed Omni version. Important for compatibility checks and troubleshooting. |
| `Public IPv4` | Your device's public IPv4 address. Updates automatically but can be refreshed manually |
| `Public IPv6` | Your device's public IPv6 address (if available). Used for modern network configurations. |
| `GUID` | Unique 128-bit identifier used for network authentication. Can be regenerated through context menu |

!!! bug
    If the `Public IP` field displays an incorrect IP address, click the context menu of the `Network Manager` script and select **Force Get Public Ip** to update the field with the correct IP address. The correct IP address is essential for server identification and connection.

!!! warning
    If the `GUID` between the client and server does not match, the connection will be refused. Ensure the `GUID` is correctly set in the `Network Manager` object to establish a successful connection. To update the `GUID`, click the context menu of the `Network Manager` script and select **Generate GUID**.

### Modules

| Modules          | Description                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------------- |
| `Tick Module`    | Allows the use of a tick-based system for sending messages and other tick-based operations. |
| `Sntp Module`    | Provides a high-precision synchronized clock between all clients and the server.            |

### Connection Settings

| Option        | Description                                                           |
| ------------- | --------------------------------------------------------------------- |
| `Server Port` | The port number on which the server listens for incoming connections. |
| `Client Port` | The port number on which the client listens for incoming connections. |

| Option         | Description                                                                                 |
| -------------- | ------------------------------------------------------------------------------------------- |
| `Host Address` | A list of IP addresses that the client can connect to, the first address is used to connect. |
| `Port`         | The port number on which the server is listening and which will be used for the connection. |

### Configuration Options

=== "Basic"
    | Option | Description |
    |--------|-------------|       
    | `Auto Start Client` | When enabled, client automatically connects to server on scene load (Default: true) | 
    | `Auto Start Server` | When enabled, server automatically starts hosting on scene load (Default: true) |
    | `Tick Rate` | Server update frequency in Hz. Higher values increase precision but consume more CPU (Default: 15) |
    | `Use UTF-8 Encoding` | Uses UTF8 for string encoding. Enable for non-ASCII text support (Default: false) |
    | `Lock Client Fps` | Limits client frame rate to reduce CPU/GPU load. Set to 0 for unlimited (Default: 60) |

=== "Advanced"
    | Option | Description |
    |--------|-------------|
    | `Pool Capacity` | Maximum size in bytes for each network message buffer. Larger values consume more memory (Default: 32768) |
    | `Pool Size` | Number of pre-allocated network buffers. Increase for high-traffic scenarios (Default: 32) |    
    | `Use Unaligned Memory` | Enables faster memory access on supported platforms. May cause issues on mobile (Default: false) |
    | `Enable Bandwidth Optimization` | Enable bandwidth optimization for data transmission |
    | `Run In Background` | Keep game running when window loses focus. Essential for server hosting (Default: true) |

=== "HTTP Server"
    | Option | Description |
    |--------|-------------|
    | `Enable Http Server` | Activates REST API endpoint for external service integration (Default: false) |
    | `Enable Http Ssl` | Enables HTTPS for secure API communication. Requires valid SSL certificate (Default: false) |
    | `Http Server Port` | Port number for HTTP/HTTPS server. Common values: 80 (HTTP), 443 (HTTPS) (Default: 80) |

### Permissions

| Option                        | Description                                                                                      |
| ----------------------------- | ------------------------------------------------------------------------------------------------ |
| `Allow NetVar's From Client`  | Determines whether client-side changes to network variables are permitted, allowing clients to modify networked variables directly. |
| `Allow Across Group Message`  | Allows messages to be sent across different network groups, enabling communication between distinct groups in the network. |

---

### Registered Prefabs

This list is used to automatically instantiate network objects. When a network object is instantiated by name or indexer, the object will be looked up in this list and instantiated automatically. Remember, manual instantiation is also available, and using this list is not required.

---

### Transporter Settings

The **Transporter Settings** section allows you to configure various network transport parameters, including disconnection timeout, network event processing per frame, lag simulation, channel setup, IPv6 support, max connections, ping intervals, and more. Available options may vary based on the selected transporter.

Currently, three transporters are supported: **Lite Transporter**, **KCP Transporter**, and **Web Socket Transporter**. Each transporter offers distinct features and capabilities, catering to different network requirements and scenarios.

| Transporter            | ReliableOrdered | Unreliable | ReliableUnordered | Sequenced | ReliableSequenced | Browser Compatibility |
|------------------------|-----------------|------------|-------------------|-----------|-------------------|------------------------|
| Lite Transporter       | ✅              | ✅         | ✅                | ✅        | ✅                | ❌                     |
| Kcp Transporter        | ✅              | ✅         | ❌                | ❌        | ❌                | ❌                     |
| Web Socket Transporter | ✅              | ❌         | ❌                | ❌        | ❌                | ✅      |

!!! danger
    The **KCP Transporter** and **Web Socket Transporter** is currently ***experimental*** and may contain unresolved issues. Use it with caution and consider thoroughly testing for stability in your specific use case.

!!! note
    By default, **Omni** utilizes the **Lite Transporter** for network operations. To switch to a different transporter, follow these steps:
 
    1. **Remove the Lite Transporter**: In your scene, locate the `Network Manager` object. Select it, and remove the `Lite Transporter` component from this object.
    2. **Add the Desired Transporter**: Once the Lite Transporter is removed, add the component of your preferred transporter to the `Network Manager` object.
    
    This configuration enables you to tailor network transport settings to suit the specific requirements of your project, ensuring optimal compatibility and performance.

!!! warning
    Some properties or functions may be unavailable for certain transporters. If an incompatible option is used, an error message will appear to inform you of the mismatch.

For detailed information on each transporter and their specific features, consult the respective documentation:

- [LiteNetLib Documentation](https://github.com/RevenantX/LiteNetLib)
- [KCP Transporter (kcp2k) Documentation](https://github.com/MirrorNetworking/kcp2k)

---

## Known Issues

!!! bug "IP Configuration"
    **The Issue**

    - When sharing your project, the `Public IP` field may show incorrect values
    - This happens because IP addresses are stored in the scene file
    
    **How to Fix**

    1. Select the `Network Manager` in your scene
    2. Right-click on the component
    3. Choose `Force Get Public IP`
    
    **Note**

    - Your correct IP will be automatically fetched
    - This step is required for proper network connections
    - Remember to update IP when changing networks

!!! bug "Installation Troubleshooting"
    **Common Issues**

    - Unity freezing during installation
    - Missing macro definitions
    - Package import errors
    
    **How to Fix**

    1. Show hidden files in Windows:
        - Open File Explorer
        - View > Show > Hidden items
    
    2. Delete macro file:
        - Go to: `Assets/Plugins/OmniNetworking`
        - Find: `omni_macros` file
        - Delete it
    
    3. Restart Unity:
        - Close Unity completely
        - Reopen your project
        - Package will regenerate macros

---

## Communication Structure

The Omni framework is structured around four foundational classes designed for general communication. These classes implement methods and properties that simplify and expedite multiplayer functionalities, making the process both efficient and straightforward. Additionally, a "low-level" class is available for advanced communication, which should be utilized in contexts where restrictions or limitations apply, offering finer control for specialized cases.

The Omni framework currently utilizes four base classes, each designed for different networking roles within the multiplayer structure. These classes include:

### Base Classes Overview
| Class | Identity Required | Usage |
|-------|------------------|--------|
| `NetworkBehaviour` | Yes | For objects needing network identity (players, items) |
| `ServerBehaviour` | No | Server-only logic (game state, matchmaking) |
| `ClientBehaviour` | No | Client-only logic (UI, input handling, requests to server) |
| `DualBehaviour` | No | Combined client/server logic |

Each class serves a unique purpose in managing networked objects and handling server or client logic.

---

#### Detailed Description

=== "NetworkBehaviour"
    - **Purpose**: Manage networked objects with unique identities
    - **Common Uses**: 
        - Player characters
        - Interactable items
        - Spawnable objects
    - **Key Features**:
        - Automatic synchronization
        - Network identity management
        - Object ownership
    
    !!! warning "Object Registration"
        **Requirements**

        - Objects must be instantiated to work correctly with `NetworkBehaviour`

=== "ServerBehaviour"
    - **Purpose**: Handle server-side logic
    - **Common Uses**: 
        - Game state management
        - Player authentication
        - Routing and messaging
        - Callbacks and events
    - **Key Features**:
        - Server-only execution
        - No network identity required
        - Performance optimized

=== "ClientBehaviour"
    - **Purpose**: Handle client-side logic
    - **Common Uses**: 
        - Requests to server-side logic and response handling
        - UI management
        - Callbacks and events
    - **Key Features**:
        - Client-only execution
        - No network identity required
        - Local processing

=== "DualBehaviour"
    - **Purpose**: Combined client/server logic
    - **Common Uses**: 
        - Shared game systems
        - Unified managers
    - **Key Features**:
        - Both client/server code
        - Conditional execution
        - Code organization

This structured approach with these base classes simplifies multiplayer development, enabling clear separation of client and server responsibilities while providing flexibility for objects with and without identities.

!!! warning "Behaviour Class Limitations"
    **Scene vs Spawned Objects**

    - `ClientBehaviour`, `ServerBehaviour`, and `DualBehaviour`:
        - ✅ Can be used on scene objects
        - ❌ Cannot be spawned at runtime
        - ❌ No NetworkIdentity support
    
    **Example Usage**

    ```csharp
    // Correct: Scene object
    public class GameManager : ServerBehaviour { }
    public class LoginManager : ClientBehaviour { }
    
    // Wrong: Spawnable object
    public class Player : ClientBehaviour { } // Won't work!!!!!
    // Correct alternative:
    public class Player : NetworkBehaviour { } // Works as expected
    ```

!!! warning "Network Features Requirements"
    **Inheritance Required**

    All scripts using network features must inherit from any:

    - `NetworkBehaviour`
    - `ServerBehaviour`
    - `ClientBehaviour`
    - `DualBehaviour`

---

### Network Identity

The `NetworkIdentity` component is at the heart of the Omni networking high-level API. It controls a game object's unique identity on the network, and it uses that identity to make the networking system aware of the game object.

The `NetworkIdentity` component is essential for network-aware GameObjects in Omni. It:

- Assigns unique identifiers to objects
- Enables network synchronization
- Manages object ownership
- Handles object spawning/despawning

!!! warning "NetworkIdentity Nesting Rules"
    **Hierarchy Rules**

    - Only parent objects can have `NetworkIdentity`
    - Child objects cannot have `NetworkIdentity`
    - Children access parent's `NetworkIdentity` via `Identity` property
    
    **Example Structure**

    ```
    Player (NetworkIdentity ✅)
    ├── Weapon (NetworkIdentity ❌)
    └── Inventory (NetworkIdentity ❌)
    ```

    **Code Example**

    ```csharp
    // Parent object
    public class Player : NetworkBehaviour {
        // Has NetworkIdentity automatically
    }

    // Child object
    public class Weapon : NetworkBehaviour {
        void Start() {
            // Access parent's NetworkIdentity
            var parentIdentity = Identity;
        }
    }
    ```

    **Common Error**

    If you see: "Multiple NetworkIdentity components in hierarchy", check for duplicate components in child objects.

??? info "🔍 NetworkIdentity Properties"

    Properties:
    
    | Property           | Type             | Description                                                                                                   |
    |--------------------|------------------|---------------------------------------------------------------------------------------------------------------|
    | `IdentityId`       | `int`            | Unique identifier used for network synchronization and object tracking |
    | `IsServer`         | `bool`           | Indicates if this instance is running on the server side |
    | `IsClient`         | `bool`           | Indicates if this instance is running on a client machine |
    | `IsLocalPlayer`    | `bool`           | Indicates if this object represents the local player on the local machine |
    | `IsServerOwner`    | `bool`           | Indicates if the server has authority over this objec |
    | `LocalPlayer`      | `NetworkIdentity`| Reference to the local player's NetworkIdentity component |
    | `Owner`            | `NetworkPeer`      | Reference to the client that has authority over this object |
    | `IsRegistered`   | `bool`             | Indicates if this object is registered with the network system |

!!! warning "LocalPlayer Assignment Rules"
    **Overview**

    The `NetworkIdentity.LocalPlayer` property:

    - Only available on client-side
    - Requires specific naming conventions
    - Auto-assigns based on prefab name or tag
    
    **Valid Naming Patterns**

    Prefab name must include "Player":

    ```csharp
    MyPlayer           // ✅ Valid
    PlayerCharacter    // ✅ Valid
    Character          // ❌ Invalid
    ```

    **Valid Tag Patterns**

    GameObject tag must include "Player":

    ```csharp
    "Player"          // ✅ Valid
    "BluePlayer"      // ✅ Valid
    "Character"       // ❌ Invalid
    ```

    **Troubleshooting**

    If `LocalPlayer` is null, check:

    - Prefab name contains "Player"
    - GameObject tag contains "Player"
    - Script is running on client-side

---

## Service Locator Pattern

The **Service Locator** pattern is a design pattern that provides a centralized registry or "locator" for retrieving instances of services or dependencies at runtime. This pattern allows for flexible dependency management and reduces the coupling between objects, making it ideal for complex applications like multiplayer games.

Key Benefits of the Service Locator Pattern

- **Centralized Access to Services**: By acting as a central registry, the Service Locator allows different parts of the application to access services without tightly coupling dependencies.
- **Flexible and Scalable**: Services can be registered, replaced, or removed dynamically, providing flexibility for handling different networked components and systems in a multiplayer environment.
- **Reduced Dependency on Singleton**: Unlike the Singleton pattern, which can introduce issues in multiplayer setups (such as unwanted global state persistence), the Service Locator keeps services manageable and avoids potential conflicts.
- **Improved Testing and Maintainability**: The pattern facilitates testing and maintainability by allowing services to be swapped or mocked, which is crucial in a large multiplayer codebase.

### Service Locator vs. Singleton

While the **Singleton pattern** is commonly used to ensure only one instance of a class exists, it can lead to issues in multiplayer environments. Singletons often hold global state, which can interfere with networked instances and create unpredictable behavior, especially when managing player-specific data.

!!! tip
    Omni recommends using the **Service Locator** pattern instead of Singletons for multiplayer development. The Service Locator provides better control over dependencies and avoids the pitfalls of global state inherent in Singletons, making it a more stable choice for complex networked systems.

### Usage Guide

By default, any script that inherits from a network class(`NetworkBehaviour`, `ServerBehaviour`, `ClientBehaviour`, `DualBehaviour` or `ServiceBehaviour`) is automatically registered in the **Service Locator**. This registration simplifies access to network services across your game's architecture. 

Service Naming and Customization

- **Automatic Naming**: Each service name is assigned automatically upon registration, based on the script's name.
- **Customizable Names**: You can modify the default service name directly in the Unity Inspector, allowing flexibility in organizing and identifying services as needed.

Types of Service Locators in Omni

Omni provides two types of Service Locators to manage service instances effectively within different scopes:

1. **Global Service Locator**: A shared Service Locator accessible across all networked instances. This global registry is ideal for managing universal services that need to be accessed by multiple objects or systems throughout the game.

2. **Local Service Locator**: Each `NetworkIdentity` has its own local Service Locator, unique to that networked identity. allows you to retrieve specific services within the same identity, providing fine control over dependencies and enabling isolated management of services per networked object.

---

With this dual Service Locator approach, Omni offers a flexible, scalable structure that enhances dependency management in multiplayer environments, ensuring services are easily accessible while maintaining clear separation between global and local contexts.

=== "Global Service Locator"
     ```csharp
     // Example usage of the Global Service Locator
     // 1. Define your services
     public class UIManager : ServiceBehaviour
     { 
         public void ShowMenu() { }
     }

     public class LoginManager : ClientBehaviour 
     { 
        public void Login() { }
     }
    
     // 2. Access services from anywhere
     public class Player : NetworkBehaviour
     {
        void Example()
        {
           // Accessing the `LoginManager` from the Global Service Locator
           LoginManager loginManager = NetworkService.Get<LoginManager>();
           loginManager.Login();
    
           // Accessing the `UIManager` from the Global Service Locator with a custom name
           UIManager uiManager = NetworkService.Get<UIManager>("MyUIManager");
           uiManager.ShowMenu();
        }
     }
     ```

=== "Local Service Locator"
     ```csharp
     // Example usage of the Local Service Locator
     // This script must be attached to the same networked object(`NetworkIdentity`).
     // 1. Define components on same GameObject
     public class WeaponManager : NetworkBehaviour
     { 
        public void Fire() { }
     }
    
     public class PlayerManager : NetworkBehaviour
     {
        void Example()
        {
           // Accessing the `WeaponManager` from the Local Service Locator using my own identity
           // Get component from same NetworkIdentity
           WeaponManager weaponManager = Identity.Get<WeaponManager>();
           weaponManager.Fire();

           // Get named component
           var secondary = Identity.Get<WeaponManager>("SecondaryWeapon");
           secondary.Fire();
        }
     }
     ```

!!! note
    The service locator can find services regardless of their depth within the hierarchy, ensuring accessibility even in deeply nested objects.

!!! warning "Multiple Services"
    **When using multiple instances of same service:**

    1. Set unique names in Inspector
    2. Use named lookup:
    ```csharp
    // Get specific weapon manager
    var pistol = Identity.Get<WeaponManager>("PistolManager");
    var rifle = Identity.Get<WeaponManager>("RifleManager");
    ```

    An exception will be thrown if the service is not found or if multiple instances of the same type have the same name.

---

#### With Dependency Injection
```csharp
// Example usage of the Global Service Locator with Dependency Injection
// 1. Define your services
public class ServerManager : ServerBehaviour
{ 
    public void SendAnnouncement(string message) { }
} 

public class WeaponManager : NetworkBehaviour
{ 
    public void Fire() { }
} 

// 2. Access services from anywhere
// 3. Properties or fields are automatically injected by the Source Generator
public partial class Player : NetworkBehaviour
{
    [GlobalService]
    private ServerManager serverManager; 

    [LocalService]
    private WeaponManager weaponManager; 

    // named service
    [LocalService("SecondaryWeapon")]
    private WeaponManager secondaryWeaponManager; 

    void Example()
    {
        // Accessing the `ServerManager` from the Global Service Locator
        serverManager.SendAnnouncement("Hello, world!"); 

        // Accessing the `WeaponManager` from the Local Service Locator
        weaponManager.Fire();
    }
}
```

!!! warning "Dependency Injection Lifecycle"
    **When using dependency injection:**

    - Dependencies are **NOT** available in `Awake()`
    - Dependencies are available starting from `Start()` onwards
    - Access services in `Awake()` manually if needed using `NetworkService.Get<T>()` or `Identity.Get<T>()`

    **Supported Classes**

    Dependencies will only be injected in:

    - `NetworkBehaviour`
    - `ClientBehaviour` and `ServerBehaviour`
    - `DualBehaviour` and `ServiceBehaviour`
    - for other classes (like `MonoBehaviour`), use manual access

!!! tip
    The Service Locator is designed to be fast and efficient, with minimal performance cost, unlike `GetComponent`, making it suitable for frequent use.

*See the API Reference for more information about the Service Locator and its usage.*

## Build & Deployment

### Build Guide

When building your project for deployment, ensure the following steps are completed to ensure a successful multiplayer experience:

!!! tip "Runtime Environment Configuration Guide"
    Choose the optimal runtime setup for your client and server components:

    IL2CPP Client + Mono Server(**Most Common Setup**):
    
    ✅ **Benefits**:

    - Client: Enhanced performance and security through IL2CPP
    - Server: Full .NET feature compatibility
    - Faster development iteration cycles
    
    IL2CPP Server + Mono Client(**Specialized Setup**):
    
    ⚠️ **Considerations**:

    - Higher server performance but limited reflection and database capabilities
    - Potential security risks with Mono client
    - Not recommended for most applications
    
    Unified Runtime(**Single Runtime Solution**):

    **IL2CPP Everywhere:**

    - Maximum performance
    - Enhanced security
    - More complex debugging process
    - Limited reflection and database capabilities
    
    **Mono Everywhere:**
    
    - Full .NET feature compatibility
    - Better debugging experience
    - Security risks on client side

    | Configuration | Performance | Security | Development Ease |
    |--------------|-------------|-----------|------------------|
    | IL2CPP Client + Mono Server | ★★★★☆ | ★★★★☆ | ★★★★★ |
    | IL2CPP Server + Mono Client | ★★★☆☆ | ★★☆☆☆ | ★★★☆☆ |
    | All IL2CPP | ★★★★★ | ★★★★★ | ★★★☆☆ |
    | All Mono | ★★★☆☆ | ★★☆☆☆ | ★★★★★ |

!!! warning "Code Stripping for Client Security"
    Always use conditional compilation to remove sensitive code from client builds:

    ```csharp
    #if OMNI_SERVER
        // Server-only sensitive code here
        private void ProcessSecureData() {
            // This code will be stripped from client builds
        }
    #endif
    ```

    ⚠️ **Critical for Mono Clients**: 

    - Mono builds have higher exposure to decompilation
    - Code stripping is *essential* when using Mono client runtime
    - Use `#if OMNI_SERVER` for:
        - Database credentials
        - Security algorithms
        - Server-side validation logic
        - Administrative functions

Before building for deployment:

1. Switch to Release mode in Unity:

    - Go to the Unity Navigation Bar
    - Select `Omni Networking` and click `Change to Release`

2. Build your project:

    - Go to `File -> Build Settings`
    - Select your target platform
    - Click `Build` to create the executable

!!! danger "Build Mode Reset Warning"
    The build configuration will reset to DEBUG when:
    
    - Switching to new target platforms
    - Updating Omni version
    - Switching Unity version
    
    This happens because each platform/version has unique preprocessor directives (#if).

    ✔️ **Always verify Release mode after:**

    - Platform changes
    - Package updates
    - Unity version changes
    
    To verify/fix:

    1. Open `Omni Networking -> Change to Release`
    2. Verify build settings are correct

### Deployment Guide

When deploying your multiplayer game, consider the following best practices to ensure a smooth and secure experience for your players:

**Recommended Providers:**

| Provider | Pros | Starting Cost |
|----------|------|---------------|
| [DigitalOcean](https://digitalocean.com) | Simple pricing, great for beginners | $5/month |
| [Azure](https://azure.microsoft.com) | Strong .NET integration | $15-20/month |
| [Google Cloud](https://cloud.google.com) | Good automation tools | $10-15/month |

**Minimum VPS Requirements:**

```bash
CPU: 1 cores
RAM: 1GB
Storage: 50GB SSD
Network: 10GB Bandwidth
OS: Ubuntu 20.04 LTS
```
#### Setup Ubuntu Server

After deploying your VPS, follow these steps to set up your Ubuntu server for hosting your multiplayer game:

1. **Connect to Server via SSH**:

    Open a terminal and use the following command to connect to your server:

    ```bash
    ssh -i your-key.pem user@server-ip

    # Example
    ssh -i "C:\Users\user\Downloads\my-key.pem" admin@192.168.0.1
    ```

    Maybe on your first connection, you may need to configure the root password.

2. **Update your Server**:

    Run the following commands to update your server:

    ```bash
    sudo apt update
    sudo apt upgrade
    sudo apt install unzip -y
    ```

3. **Database Setup (Optional)**:

    If your game requires a database, install MySQL/MariaDB or other database systems:

    ```bash
    # Install MySQL
    sudo apt install mysql-server -y

    # Secure MySQL installation
    sudo mysql_secure_installation

    # Start MySQL service
    sudo systemctl start mysql
    sudo systemctl enable mysql
    ```

    If you prefer MariaDB:

    ```bash
    # Install MariaDB
    sudo apt install mariadb-server -y
    
    # Secure MariaDB installation
    sudo mysql_secure_installation

    # Start MariaDB service
    sudo systemctl start mariadb
    sydo systemctl enable mariadb
    ```

    !!! warning "Database Security"
        - Use strong passwords (16+ characters)
        - Only allow local connections by default
        - Regularly backup your database:

        **Backup Your Database**:

        ```bash
        # Access MySQL/MariaDB
        sudo mysql -u root -p

        # Backup your database
        mysqldump -u root -p my_database > backup.sql

        # Restore your database
        mysql -u root -p my_database < backup.sql
        ```




