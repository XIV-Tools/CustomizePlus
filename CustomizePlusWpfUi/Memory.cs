// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusWpfUi
{
	using System;
	using System.Diagnostics;
	using CustomizePlusLib;

	public class Memory : IMemory
	{
		private readonly SignatureScanner scanner;
		private readonly MemoryService service;

		public Memory(Process process)
		{
			if (process.MainModule == null)
				throw new Exception();

			this.service = new MemoryService(process);
			this.scanner = new SignatureScanner(process.MainModule, this.service);
		}

		IntPtr IMemory.ActorTableAddress => this.scanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 44 0F B6 83", 0);

		public T Read<T>(IntPtr address)
			where T : struct
		{
			return this.service.Read<T>(address);
		}

		public void Read(IntPtr address, byte[] buffer, int length)
		{
			this.service.Read(address, buffer, length);
		}

		public IntPtr ReadPtr(IntPtr address)
		{
			return this.service.ReadPtr(address);
		}

		public void Write<T>(IntPtr address, T value)
			where T : struct
		{
			this.service.Write<T>(address, value);
		}

		public bool Write(IntPtr address, byte[] buffer)
		{
			return this.service.Write(address, buffer);
		}
	}
}
