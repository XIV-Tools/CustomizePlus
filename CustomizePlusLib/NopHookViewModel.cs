// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib.Memory
{
	using System;
	using System.Collections.Generic;
	using CustomizePlusLib;

	public class NopHookViewModel
	{
		private static readonly List<NopHookViewModel> Nops = new List<NopHookViewModel>();

		private readonly IntPtr address;
		private readonly byte[] originalValue;
		private readonly byte[] nopValue;
		private bool value;

		public NopHookViewModel(IntPtr address, int count)
		{
			this.address = address;

			this.originalValue = new byte[count];
			this.nopValue = new byte[count];

			CustomizePlusApi.Memory.Read(this.address, this.originalValue, this.originalValue.Length);

			for (int i = 0; i < count; i++)
			{
				this.nopValue[i] = 0x90;
			}
		}

		public bool Enabled
		{
			get
			{
				return this.value;
			}

			set
			{
				this.value = value;
				this.SetEnabled(value);
			}
		}

		public static void ClearAll()
		{
			for (int i = Nops.Count - 1; i >= 0; i--)
			{
				Nops[i].SetEnabled(false);
			}
		}

		public void SetEnabled(bool enabled)
		{
			this.value = enabled;

			if (enabled)
			{
				Nops.Add(this);

				// Write Nop
				CustomizePlusApi.Memory.Write(this.address, this.nopValue);
			}
			else
			{
				// Write the original value
				CustomizePlusApi.Memory.Write(this.address, this.originalValue);

				Nops.Remove(this);
			}
		}
	}
}
