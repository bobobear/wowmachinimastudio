using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace WoW_Machinima_Studio.Network
{
	public class Connector
	{
		//TODO Connector class implemention
		private bool _is_accepting;
		private bool _is_reading;
		private Thread _get_packet_thread;
		private Thread _accept_client_thread;
		private String _clientversion;
		public String ClientVersion
		{
			get { return _clientversion; }
		}
		private String _clientbuild;
		public String ClientBuild
		{
			get { return _clientbuild; }
		}
		private String _username;
		public String Username
		{
			get { return _username; }
		}
		private TcpClient _client;
		public TcpClient Client
		{
			get { return _client; }
		}
		private TcpListener _listener;
		public TcpListener Listener
		{
			get { return _listener; }
		}
		private IPAddress _clientip;
		public IPAddress ClientIP
		{
			get { return _clientip; }
		}
		public event Action<Packet> OnReceive;
		public event Action<IPAddress> OnAccept;

		public Connector()
		{
			_listener = new TcpListener(IPAddress.Parse("127.0.0.1"),3724);
		}
		public void Start()
		{
			_listener.Start();
			//_accept_client_thread = new Thread(_accept_client_thread_proc);
			//_accept_client_thread.Start();
			_accept_client_thread_proc();
		}
		private void _analyze_packet(Packet p)
		{
		}
		private void _get_packet_thread_proc()
		{
			bool is_first_packet = true;
			_is_reading = true;
			while (_is_reading)
			{
				if (_client.Available != 0)
				{
					if (is_first_packet)
					{
						try
						{
							is_first_packet = false;
							BinaryReader reader = new BinaryReader(_client.GetStream());
							switch (reader.ReadByte())
							{
								case (byte)Packet.RS_RECONNECT_CHALLENGE:
								case (byte)Packet.RS_LOGON_CHALLENGE:
									reader.ReadBytes(7);
									_clientversion = reader.ReadByte() + "." + reader.ReadByte() + "." + reader.ReadByte();
									_clientbuild = reader.ReadInt16().ToString();
									reader.ReadBytes(16);
									_clientip = new IPAddress(reader.ReadBytes(4));
									_username = reader.ReadString();
									_client.GetStream().WriteByte((byte)Packet.RS_LOGON_CHALLENGE);
									_client.GetStream().WriteByte((byte)Packet.login_no_error);
									_client.GetStream().Write(new byte[101], 0, 101);
									break;
								default:
									break;
							}
						}
						catch
						{
							_client.Close();
							is_first_packet = true;
						}
					}
					else
					{
						Packet p = Packet.FromStream(_client.GetStream());
						_analyze_packet(p);
						Thread.Sleep(1000);
					}
				}
				if (!_client.Connected)
					_is_reading = false;
			}
		}
		private void _accept_client_thread_proc()
		{
			_is_accepting = true;
			while (_is_accepting)
			{
				if (_listener.Pending())
				{
					_client = _listener.AcceptTcpClient();
					//OnAccept(((IPEndPoint)_client.Client.RemoteEndPoint).Address);
					_is_accepting = false;
				}
			}

			//_get_packet_thread = new Thread(_get_packet_thread_proc);
			//_get_packet_thread.Start();
			_get_packet_thread_proc();
		}
		public void Stop()
		{
			if ((_get_packet_thread != null) && (_get_packet_thread.IsAlive))
				_get_packet_thread.Abort();
			if ((_accept_client_thread != null) && (_accept_client_thread.IsAlive))
				_accept_client_thread.Abort();
			_get_packet_thread = null;
			_listener.Stop();
		}

		public void Send(Packet packet)
		{
		}
	}
}
