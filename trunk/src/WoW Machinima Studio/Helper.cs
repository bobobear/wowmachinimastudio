using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace WoW_Machinima_Studio
{
	public static class Helper
	{
		public static Struct StreamToStruct<Struct>(Stream stream)
		{
			Struct retval;
			int size = Marshal.SizeOf(typeof(Struct));
			IntPtr ptr = Marshal.AllocHGlobal(size);
			if (ptr == IntPtr.Zero)
				throw new OutOfMemoryException();
			for (int i = 0; i < size; i++)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new EndOfStreamException();
				Marshal.WriteByte(ptr, i,(byte)b);
			}
			retval = (Struct)Marshal.PtrToStructure(ptr,typeof(Struct));
			Marshal.FreeHGlobal(ptr);
			return retval;
		}
		public static Stream StructToStream(Object Struct)
		{
			int size = Marshal.SizeOf(Struct);
			byte[] bytes = new byte[size];
			IntPtr ptr = Marshal.AllocHGlobal(size);
			if (ptr == IntPtr.Zero)
				throw new OutOfMemoryException();
			Marshal.StructureToPtr(Struct, ptr, false);
			for (int i = 0; i < size; i++)
			{
				bytes[i] = Marshal.ReadByte(ptr, i);
			}
			return new MemoryStream(bytes);
		}
	}
}
