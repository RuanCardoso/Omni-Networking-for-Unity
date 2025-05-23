﻿using System;
using System.Data.Common;

namespace Omni.Core
{
    public static partial class SqlMapper
    {
        private readonly struct DeserializerState
        {
            public readonly int Hash;
            public readonly Func<DbDataReader, object> Func;

            public DeserializerState(int hash, Func<DbDataReader, object> func)
            {
                Hash = hash;
                Func = func;
            }
        }
    }
}
