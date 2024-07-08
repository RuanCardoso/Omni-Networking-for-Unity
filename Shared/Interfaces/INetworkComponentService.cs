using UnityEngine;

namespace Omni.Core.Interfaces
{
    public interface INetworkComponentService
    {
        GameObject GameObject { get; }
        MonoBehaviour Component { get; }
    }
}
