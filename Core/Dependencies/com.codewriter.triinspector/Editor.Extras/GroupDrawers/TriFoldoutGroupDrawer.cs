using Omni.Inspector.Elements;
using Omni.Inspector;
using Omni.Inspector.GroupDrawers;

[assembly: RegisterTriGroupDrawer(typeof(TriFoldoutGroupDrawer))]

namespace Omni.Inspector.GroupDrawers
{
    public class TriFoldoutGroupDrawer : TriGroupDrawer<DeclareFoldoutGroupAttribute>
    {
        public override TriPropertyCollectionBaseElement CreateElement(DeclareFoldoutGroupAttribute attribute)
        {
            return new TriBoxGroupElement(new TriBoxGroupElement.Props
            {
                title = attribute.Title,
                titleMode = TriBoxGroupElement.TitleMode.Foldout,
                expandedByDefault = attribute.Expanded,
                hideIfChildrenInvisible = true,
            });
        }
    }
}