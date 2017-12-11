using System;
using System.Collections.Generic;
using System.Text;

namespace Svt.Network
{
	public interface IProtocolStrategy
	{
        void Parse(string str, RemoteHostState state);
        void Parse(byte[] data, int length, RemoteHostState state);

		System.Text.Encoding Encoding {
			get;
		}
		string Delimiter {
			get;
		}
	}
}
