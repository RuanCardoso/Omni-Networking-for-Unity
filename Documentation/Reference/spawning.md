## Overview

Omni supports spawning objects on both the server and client, providing several methods for network object instantiation. In this guide, we’ll walk you through the available options, from the simplest to the most advanced approaches, enabling you to choose the best method based on your project’s requirements.

Each method allows for a different level of control over networked objects, from basic automatic spawning to more complex, customizable instantiation processes.

## Simple Instantiation

The **Network Object Spawner** component provides a quick and efficient way to instantiate networked objects, making it ideal for prototyping stages. This component simplifies the process by automatically handling the instantiation of both prefabs and scene objects over the network. 

With **Network Object Spawner**, you can set up networked objects with minimal configuration, allowing you to focus on testing gameplay and mechanics rather than complex network setups.

- Add the `Network Object Spawner` component to any game object in your scene.

| Field               | Description                                                                                                                                                       |
|---------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Local Player**    | A `NetworkIdentity` reference for the object that will be controlled by the local player. This object will receive input and commands directly from the local player. |
| **Objects to Spawn** | A list of `NetworkIdentity` references for objects to be managed by the server. These objects will be instantiated over the network and controlled by the server.   |

Using this setup, the `Network Object Spawner` manages instantiation and synchronization, simplifying networked spawning for both player-controlled and server-managed objects.

!!! tip
    You can add either a prefab or a scene object to the **Local Player** or **Objects to Spawn** list, depending on your project’s requirements.

### Manual Instantiation

If you want to instantiate a networked object manually in a straightforward way, here are some examples to guide you. Manual instantiation provides you with more control over when and where the networked object appears in the scene, ideal for scenarios where specific logic or conditions dictate object creation. 

Follow these examples to quickly and easily get started with manual instantiation of networked objects in your project.

**First example:**

```csharp
   // Cache to store all spawn-related messages.
   private DataCache m_SpawnCache = new DataCache(CachePresets.ServerNew);
   
   protected override void OnServerPeerConnected(NetworkPeer peer, Phase phase)
   {
       // Ensure actions occur only after the client is fully connected and authenticated.
       if (phase == Phase.End) // Phase.End: Indicates the client is authenticated and ready for network interactions
       {
           // Retrieve the first registered prefab from the NetworkManager’s prefab list. 
           // Note: you can also use:
           // var prefab = NetworkManager.GetPrefab("PrefabName");
           NetworkIdentity prefab = NetworkManager.GetPrefab(0);

           // Spawn the prefab for the connected peer and pass the spawn cache
           prefab.Spawn(peer, dataCache: m_SpawnCache);
   
           // Send the cached spawn data to the connected peer, allowing late-joining players to 
           // receive all relevant spawn information and ensuring they have a consistent game state.
           m_SpawnCache.SendToPeer(peer);
       }
   }
```

!!! tip
    - `NetworkManager.GetPrefab` offers two overloads: one for retrieving a prefab by its name and another for accessing a prefab by its index in the registered prefab list.
    - `prefab.Spawn` offers two overloads and optional arguments to control the spawn process.

    Check de API reference for more details.

