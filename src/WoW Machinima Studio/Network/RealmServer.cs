using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace WoW_Machinima_Studio.Network
{
	internal struct async_packet
	{
		public bool isHeader;
		public Stream stream;
		public byte[] buffer;
		public async_packet(Stream stream, int size,bool isHeader)
		{
			this.buffer = new byte[size];
			this.stream = stream;
			this.isHeader = isHeader;
		}
	}
	enum RealmOpcode
	{
		RS_AUTH_LOGON_CHALLENGE = 0x00,
		RS_AUTH_LOGON_PROOF = 0x01,
		RS_AUTH_RECON_CHALLENGE = 0x02,
		RS_AUTH_RECON_PROOF = 0x03
	}
	enum RealmData
	{
		RD_UNKNOWN,
		RD_CLIENT_VERSION,
		RD_CLIENT_BUILD,
		RD_CLIENT_IP,
		RD_ACCOUNT_NAME,
		RD_AUTH_USERNAME,
		RD_AUTH_PASSWORD,
		RD_AUTH_HASH,
		RD_AUTH_SALT,
		RD_AUTH_N,
		RD_AUTH_ERROR
	}
	class RealmPacket
	{
		public RealmOpcode opcode;
		public Dictionary<RealmData, object> Data;
		private static byte[] N = { 0x89, 0x4B, 0x64, 0x5E, 0x89, 0xE1, 0x53, 0x5B, 
									0xBD, 0xAD, 0x5B, 0x8B, 0x29, 0x06, 0x50, 0x53, 
									0x08, 0x01, 0xB1, 0x8E, 0xBF, 0xBF, 0x5E, 0x8F, 
									0xAB, 0x3C, 0x82, 0x87, 0x2A, 0x3E, 0x9B, 0xB7 };
		private static byte[] rN = reverse(N);
		private static byte[] reverse(byte[] data)
		{
			byte[] b = (byte[])(data.Clone());
			Array.Reverse(b);
			return b;
		}
		public RealmPacket()
		{
			Data = new Dictionary<RealmData, object>();
		}
		public static RealmPacket FromStream(TcpClient client)
		{
			while (client.Available < 3)
				Thread.Sleep(10);
			Stream stream = client.GetStream();
			RealmPacket p = new RealmPacket();
			BinaryReader reader = new BinaryReader(stream);
			p.opcode = (RealmOpcode)(reader.ReadByte());
			reader.ReadByte();
			int packetSize = reader.ReadUInt16();
			while (client.Available < packetSize)
				Thread.Sleep(10);
			switch (p.opcode)
			{
				case RealmOpcode.RS_AUTH_RECON_CHALLENGE:
				case RealmOpcode.RS_AUTH_LOGON_CHALLENGE:
					reader.ReadBytes(4);
					p.Data.Add(RealmData.RD_CLIENT_VERSION, reader.ReadByte() + "." + reader.ReadByte() + "." + reader.ReadByte());
					p.Data.Add(RealmData.RD_CLIENT_BUILD, reader.ReadUInt16());
					reader.ReadBytes(16);
					p.Data.Add(RealmData.RD_CLIENT_IP, new IPAddress(reader.ReadBytes(4)));
					p.Data.Add(RealmData.RD_ACCOUNT_NAME, reader.ReadString());
					break;
				default:
					throw new InvalidDataException();
			}
			return p;
		}
		public static RealmPacket CreateAuthResponse(String name, String password)
		{
			SHA1 sha = new SHA1CryptoServiceProvider();
			byte[] user_pass = System.Text.Encoding.UTF8.GetBytes(((name + ":" + password).ToCharArray()));
			byte[] hash = sha.ComputeHash(user_pass);
			byte[] salt = new byte[32];
			new Random().NextBytes(salt);
			byte[] result = new byte[hash.Length + salt.Length];
			hash.CopyTo(result, 0);
			salt.CopyTo(result, hash.Length);
			byte[] finalhash = sha.ComputeHash(result);

			byte[] rand = new byte[20];
			new Random().NextBytes(rand);
			//BigInteger int_a = new BigInteger(reverse(finalhash));
			//BigInteger int_b = new BigInteger(reverse(N));
			BigInteger int_c = new BigInteger(new Byte[] { 7 });
			BigInteger int_d = int_c.modPow(new BigInteger(reverse(finalhash)),new BigInteger(reverse(N)));

			BigInteger K = new BigInteger(new Byte[] { 3 });
			BigInteger temp = ((K * int_d) + int_c.modPow(new BigInteger(reverse(rand)), new BigInteger(reverse(N)))) % new BigInteger(reverse(N));

			RealmPacket packet = new RealmPacket();
			packet.opcode = RealmOpcode.RS_AUTH_LOGON_CHALLENGE;
			packet.Data.Add(RealmData.RD_AUTH_ERROR, 0);
			packet.Data.Add(RealmData.RD_AUTH_HASH, reverse(temp.getBytes()));
			packet.Data.Add(RealmData.RD_AUTH_SALT, salt);
			packet.Data.Add(RealmData.RD_AUTH_N, N);
			return packet;
		}
		public byte[] ToByteArray()
		{
			byte[] buffer = new byte[0];
			switch (this.opcode)
			{
				case RealmOpcode.RS_AUTH_RECON_CHALLENGE:
				case RealmOpcode.RS_AUTH_LOGON_CHALLENGE:
					buffer = new byte[118];
					BinaryWriter writer = new BinaryWriter(new MemoryStream(buffer));
					writer.Write((byte)0);
					writer.Write((byte)0);
					writer.Write((byte)32);
					writer.Write(reverse((byte[])Data[RealmData.RD_AUTH_HASH]));
					writer.Write((byte)1);
					writer.Write((byte)7);
					writer.Write((byte)32);
					writer.Write((byte[])Data[RealmData.RD_AUTH_N]);
					writer.Write((byte[])Data[RealmData.RD_AUTH_SALT]);
					break;
			}
			return buffer;
		}
		//private void _readstream_callback(IAsyncResult ar)
		//{
		//    async_packet p = (async_packet)Convert.ChangeType(ar.AsyncState, typeof(async_packet));
		//    int n = 0;
		//    while (n != p.buffer.Length)
		//            n += p.stream.EndRead(ar);
		//    BinaryReader reader = new BinaryReader(new MemoryStream(p.buffer));
		//    if (p.isHeader)
		//    {
		//        opcode = (RealmOpcode)(reader.ReadByte());
		//        reader.ReadByte();
		//        int size = (int)reader.ReadUInt16();
		//        async_packet p2 = new async_packet(p.stream,size,false);
		//        p2.stream.BeginRead(p2.buffer, 0, p2.buffer.Length, new AsyncCallback(_readstream_callback), p2);
		//    }
		//    else
		//    {
		//        switch (opcode)
		//        {
		//            case RealmOpcode.RS_AUTH_RECON_CHALLENGE:
		//            case RealmOpcode.RS_AUTH_LOGON_CHALLENGE:

		//                //	Client Name ("WoW\0"):
		//                reader.ReadBytes(4);

		//                //	Client version:
		//                Data.Add(RealmData.RD_CLIENT_VERSION, reader.ReadByte() + "." + reader.ReadByte() + "." + reader.ReadByte());

		//                //	Client Build:
		//                Data.Add(RealmData.RD_CLIENT_BUILD, reader.ReadUInt16());

		//                //	Client Platform:
		//                reader.ReadBytes(4);

		//                //	Client OS:
		//                reader.ReadBytes(4);

		//                //	Client Area:
		//                reader.ReadBytes(4);

		//                //	Client Timezone:
		//                reader.ReadUInt32();

		//                //	Client IP:
		//                Data.Add(RealmData.RD_CLIENT_IP, new IPAddress(reader.ReadBytes(4)));

		//                //	Account name Length and Account Name:
		//                int acclen = reader.ReadByte();
		//                Data.Add(RealmData.RD_ACCOUNT_NAME, System.Text.Encoding.UTF8.GetString((reader.ReadBytes(acclen))));
		//                break;

		//        }
		//    }
		//}
	}
	class RealmServer
	{
		private TcpListener _listener;
		private TcpClient _client;
		private String _password;
		private bool _is_running;
		public RealmServer(string password)
		{
			_password = password;
			_listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 3724);
		}
		public RealmServer()
			: this("12345") 
		{ 
		}

		public void Start()
		{
			_listener.Start(1);
			_listener.BeginAcceptTcpClient(new AsyncCallback(_on_accept), _listener);
		}
		public void Stop()
		{
			_is_running = false;
			try
			{
				_client.Close();
				_listener.Stop();
			}
			catch { }
		}
		private void _on_accept(IAsyncResult ar)
		{
			_client = _listener.EndAcceptTcpClient(ar);
			if (_client == null)
				throw new Exception();
			_read_packets_loop();
		}
		private void _read_packets_loop()
		{
			_is_running = true;
			while (_is_running)
			{
				RealmPacket packet = RealmPacket.FromStream(_client);
				switch (packet.opcode)
				{
					case RealmOpcode.RS_AUTH_RECON_CHALLENGE:
					case RealmOpcode.RS_AUTH_LOGON_CHALLENGE:
						RealmPacket p = RealmPacket.CreateAuthResponse((string)packet.Data[RealmData.RD_ACCOUNT_NAME], _password);
						byte[] b = p.ToByteArray();
						_client.GetStream().Write(b,0,b.Length);
						break;
					default:
						break;
				}
			}
		}
	}
}
