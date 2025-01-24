﻿using System;
using System.Diagnostics;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    [Conditional("UNITY_EDITOR")]
    public class EnableInEditModeAttribute : DisableInEditModeAttribute
    {
        public EnableInEditModeAttribute()
        {
            Inverse = true;
        }
    }
}