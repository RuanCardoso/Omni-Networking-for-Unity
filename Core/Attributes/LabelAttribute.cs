using UnityEngine;

namespace Omni.Core.Attributes
{
    public class LabelAttribute : PropertyAttribute
    {
        public string Label { get; }

        public LabelAttribute(string label)
        {
            Label = label;
        }
    }
}
