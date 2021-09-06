// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib
{
	using System;

	public interface IMemory
	{
		IntPtr ActorTableAddress { get; }

		T Read<T>(IntPtr address)
			where T : struct;

		IntPtr ReadPtr(IntPtr address);

		void Read(IntPtr address, byte[] buffer, int length);

		void Write<T>(IntPtr address, T value)
			where T : struct;

		bool Write(IntPtr address, byte[] buffer);
	}
}
