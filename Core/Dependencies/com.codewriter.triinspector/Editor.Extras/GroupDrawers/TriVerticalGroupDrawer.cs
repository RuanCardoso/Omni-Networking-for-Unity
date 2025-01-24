using Omni.Inspector.Elements;
using Omni.Inspector;
using Omni.Inspector.GroupDrawers;

[assembly: RegisterTriGroupDrawer(typeof(TriVerticalGroupDrawer))]

namespace Omni.Inspector.GroupDrawers
{
    public class TriVerticalGroupDrawer : TriGroupDrawer<DeclareVerticalGroupAttribute>
    {
        public override TriPropertyCollectionBaseElement CreateElement(DeclareVerticalGroupAttribute attribute)
        {
            return new TriVerticalGroupElement();
        }
    }
}