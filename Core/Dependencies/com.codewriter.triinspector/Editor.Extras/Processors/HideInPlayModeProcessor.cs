using Omni.Inspector.Processors;
using Omni.Inspector;
using UnityEngine;

[assembly: RegisterTriPropertyHideProcessor(typeof(HideInPlayModeProcessor))]

namespace Omni.Inspector.Processors
{
    public class HideInPlayModeProcessor : TriPropertyHideProcessor<HideInPlayModeAttribute>
    {
        public override bool IsHidden(TriProperty property)
        {
            return Application.isPlaying != Attribute.Inverse;
        }
    }
}