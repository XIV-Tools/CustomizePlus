// © Anamnesis.
// Developed by W and A Walsh.
// Licensed under the MIT license.

namespace Anamnesis.Core.Memory
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using Anamnesis.Memory;
	using Serilog;

	public class AddressService
	{
		// Static offsets
		public static IntPtr ActorTable { get; private set; }
		public static IntPtr TargetManager { get; private set; }
		public static IntPtr GPoseActorTable { get; private set; }
		public static IntPtr GPoseTargetManager { get; private set; }
		public static IntPtr SkeletonFreezeScale { get; private set; }
		public static IntPtr SkeletonFreezeScale2 { get; private set; }
		public static IntPtr GposeCheck { get; private set; }   // GPoseCheckOffset
		public static IntPtr GposeCheck2 { get; private set; }   // GPoseCheck2Offset

		public static void Scan()
		{
			if (MemoryService.Process == null)
				return;

			if (MemoryService.Process.MainModule == null)
				throw new Exception("Process has no main module");

			Stopwatch sw = new Stopwatch();
			sw.Start();

			// Scan for all static addresses
			// Some signatures taken from Dalamud: https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Game/ClientState/ClientStateAddressResolver.cs
			ActorTable = GetAddressFromSignature("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 44 0F B6 83", 0);
			TargetManager = GetAddressFromSignature("48 8B 05 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? FF 50 ?? 48 85 DB", 3) + 0x80;

			SkeletonFreezeScale = GetAddressFromTextSignature("41 0F 29 44 12 20");  // SkeletonAddress4
			SkeletonFreezeScale2 = GetAddressFromTextSignature("43 0F 29 44 18 20");  // SkeletonAddress6
			
			// TODO: replace these manual CMTool offsets with signatures
			IntPtr baseAddress = MemoryService.Process.MainModule.BaseAddress;

			GPoseActorTable = baseAddress + 0x1DB9500;	// GPoseEntityOffset
			GPoseTargetManager = baseAddress + 0x1DB9500;	// GPoseEntityOffset
			GposeCheck = baseAddress + 0x1DBBD00;
			GposeCheck2 = baseAddress + 0x1DBBCE0;


			Log.Information($"Took {sw.ElapsedMilliseconds}ms to scan for addresses");
		}

		private static IntPtr GetAddressFromSignature(string signature, int offset)
		{
			if (MemoryService.Scanner == null)
				throw new Exception("No memory scanner");

			return MemoryService.Scanner.GetStaticAddressFromSig(signature, offset);
		}

		private static IntPtr GetAddressFromTextSignature(string signature)
		{
			if (MemoryService.Scanner == null)
				throw new Exception("No memory scanner");

			return MemoryService.Scanner.ScanText(signature);
		}

		private static Task GetBaseAddressFromSignature(string signature, int skip, bool moduleBase, Action<IntPtr> callback)
		{
			if (MemoryService.Scanner == null)
				throw new Exception("No memory scanner");

			return Task.Run(() =>
			{
				if (MemoryService.Process?.MainModule == null)
					return;

				IntPtr ptr = MemoryService.Scanner.ScanText(signature);

				ptr += skip;
				int offset = MemoryService.Read<int>(ptr);

				if (moduleBase)
				{
					ptr = MemoryService.Process.MainModule.BaseAddress + offset;
				}
				else
				{
					ptr += offset + 4;
				}

				callback.Invoke(ptr);
			});
		}
	}
}
