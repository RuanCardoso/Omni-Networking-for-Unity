﻿using System;
using System.Diagnostics;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
                    AttributeTargets.Class | AttributeTargets.Struct)]
    [Conditional("UNITY_EDITOR")]
    public class ReadOnlyAttribute : Attribute
    {
    }
}