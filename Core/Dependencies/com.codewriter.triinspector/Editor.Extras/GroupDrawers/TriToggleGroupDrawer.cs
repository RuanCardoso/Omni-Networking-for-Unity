using Omni.Inspector.Elements;
using Omni.Inspector;
using Omni.Inspector.GroupDrawers;

[assembly: RegisterTriGroupDrawer(typeof(TriToggleGroupDrawer))]

namespace Omni.Inspector.GroupDrawers
{
    public class TriToggleGroupDrawer : TriGroupDrawer<DeclareToggleGroupAttribute>
    {
        public override TriPropertyCollectionBaseElement CreateElement(DeclareToggleGroupAttribute attribute)
        {
            return new TriBoxGroupElement(new TriBoxGroupElement.Props
            {
                title = attribute.Title,
                titleMode = TriBoxGroupElement.TitleMode.Toggle,
                expandedByDefault = attribute.Collapsible,
                hideIfChildrenInvisible = true,
            });
        }
    }
}