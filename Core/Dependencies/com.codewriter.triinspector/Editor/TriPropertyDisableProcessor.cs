using System;
using JetBrains.Annotations;

namespace Omni.Inspector
{
    public abstract class TriPropertyDisableProcessor : TriPropertyExtension
    {
        internal Attribute RawAttribute { get; set; }

        [PublicAPI]
        public abstract bool IsDisabled(TriProperty property);
    }

    public abstract class TriPropertyDisableProcessor<TAttribute> : TriPropertyDisableProcessor
        where TAttribute : Attribute
    {
        [PublicAPI]
        public TAttribute Attribute => (TAttribute) RawAttribute;
    }
}