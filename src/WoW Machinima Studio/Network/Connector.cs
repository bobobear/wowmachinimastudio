using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace WoW_Machinima_Studio.Network
{
	public class Connector
	{
		//TODO Connector class implemention
		public event Action<Packet> OnRecieve;
		public event Action<IPAddress> OnAccept;

		public void Start();
		public void Stop();

		public void Send(Packet packet);
	}
}
