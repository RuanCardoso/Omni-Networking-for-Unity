using Omni.Inspector;
using Omni.Inspector.Drawers;
using UnityEditor;
using UnityEngine;

[assembly: RegisterTriAttributeDrawer(typeof(LabelWidthDrawer), TriDrawerOrder.Decorator)]

namespace Omni.Inspector.Drawers
{
    public class LabelWidthDrawer : TriAttributeDrawer<LabelWidthAttribute>
    {
        public override void OnGUI(Rect position, TriProperty property, TriElement next)
        {
            var oldLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = Attribute.Width;
            next.OnGUI(position);
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }
    }
}