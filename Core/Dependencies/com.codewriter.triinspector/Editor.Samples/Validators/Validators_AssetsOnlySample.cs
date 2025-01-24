using Omni.Inspector;
using UnityEngine;

public class Validators_AssetsOnlySample : ScriptableObject
{
    [AssetsOnly]
    public GameObject obj;
}