using Omni.Inspector.Elements;
using Omni.Inspector;
using Omni.Inspector.GroupDrawers;
using UnityEngine;

[assembly: RegisterTriGroupDrawer(typeof(TriHorizontalGroupDrawer))]

namespace Omni.Inspector.GroupDrawers
{
    public class TriHorizontalGroupDrawer : TriGroupDrawer<DeclareHorizontalGroupAttribute>
    {
        public override TriPropertyCollectionBaseElement CreateElement(DeclareHorizontalGroupAttribute attribute)
        {
            return new TriHorizontalGroupElement(attribute.Sizes);
        }
    }
}