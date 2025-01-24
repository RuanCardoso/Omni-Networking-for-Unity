using Omni.Inspector.Elements;
using Omni.Inspector;
using Omni.Inspector.GroupDrawers;

[assembly: RegisterTriGroupDrawer(typeof(TriTabGroupDrawer))]

namespace Omni.Inspector.GroupDrawers
{
    public class TriTabGroupDrawer : TriGroupDrawer<DeclareTabGroupAttribute>
    {
        public override TriPropertyCollectionBaseElement CreateElement(DeclareTabGroupAttribute attribute)
        {
            return new TriTabGroupElement();
        }
    }
}