using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
//using WoW_Machinima_Studio.Network;

namespace WoW_Machinima_Studio.Network
{
	/*internal struct async_packet
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
	}*/
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
		RD_AUTH_ERROR,
		RD_SRP6_A,
		RD_SRP6_M1,
		RD_SRP6_M2,
		RD_SRP6_CRC,
		RD_SRP6_KEYNUM
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
		public static RealmPacket FromStream(Stream stream)
		{
			RealmPacket p = new RealmPacket();
			BinaryReader reader = new BinaryReader(stream);
			p.opcode = (RealmOpcode)(reader.ReadByte());
			reader.ReadByte();
			int packetSize = reader.ReadUInt16();
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
				case RealmOpcode.RS_AUTH_RECON_PROOF:
				case RealmOpcode.RS_AUTH_LOGON_PROOF:
					p.Data.Add(RealmData.RD_SRP6_A, reader.ReadBytes(32));
					p.Data.Add(RealmData.RD_SRP6_M1, reader.ReadBytes(20));
					p.Data.Add(RealmData.RD_SRP6_CRC, reader.ReadBytes(20));
					p.Data.Add(RealmData.RD_SRP6_KEYNUM, reader.ReadByte());
					break;
				default:
					throw new InvalidDataException();
			}
			return p;
		}
		public static RealmPacket FromByteArray(byte[] array)
		{
			return RealmPacket.FromStream(new MemoryStream(array));
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

			RealmPacket response = new RealmPacket();
			response.opcode = RealmOpcode.RS_AUTH_LOGON_CHALLENGE;
			response.Data.Add(RealmData.RD_AUTH_ERROR, 0);
			response.Data.Add(RealmData.RD_AUTH_HASH, temp.getBytes());
			response.Data.Add(RealmData.RD_AUTH_SALT, salt);
			response.Data.Add(RealmData.RD_AUTH_N, N);
			return response;
		}
		public byte[] ToByteArray()
		{
			byte[] buffer = new byte[0];
			BinaryWriter writer;
			switch (this.opcode)
			{
				case RealmOpcode.RS_AUTH_RECON_CHALLENGE:
				case RealmOpcode.RS_AUTH_LOGON_CHALLENGE:
					buffer = new byte[119];
					writer = new BinaryWriter(new MemoryStream(buffer));
					writer.Write((byte)RealmOpcode.RS_AUTH_LOGON_CHALLENGE);
					writer.Write((byte)0);
					writer.Write((byte)0);
					writer.Write(reverse((byte[])Data[RealmData.RD_AUTH_HASH]));
					writer.Write((byte)1);
					writer.Write((byte)7);
					writer.Write((byte)32);
					writer.Write((byte[])Data[RealmData.RD_AUTH_N]);
					writer.Write((byte[])Data[RealmData.RD_AUTH_SALT]);
					writer.Write((byte)0);
					break;
				case RealmOpcode.RS_AUTH_RECON_PROOF:
				case RealmOpcode.RS_AUTH_LOGON_PROOF:
					buffer = new byte[26];
					writer = new BinaryWriter(new MemoryStream(buffer));
					writer.Write((byte)RealmOpcode.RS_AUTH_LOGON_PROOF);
					writer.Write((byte)0);
					writer.Write((byte[])Data[RealmData.RD_SRP6_M2]);
					writer.Write((uint)0);
					break;
			}
			return buffer;
		}
	}
	class RealmServer
	{
		private TcpListener _listener;
		private TcpClient _client;
		private String _password;
		private bool _is_running;
		private static byte[] N = { 0x89, 0x4B, 0x64, 0x5E, 0x89, 0xE1, 0x53, 0x5B, 
									0xBD, 0xAD, 0x5B, 0x8B, 0x29, 0x06, 0x50, 0x53, 
									0x08, 0x01, 0xB1, 0x8E, 0xBF, 0xBF, 0x5E, 0x8F, 
									0xAB, 0x3C, 0x82, 0x87, 0x2A, 0x3E, 0x9B, 0xB7 };
		private static byte[] rN = reverse(N);
		private byte[] bA;
		private byte[] bB;
		private byte[] bS;
		private byte[] bV;
		private byte[] bR;
		private static byte[] reverse(byte[] data)
		{
			byte[] b = (byte[])(data.Clone());
			Array.Reverse(b);
			return b;
		}
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
			_client.Client.Blocking = true;
			_client.Client.ReceiveTimeout = -1;
			while (_is_running)
			{
				Thread.Sleep(10);
				if ((_client == null) || !_client.Connected)
					_is_running = false;
				byte[] buffer = new byte[4];
				try
				{
					_client.Client.Receive(buffer);
					int size = (int)BitConverter.ToUInt16(buffer, 2);
					if (size == 0)
						continue;
					Array.Resize<byte>(ref buffer, buffer.Length + size);
					_client.Client.Receive(buffer,4,size,SocketFlags.None);
				}
				catch // Disconnected
				{
					break;
				}
				RealmPacket packet = RealmPacket.FromByteArray(buffer);
				
				switch (packet.opcode)
				{
					#region AUTH Packet
					case RealmOpcode.RS_AUTH_RECON_CHALLENGE:
					case RealmOpcode.RS_AUTH_LOGON_CHALLENGE:
						{
							RealmPacket response = new RealmPacket();
							byte[] bytestosend;
							SHA1 sha = new SHA1CryptoServiceProvider();
							byte[] user_pass = System.Text.Encoding.UTF8.GetBytes((("WoWMS:" + _password).ToCharArray()));
							byte[] hash = sha.ComputeHash(user_pass);
							byte[] salt = new byte[32];
							new Random().NextBytes(salt);
							byte[] result = new byte[hash.Length + salt.Length];
							hash.CopyTo(result, 0);
							salt.CopyTo(result, hash.Length);
							byte[] finalhash = sha.ComputeHash(result);

							byte[] rand = new byte[20];
							new Random().NextBytes(rand);
							bR = (byte[])rand.Clone();
							//BigInteger int_a = new BigInteger(reverse(finalhash));
							//BigInteger int_b = new BigInteger(reverse(N));
							BigInteger int_c = new BigInteger(new Byte[] { 7 });
							BigInteger int_d = int_c.modPow(new BigInteger(reverse(finalhash)), new BigInteger(reverse(N)));

							BigInteger K = new BigInteger(new Byte[] { 3 });
							BigInteger temp = ((K * int_d) + int_c.modPow(new BigInteger(reverse(rand)), new BigInteger(reverse(N)))) % new BigInteger(reverse(N));

							this.bB = temp.getBytes();

							response = new RealmPacket();
							response.opcode = RealmOpcode.RS_AUTH_LOGON_CHALLENGE;
							response.Data.Add(RealmData.RD_AUTH_ERROR, 0);
							response.Data.Add(RealmData.RD_AUTH_HASH, temp.getBytes());
							response.Data.Add(RealmData.RD_AUTH_SALT, salt);
							response.Data.Add(RealmData.RD_AUTH_N, N);
							//RealmPacket p = RealmPacket.CreateAuthResponse((string)packet.Data[RealmData.RD_ACCOUNT_NAME], _password);
							bytestosend = response.ToByteArray();
							_client.Client.Send(bytestosend);
							break;
						}
					#endregion
					#region PROOF Packet
					case RealmOpcode.RS_AUTH_RECON_PROOF:
					case RealmOpcode.RS_AUTH_LOGON_PROOF:
						{
							RealmPacket response = new RealmPacket();
							byte[] bytestosend;
							SHA1 sha = new SHA1CryptoServiceProvider();
							response = new RealmPacket();
							response.opcode = RealmOpcode.RS_AUTH_LOGON_PROOF;
							byte[] b = new byte[32 + bB.Length + 1 + 16 + 16];
							BinaryWriter writer = new BinaryWriter(new MemoryStream(b));
							//((byte[])packet.Data[RealmData.RD_SRP6_M1]).CopyTo(b, 0);
							writer.Write((byte[])packet.Data[RealmData.RD_SRP6_M1]);
							//bB.CopyTo(b, 32);
							writer.Write(bB);
							BigInteger u = new BigInteger(sha.ComputeHash(b));
							BigInteger s = ((BigInteger)(new BigInteger((byte[])(packet.Data[RealmData.RD_SRP6_M1]))
								* (new BigInteger(bV).modPow(u, new BigInteger(N))))).modPow(new BigInteger(bR), new BigInteger(N));
							byte[] t = s.getBytes();
							byte[] t1 = new byte[16];

							for (int i = 0; i < 16; i++)
								t1[i] = t[i * 2];
							writer.Write(t1);

							byte[] hash = sha.ComputeHash(b);
							byte[] vK = new byte[40];
							for (int i = 0; i < 20; i++)
							{
								vK[2 * i] = sha.ComputeHash(b)[i];
							}
							for (int i = 0; i < 16; i++)
								t1[i] = t[i * 2 + 1];
							writer.Write(t1);
							for (int i = 0; i < 20; i++)
							{
								vK[2 * i + 1] = sha.ComputeHash(b)[i];
							}
							bytestosend = response.ToByteArray();
							_client.Client.Send(bytestosend);
							break;
						}
					#endregion
					default:
						break;
				}
			}
		}
	}
}
