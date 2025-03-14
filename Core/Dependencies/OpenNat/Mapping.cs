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
using System.Net;

namespace OpenNat
{
	/// <summary>
	/// Represents a port forwarding entry in the NAT translation table.
	/// </summary>
	public class Mapping
	{
		private DateTime _expiration;
		private int _lifetime;

		/// <summary>
		/// Gets the mapping's description. It is the value stored in the NewPortMappingDescription parameter. 
		/// The NewPortMappingDescription parameter is a human readable string that describes the connection. 
		/// It is used in sorme web interfaces of routers so the user can see which program is using what port.
		/// </summary>
		public string Description { get; internal set; }

		/// <summary>
		/// Gets the private ip.
		/// </summary>
		public IPAddress PrivateIP { get; internal set; }

		/// <summary>
		/// Gets the protocol.
		/// </summary>
		public Protocol Protocol { get; internal set; }

		/// <summary>
		/// The PrivatePort parameter specifies the port on a client machine to which all traffic 
		/// coming in on <see cref="PublicPort">PublicPort</see> for the protocol specified by 
		/// <see cref="Protocol">Protocol</see> should be forwarded to.
		/// </summary>
		/// <see cref="Protocol">Protocol enum</see>
		public int PrivatePort { get; internal set; }

		/// <summary>
		/// Gets the public ip.
		/// </summary>
		public IPAddress PublicIP { get; internal set; }

		/// <summary>
		/// Gets the external (visible) port number.
		/// It is the value stored in the NewExternalPort parameter .
		/// The NewExternalPort parameter is used to specify the TCP or UDP port on the WAN side of the router which should be forwarded. 
		/// </summary>
		public int PublicPort { get; internal set; }

		/// <summary>
		/// Gets the lifetime in seconds. The Lifetime parameter tells the router how long the portmapping should be active. 
		/// Since most programs don't know this in advance, it is often set to 0, which means 'unlimited' or 'permanent'.
		/// </summary>
		/// <remarks>
		/// All portmappings are release automatically as part of the shutdown process.
		/// Permanent portmappings will not be released if the process ends anormally.
		/// Since most programs don't know the lifetime in advance, SharpOpenNat renew all the portmappings (except the permanents) before they expires. So, developers have to close explicitly those portmappings
		/// they don't want to remain open for the session.
		/// </remarks>
		public int Lifetime
		{
			get { return _lifetime; }
			internal set
			{
				_lifetime = value;
				_expiration = DateTime.UtcNow.AddSeconds(_lifetime);
			}
		}

		/// <summary>
		/// Gets the expiration. The property value is calculated using <see cref="Lifetime">Lifetime</see> property.
		/// </summary>
		public DateTime Expiration
		{
			get { return _expiration; }
			set
			{
				_expiration = value;
				_lifetime = (int)(_expiration - DateTime.UtcNow).TotalSeconds;
			}
		}

		internal Mapping(Protocol protocol, IPAddress privateIP, int privatePort, int publicPort)
			: this(protocol, privateIP, privatePort, publicPort, int.MaxValue, "SharpOpenNat")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mapping"/> class.
		/// </summary>
		/// <param name="protocol">The protocol.</param>
		/// <param name="privateIP">The private ip.</param>
		/// <param name="privatePort">The private port.</param>
		/// <param name="publicPort">The public port.</param>
		/// <param name="lifetime">The lifetime in seconds.</param>
		/// <param name="description">The description.</param>
		public Mapping(Protocol protocol, IPAddress privateIP, int privatePort, int publicPort, int lifetime, string description)
		{
			Guard.IsInRange(privatePort, 0, ushort.MaxValue, nameof(privatePort));
			Guard.IsInRange(publicPort, 0, ushort.MaxValue, nameof(publicPort));
			Guard.IsInRange(lifetime, 0, int.MaxValue, nameof(lifetime));
			Guard.IsTrue(protocol == Protocol.Tcp || protocol == Protocol.Udp, nameof(protocol));
			Guard.IsNotNull(privateIP, nameof(privateIP));

			Protocol = protocol;
			PrivateIP = privateIP;
			PrivatePort = privatePort;
			PublicIP = IPAddress.None;
			PublicPort = publicPort;
			Lifetime = lifetime;
			Description = description;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mapping"/> class.
		/// </summary>
		/// <param name="protocol">The protocol.</param>
		/// <param name="privatePort">The private port.</param>
		/// <param name="publicPort">The public port.</param>
		/// <remarks>
		/// This constructor initializes a Permanent mapping. The description by default is "SharpOpenNat"
		/// </remarks>
		public Mapping(Protocol protocol, int privatePort, int publicPort)
			: this(protocol, IPAddress.None, privatePort, publicPort, int.MaxValue, "SharpOpenNat")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mapping"/> class.
		/// </summary>
		/// <param name="protocol">The protocol.</param>
		/// <param name="privatePort">The private port.</param>
		/// <param name="publicPort">The public port.</param>
		/// <param name="description">The description.</param>
		/// <remarks>
		/// This constructor initializes a Permanent mapping.
		/// </remarks>
		public Mapping(Protocol protocol, int privatePort, int publicPort, string description)
			: this(protocol, IPAddress.None, privatePort, publicPort, int.MaxValue, description)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Mapping"/> class.
		/// </summary>
		/// <param name="protocol">The protocol.</param>
		/// <param name="privatePort">The private port.</param>
		/// <param name="publicPort">The public port.</param>
		/// <param name="lifetime">The lifetime in seconds.</param>
		/// <param name="description">The description.</param>
		public Mapping(Protocol protocol, int privatePort, int publicPort, int lifetime, string description)
			: this(protocol, IPAddress.None, privatePort, publicPort, lifetime, description)
		{
		}

		internal Mapping(Mapping mapping)
		{
			PrivateIP = mapping.PrivateIP;
			PrivatePort = mapping.PrivatePort;
			Protocol = mapping.Protocol;
			PublicIP = mapping.PublicIP;
			PublicPort = mapping.PublicPort;
			Description = mapping.Description;
			_lifetime = mapping._lifetime;
			_expiration = mapping._expiration;
		}

		/// <summary>
		/// Determines whether this instance is expired.
		/// </summary>
		/// <remarks>
		/// Permanent mappings never expires.
		/// </remarks>
		public bool IsExpired()
		{
			return Expiration < DateTime.UtcNow;
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj is not Mapping m) return false;
			return PublicPort == m.PublicPort && PrivatePort == m.PrivatePort;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return HashCode.Combine(PublicPort, PrivateIP, PrivatePort);
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString()
		{
			return string.Format("{0} {1} --> {2}:{3} ({4})",
									Protocol == Protocol.Tcp ? "Tcp" : "Udp",
									PublicPort,
									PrivateIP,
									PrivatePort,
									Description);
		}
	}
}
