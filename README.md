# OmniNet - Networking at the Subatomic Level in Unity

<table>
  <tr>
    <td width="200">
      <img src="icon.png" alt="OmniNet Logo" width="200" />
    </td>
    <td>
      <h3>OmniNet – High-Performance Multiplayer Framework for Unity</h3>
      <p>
        <strong>OmniNet</strong> is a Unity networking framework designed for performance-critical multiplayer game development. Built to handle real-time communication efficiently, it leverages low-level memory optimizations and modern .NET features to ensure minimal overhead and maximum scalability.
      </p>
      <p>
        <strong>Requirements:</strong> Unity 2021.3 or newer. This version introduced support for .NET Standard 2.1, enabling advanced features such as <code>Span</code>, <code>Memory</code>, and <code>ArrayPool</code>, which are extensively used in OmniNet for optimal performance.
      </p>
      <p>
        <strong>Documentation:</strong>  
        <a href="https://ruancardoso.github.io/Omni-Networking-Doc/">https://ruancardoso.github.io/Omni-Networking-Doc/</a>
      </p>
      <p><em>Developed by Ruan Cardoso and Antonio Junior</em></p>
    </td>
  </tr>
</table>

> ⚠️ **This README provides a summarized overview.**  
> For full documentation, implementation details, API references, and advanced use cases, visit:  
> 📘 [OmniNet Full Documentation](https://ruancardoso.github.io/Omni-Networking-Doc/)

## 🚀 Installation

### ✅ Requirements

- Unity `2021.3` or newer
- API Compatibility Level: `.NET Standard 2.1+`
- Git installed (optional)

> ⚠️ For best results, use Unity LTS versions. Refer to the [Releases page](https://github.com/RuanCardoso/Omni-Networking-for-Unity/releases) for tested versions.

| Unity Version   | Supported |
|-----------------|-----------|
| 2021.3 LTS      | ✅        |
| 2022.3 LTS      | ✅        |
| 2023.2 (Beta)   | ✅        |
| 6000.0 LTS      | ✅        |

---

### 📦 Install via Package Manager (Recommended)

1. Open `Window > Package Manager`
2. Click the `+` button → `Add package from git URL`
3. Paste the following:
   ```
   https://github.com/RuanCardoso/Omni-Networking-for-Unity.git
   ```

---

### 🛠️ Manual Installation

1. Install required packages via Package Manager:
   ```
   com.unity.localization@1.5.4  
   com.unity.nuget.newtonsoft-json@3.2.1
   ```
2. Download the latest version from GitHub and copy the contents to your project’s `Assets` folder.

---

### ✅ Verify Installation

- No errors in the Console
- "Omni Networking" appears in Unity’s top menu

## ⚙️ Setup

Omni provides automatic setup to streamline your development process.

### 🔧 Build Defines

After installation, the following preprocessor defines are auto-configured:

- `OMNI_DEBUG`: Enables debug logs and runtime checks (default in Editor)
- `OMNI_RELEASE`: Disables logging for production builds
- `OMNI_SERVER`: Removes client-only logic for dedicated server builds

Use conditional compilation to separate client/server logic:

```csharp
#if OMNI_DEBUG
    Debug.Log("Debug mode");
#endif

#if OMNI_SERVER
    Debug.Log("Server running");
#endif
```

You can also use `[Conditional]` attributes for optional debug/server methods.

---

### 🧱 Network Manager

1. Go to Unity menu: `Omni Networking > Setup`
2. A `Network Manager` GameObject will be created in the scene

> ⚠️ Do not rename or destroy the `Network Manager` during runtime

---

### 🚚 Transporters

Omni supports three transporters:

- **Lite Transporter** (default): full feature support, including sequenced and reliable modes
- **KCP Transporter**: low-latency, reliable-only UDP (experimental)
- **WebSocket Transporter**: browser-compatible, limited reliability (experimental)

#### Delivery Modes:

- **Reliable Ordered**: guaranteed delivery and order (use for critical game state, inventory, match results)
- **Unreliable**: best for frequent, non-critical updates like movement or effects
- **Sequenced**: only latest packet is processed; older ones are dropped (e.g., player position, timers)
- **Reliable Sequenced**: ensures only the latest critical state is delivered reliably

> ⚠️ **Sequenced** and **Reliable Sequenced** modes are only available with the **Lite Transporter**, and must use separate channels to avoid data conflicts.

To change the transporter:

1. Remove the current transporter component from the `Network Manager`
2. Add the desired transporter component (Lite, KCP, or WebSocket)

> ⚠️ Some transporters do not support all delivery modes. An error will appear if an unsupported feature is used.

For more details:

- [LiteNetLib Documentation](https://github.com/RevenantX/LiteNetLib)  
- [KCP (kcp2k) Documentation](https://github.com/MirrorNetworking/kcp2k)

## 📡 Remote Communication

### 🧠 RPCs (Remote Procedure Calls)

Omni supports client-to-server and server-to-client communication via attribute-based RPCs. Just declare methods with `[Client(id)]` or `[Server(id)]`, and call them using `.Rpc()`.

#### ✅ Basic Usage

```csharp
public class Player : NetworkBehaviour
{
    private const byte RPC_ID = 1;

    [Server(RPC_ID)]
    void ReceiveMove(DataBuffer message)
    {
        Vector3 pos = message.Read<Vector3>();
        Server.Rpc(RPC_ID, pos); // Broadcast to clients
    }

    [Client(RPC_ID)]
    void ApplyMove(DataBuffer message)
    {
        transform.position = message.Read<Vector3>();
    }

    void Update()
    {
        if (IsLocalPlayer)
        {
            Client.Rpc(RPC_ID, transform.position); // Send to server
        }
    }
}
```

#### 🧩 RPC Signatures

You can use up to 3 parameters:

- `DataBuffer message` – data
- `NetworkPeer peer` – sender (server only)
- `int seqChannel` – channel for sequencing or priority

Omni has overloads for sending raw types or `DataBuffer`:

```csharp
Client.Rpc(1, 42, true);                          // primitives
Client.Rpc(1, transform.position, rotation);      // structs
Server.Rpc(1, dataBuffer, Target.AllPlayers);     // with target
```

> ℹ️ IDs must be between `1` and `230`, unique per class. Use `const` values to stay organized.

#### 📤 How to Send

On client:
```csharp
Client.Rpc(1, someData); // to server
```

On server:
```csharp
Server.Rpc(1, someData, Target.SelfOnly); // to specific client
```

> ✅ Use `DeliveryMode` and `Target` for reliability and targeting.

---

### 🔁 Network Variables

Omni provides `[NetworkVariable]` for automatic value sync from server to clients.

```csharp
public partial class Player : NetworkBehaviour
{
    [NetworkVariable]
    [SerializeField] // optional for inspector editing
    private float m_Health = 100f;

    void Update()
    {
        if (IsServer && Input.GetKeyDown(KeyCode.Space))
        {
            Health -= 10f; // auto-syncs
        }
    }
}
```

#### 🧩 Hooks & Sync

- Classes must be `partial`
- Fields must use the `m_` prefix and start with a capital (e.g., `m_Health`)
- The property is generated and synced automatically
- Use `SyncHealth()` if setting the field directly

```csharp
partial void OnHealthChanged(float prev, float next, bool isWriting)
{
    Debug.Log($"Health changed: {prev} → {next}");
}
```

> ℹ️ You can customize delivery via `HealthOptions` or use `DefaultNetworkVariableOptions` globally.

---

### 🌐 RouteX (Server Routing System)

RouteX is an Express-style server routing system. It allows structured communication with routes like `/login`, `/match`, etc.

#### 🔧 Define Routes (Server)

```csharp
protected override void OnAwake()
{
    XServer.GetAsync("/login", (res) => {
        res.WriteString("You're logged in!");
        res.Send();
    });

    XServer.PostAsync("/login", (req, res) => {
        string username = req.ReadString();
        res.WriteString($"Welcome, {username}!");
        res.Send();
    });
}
```

#### 📲 Send Requests (Client)

```csharp
async void Login()
{
    using var res = await XClient.PostAsync("/login", req => {
        req.WriteString("John Doe");
    });

    string msg = res.ReadString();
    Debug.Log(msg);
}
```

Supports `HttpResponse`, status codes, and payloads:

```csharp
res.WriteHttpResponse(new HttpResponse() {
    StatusCode = StatusCode.Success,
    StatusMessage = "OK!"
});
```

---

### 📦 Serialization & DataBuffer

Omni uses `DataBuffer` for all messages — a binary stream system like `BinaryWriter`.

#### ✅ Supported

- Primitive types (`int`, `float`, `bool`, etc.)
- Unity types (`Vector3`, `Quaternion`, etc.)
- Complex objects (via JSON or MemoryPack)
- Compression (`LZ4`, `Brotli`)
- Encryption (`AES` + RSA key exchange)

#### ✏️ Example: Write & Read

```csharp
var buffer = new DataBuffer();
buffer.Write(42);
buffer.WriteAsJson(player);

int value = buffer.Read<int>();
Player p = buffer.ReadAsJson<Player>();
```

> ⚠️ Always read in the same order you write.

---

### 🔐 Encryption

Omni encrypts buffers using AES. You can encrypt using:

- Peer key: `message.EncryptRaw(peer)`
- Global key: `message.EncryptRaw(NetworkManager.SharedPeer)`

> RSA is used to securely exchange keys. Each peer gets a unique AES key.

```csharp
message.EncryptRaw(NetworkManager.LocalPeer);
message.DecryptRaw(NetworkManager.SharedPeer);
```

---

### 🧰 IMessage Interface

Create custom serializable types using `IMessage`:

```csharp
public class PlayerStruct : IMessage
{
    string Name;
    Vector3 Position;

    public void Serialize(DataBuffer writer)
    {
        writer.WriteString(Name);
        writer.Write(Position);
    }

    public void Deserialize(DataBuffer reader)
    {
        Name = reader.ReadString();
        Position = reader.Read<Vector3>();
    }
}
```

Use it in `NetworkVariable` or `Rpc()`:

```csharp
[NetworkVariable]
private PlayerStruct m_PlayerData;
```

Or send directly:

```csharp
Server.Rpc(1, m_PlayerData, new() {
    DeliveryMode = DeliveryMode.ReliableOrdered
});
```

