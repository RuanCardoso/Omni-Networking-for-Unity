﻿using System;
using System.Diagnostics;

namespace Omni.Inspector
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class DeclareTabGroupAttribute : DeclareGroupBaseAttribute
    {
        public DeclareTabGroupAttribute(string path) : base(path)
        {
        }
    }
}