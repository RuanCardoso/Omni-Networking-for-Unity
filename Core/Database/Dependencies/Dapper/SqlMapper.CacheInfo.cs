﻿using System;
using System.Data;
using System.Data.Common;
using System.Threading;

#pragma warning disable

namespace Omni.Core
{
    public static partial class SqlMapper
    {
        private class CacheInfo
        {
            public DeserializerState Deserializer { get; set; }
            public Func<DbDataReader, object>[]? OtherDeserializers { get; set; }
            public Action<IDbCommand, object?>? ParamReader { get; set; }
            private int hitCount;
            public int GetHitCount() { return Interlocked.CompareExchange(ref hitCount, 0, 0); }
            public void RecordHit() { Interlocked.Increment(ref hitCount); }
        }
    }
}
