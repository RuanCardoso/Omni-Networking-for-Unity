﻿using System;
using System.Reflection;

#pragma warning disable

namespace Omni.Core
{
    internal static class TypeExtensions
    {
        public static MethodInfo? GetPublicInstanceMethod(this Type type, string name, Type[] types)
            => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null, types, null);
    }
}
