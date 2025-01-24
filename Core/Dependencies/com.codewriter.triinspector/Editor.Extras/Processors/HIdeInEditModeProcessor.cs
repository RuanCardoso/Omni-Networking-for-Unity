using Omni.Inspector.Processors;
using Omni.Inspector;
using UnityEngine;

[assembly: RegisterTriPropertyHideProcessor(typeof(HideInEditModeProcessor))]

namespace Omni.Inspector.Processors
{
    public class HideInEditModeProcessor : TriPropertyHideProcessor<HideInEditModeAttribute>
    {
        public override bool IsHidden(TriProperty property)
        {
            return Application.isPlaying == Attribute.Inverse;
        }
    }
}