using Omni.Inspector.Processors;
using Omni.Inspector;
using UnityEngine;

[assembly: RegisterTriPropertyDisableProcessor(typeof(DisableInEditModeProcessor))]

namespace Omni.Inspector.Processors
{
    public class DisableInEditModeProcessor : TriPropertyDisableProcessor<DisableInEditModeAttribute>
    {
        public override bool IsDisabled(TriProperty property)
        {
            return Application.isPlaying == Attribute.Inverse;
        }
    }
}