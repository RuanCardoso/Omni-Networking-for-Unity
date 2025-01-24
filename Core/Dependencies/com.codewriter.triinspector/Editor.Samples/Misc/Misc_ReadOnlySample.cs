using Omni.Inspector;
using UnityEngine;

public class Misc_ReadOnlySample : ScriptableObject
{
    [ReadOnly]
    public Vector3 vec;
}