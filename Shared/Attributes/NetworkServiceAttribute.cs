using System;

namespace Omni.Core
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class ServiceAttribute : Attribute
	{
		internal string ServiceName { get; }

		public ServiceAttribute(string serviceName)
		{
			ServiceName = serviceName;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class GlobalServiceAttribute : ServiceAttribute
	{
		public GlobalServiceAttribute(string serviceName) : base(serviceName)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class LocalServiceAttribute : ServiceAttribute
	{
		public LocalServiceAttribute(string serviceName) : base(serviceName)
		{
		}
	}
}