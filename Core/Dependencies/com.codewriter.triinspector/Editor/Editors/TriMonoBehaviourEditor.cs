using UnityEditor;
using UnityEngine;

namespace Omni.Inspector.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), editorForChildClasses: true, isFallback = true)]
    internal sealed class TriMonoBehaviourEditor : TriEditor
    {
    }
}