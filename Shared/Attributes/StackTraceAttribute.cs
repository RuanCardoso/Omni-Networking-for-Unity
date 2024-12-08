using System;
using System.Diagnostics;

namespace Omni.Core
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	[Conditional("UNITY_EDITOR")]
	public sealed class StackTraceAttribute : Attribute
	{

	}
}
