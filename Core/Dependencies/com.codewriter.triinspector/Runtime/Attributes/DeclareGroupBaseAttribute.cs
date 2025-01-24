using System;

namespace Omni.Inspector
{
    public abstract class DeclareGroupBaseAttribute : Attribute
    {
        protected DeclareGroupBaseAttribute(string path)
        {
            Path = path ?? "None";
        }

        public string Path { get; }
    }
}