using Omni.Inspector;
using Omni.Inspector.Drawers;
using UnityEditor;
using UnityEngine;

[assembly: RegisterTriValueDrawer(typeof(ByteDrawer), TriDrawerOrder.Fallback)]

namespace Omni.Inspector.Drawers
{
    public class ByteDrawer : BuiltinDrawerBase<byte>
    {
        protected override byte OnValueGUI(Rect position, GUIContent label, byte value)
        {
            var intValue = EditorGUI.IntField(position, label, value);
            return (byte) Mathf.Clamp(intValue, byte.MinValue, byte.MaxValue);
        }
    }
}