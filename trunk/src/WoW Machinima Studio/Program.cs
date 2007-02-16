using System;
using System.Collections.Generic;
using System.Text;
using WoW_Machinima_Studio.Network;

namespace WoW_Machinima_Studio
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Thanks to vWoW team.");
			WorldServer wowconnector = new WorldServer();
			wowconnector.Start();
			Console.ReadKey(false);
		}
	}
}
