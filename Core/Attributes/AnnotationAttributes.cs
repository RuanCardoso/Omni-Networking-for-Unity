using System;

namespace Omni.Core.Attributes
{
    internal class ServerOnlyAttribute : Attribute
    {
    }

    internal class ClientOnlyAttribute : Attribute
    {
    }
}

namespace Omni.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class SkipCodeGen : Attribute
    {
    }
}