using System.Collections.Generic;
using Omni.Inspector;
using UnityEngine;

public class Decorators_SceneSample : ScriptableObject
{
    [Scene] public string scene;

    [Scene] public List<string> scenes;
}