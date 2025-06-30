using System;
using Omni.Core;
using Omni.Core.Cryptography;
using UnityEngine;

public partial class ServerSampleTest : ServerBehaviour
{
    protected override void OnServerPeerConnected(NetworkPeer peer, Phase phase)
    {
        if (phase == Phase.Ended)
        {
            using DataBuffer message = Rent();
            message.WriteString("Hello from server");
            // criptografa os dados da mensagem com a chave global compartilhada pelo servidor
            // Isso significa que a mensagem pode ser decrifrada por qualquer cliente que tenha a chave global.
            // message.EncryptInPlace(NetworkManager.SharedPeer);
            // Envia a mensagem pro cliente
            // Server.Rpc(1, peer, message);
        }
    }

    [Client(1)]
    void OnMessageReceivedRpc(DataBuffer message)
    {
        // Vamos decifrar a mensagem com a chave global compartilhada pelo servidor
        // message.DecryptInPlace(NetworkManager.SharedPeer);

        // Agora podemos ler a mensagem
        string msg = message.ReadString();
        Debug.Log($"Message received: {msg}");
    }
}
