//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//   Lucas Ontivero lucasontivero@gmail.com 
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Lucas Ontivero
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#pragma warning disable

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace OpenNat
{
	internal sealed class UpnpNatDevice : NatDevice
	{
		public override IPEndPoint HostEndPoint
		{
			get { return DeviceInfo.HostEndPoint; }
		}

		public override IPAddress LocalAddress
		{
			get { return DeviceInfo.LocalAddress; }
		}

		internal readonly UpnpNatDeviceInfo DeviceInfo;
		private readonly SoapClient _soapClient;

		internal UpnpNatDevice(UpnpNatDeviceInfo deviceInfo)
		{
			Touch();
			DeviceInfo = deviceInfo;
			_soapClient = new SoapClient(DeviceInfo.ServiceControlUri, DeviceInfo.ServiceType);
		}

		public override async Task<IPAddress?> GetExternalIPAsync()
		{
			NatDiscoverer.TraceSource.LogInfo("GetExternalIPAsync - Getting external IP address");
			var message = new GetExternalIPAddressRequestMessage();
			var responseData = await _soapClient
				.InvokeAsync("GetExternalIPAddress", message.ToXml())
				.TimeoutAfter(TimeSpan.FromSeconds(4));

			var response = new GetExternalIPAddressResponseMessage(responseData, DeviceInfo.ServiceType);
			return response.ExternalIPAddress;
		}

		public override async Task CreatePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, nameof(mapping));
			if (mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP = DeviceInfo.LocalAddress;

			NatDiscoverer.TraceSource.LogInfo("CreatePortMapAsync - Creating port mapping {0}", mapping);
			bool retry = false;
			try
			{
				var message = new CreatePortMappingRequestMessage(mapping);
				await _soapClient
					.InvokeAsync("AddPortMapping", message.ToXml())
					.TimeoutAfter(TimeSpan.FromSeconds(4));
				RegisterMapping(mapping);
			}
			catch (MappingException me)
			{
				switch (me.ErrorCode)
				{
					case UpnpConstants.OnlyPermanentLeasesSupported:
						NatDiscoverer.TraceSource.LogWarn("Only Permanent Leases Supported - There is no warranty it will be closed");
						mapping.Lifetime = 0;
						retry = true;
						break;
					case UpnpConstants.SamePortValuesRequired:
						NatDiscoverer.TraceSource.LogWarn("Same Port Values Required - Using internal port {0}", mapping.PrivatePort);
						mapping.PublicPort = mapping.PrivatePort;
						retry = true;
						break;
					case UpnpConstants.RemoteHostOnlySupportsWildcard:
						NatDiscoverer.TraceSource.LogWarn("Remote Host Only Supports Wildcard");
						mapping.PublicIP = IPAddress.None;
						retry = true;
						break;
					case UpnpConstants.ExternalPortOnlySupportsWildcard:
						NatDiscoverer.TraceSource.LogWarn("External Port Only Supports Wildcard");
						throw;
					case UpnpConstants.ConflictInMappingEntry:
						NatDiscoverer.TraceSource.LogWarn("Conflict with an already existing mapping");
						throw;

					default:
						throw;
				}
			}

			if (retry)
			{
				await CreatePortMapAsync(mapping);
			}
		}

		public override async Task DeletePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, nameof(mapping));

			if (mapping.PrivateIP.Equals(IPAddress.None)) mapping.PrivateIP = DeviceInfo.LocalAddress;

			NatDiscoverer.TraceSource.LogInfo("DeletePortMapAsync - Deleteing port mapping {0}", mapping);

			try
			{
				var message = new DeletePortMappingRequestMessage(mapping);
				await _soapClient
					.InvokeAsync("DeletePortMapping", message.ToXml())
					.TimeoutAfter(TimeSpan.FromSeconds(4));
				UnregisterMapping(mapping);
			}
			catch (MappingException e)
			{
				if (e.ErrorCode != UpnpConstants.NoSuchEntryInArray) throw;
			}
		}

		public override async Task<IEnumerable<Mapping>> GetAllMappingsAsync()
		{
			var index = 0;
			var mappings = new List<Mapping>();

			NatDiscoverer.TraceSource.LogInfo("GetAllMappingsAsync - Getting all mappings");
			while (true)
			{
				try
				{
					var message = new GetGenericPortMappingEntry(index++);

					var responseData = await _soapClient
						.InvokeAsync("GetGenericPortMappingEntry", message.ToXml())
						.TimeoutAfter(TimeSpan.FromSeconds(4));

					var responseMessage = new GetPortMappingEntryResponseMessage(responseData, DeviceInfo.ServiceType, true);

					if (!IPAddress.TryParse(responseMessage.InternalClient, out IPAddress? internalClientIp))
					{
						NatDiscoverer.TraceSource.LogWarn("InternalClient is not an IP address. Mapping ignored!");
						continue;
					}

					var mapping = new Mapping(responseMessage.Protocol
						, internalClientIp
						, responseMessage.InternalPort
						, responseMessage.ExternalPort
						, responseMessage.LeaseDuration
						, responseMessage.PortMappingDescription);
					mappings.Add(mapping);
				}
				catch (MappingException e)
				{
					// there are no more mappings
					if (e.ErrorCode == UpnpConstants.SpecifiedArrayIndexInvalid
					 || e.ErrorCode == UpnpConstants.NoSuchEntryInArray
					 // DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument when index is out of range
					 || e.ErrorCode == UpnpConstants.InvalidArguments
					 // LINKSYS WRT1900AC AC1900 it returns errocode 501-PAL_UPNP_SOAP_E_ACTION_FAILED
					 || e.ErrorCode == UpnpConstants.ActionFailed
					 // LINKSYS EA8300 fails with 601:"Argument Value Out of Range" when index is out of range
					 || e.ErrorCode == UpnpConstants.ArgumentValueOutOfRange)
					{
						NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", e.ErrorCode, e.ErrorText ?? string.Empty);
						break;
					}
					throw;
				}
			}

			return mappings.ToArray();
		}

		public override async Task<Mapping?> GetSpecificMappingAsync(Protocol protocol, int publicPort)
		{
			Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, nameof(protocol));
			Guard.IsInRange(publicPort, 0, ushort.MaxValue, "port");

			NatDiscoverer.TraceSource.LogInfo("GetSpecificMappingAsync - Getting mapping for protocol: {0} port: {1}", Enum.GetName(typeof(Protocol), protocol)!, publicPort);

			try
			{
				var message = new GetSpecificPortMappingEntryRequestMessage(protocol, publicPort);
				var responseData = await _soapClient
					.InvokeAsync("GetSpecificPortMappingEntry", message.ToXml())
					.TimeoutAfter(TimeSpan.FromSeconds(4));

				var messageResponse = new GetPortMappingEntryResponseMessage(responseData, DeviceInfo.ServiceType, false);

				if (messageResponse.Protocol != protocol)
					NatDiscoverer.TraceSource.LogWarn("Router responded to a protocol {0} query with a protocol {1} answer, work around applied.", protocol, messageResponse.Protocol);

				return new Mapping(protocol
					, IPAddress.Parse(messageResponse.InternalClient)
					, messageResponse.InternalPort
					, publicPort // messageResponse.ExternalPort is short.MaxValue
					, messageResponse.LeaseDuration
					, messageResponse.PortMappingDescription);
			}
			catch (MappingException e)
			{
				// there are no more mappings
				if (e.ErrorCode == UpnpConstants.SpecifiedArrayIndexInvalid
				 || e.ErrorCode == UpnpConstants.NoSuchEntryInArray
				 // DD-WRT Linux base router (and others probably) fails with 402-InvalidArgument when index is out of range
				 || e.ErrorCode == UpnpConstants.InvalidArguments
				 // LINKSYS WRT1900AC AC1900 it returns errocode 501-PAL_UPNP_SOAP_E_ACTION_FAILED
				 || e.ErrorCode == UpnpConstants.ActionFailed
				 // LINKSYS EA8300 fails with 601:"Argument Value Out of Range" when index is out of range
				 || e.ErrorCode == UpnpConstants.ArgumentValueOutOfRange)
				{
					NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", e.ErrorCode, e.ErrorText ?? string.Empty);
					return null;
				}

				throw;
			}
		}

		public override string ToString()
		{
			//GetExternalIP is blocking and can throw exceptions, can't use it here.
			return String.Format(
				"EndPoint: {0}\nControl Url: {1}\nService Type: {2}\nLast Seen: {3}",
				DeviceInfo.HostEndPoint, DeviceInfo.ServiceControlUri, DeviceInfo.ServiceType, LastSeen);
		}
	}
}
