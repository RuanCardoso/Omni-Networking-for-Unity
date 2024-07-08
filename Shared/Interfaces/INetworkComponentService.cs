using UnityEngine;

namespace Omni.Core.Interfaces
{
    public interface INetworkComponentService
    {
        Component Component { get; }
        GameObject GameObject { get; }
    }
}
