using Omni.Inspector.Utilities;
using Omni.Inspector;
using Omni.Inspector.Drawers;
using UnityEngine;

[assembly: RegisterTriAttributeDrawer(typeof(IndentDrawer), TriDrawerOrder.Decorator)]

namespace Omni.Inspector.Drawers
{
    public class IndentDrawer : TriAttributeDrawer<IndentAttribute>
    {
        public override void OnGUI(Rect position, TriProperty property, TriElement next)
        {
            using (var indentedRectScope = TriGuiHelper.PushIndentedRect(position, Attribute.Indent))
            {
                next.OnGUI(indentedRectScope.IndentedRect);
            }
        }
    }
}