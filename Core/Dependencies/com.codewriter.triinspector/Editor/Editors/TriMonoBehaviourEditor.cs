using Omni.Core;
using UnityEditor;

namespace Omni.Inspector.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(OmniBehaviour), editorForChildClasses: true, isFallback = true)]
    internal sealed class TriMonoBehaviourEditor : TriEditor
    {
    }
}