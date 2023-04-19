// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Core;
using CustomizePlus.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Services
{
	/// <summary>
	/// Service which detects if Anamnesis/Ktisis posing mode is enabled
	/// </summary>
	internal class PosingModeDetectService : ServiceBase<PosingModeDetectService>
	{
		// Borrowed from Ktisis:
		// If this is NOP'd, Anam posing is enabled.
		internal unsafe static byte* AnamnesisFreezePosition;
		internal unsafe static byte* AnamnesisFreezeRotation;
		internal unsafe static byte* AnamnesisFreezeScale;
		internal unsafe static bool IsAnamnesisPositionFrozen => AnamnesisFreezePosition != null && *AnamnesisFreezePosition == 0x90 || *AnamnesisFreezePosition == 0x00;
		internal unsafe static bool IsAnamnesisRotationFrozen => AnamnesisFreezeRotation != null && *AnamnesisFreezeRotation == 0x90 || *AnamnesisFreezeRotation == 0x00;
		internal unsafe static bool IsAnamnesisScalingFrozen => AnamnesisFreezeScale != null && *AnamnesisFreezeScale == 0x90 || *AnamnesisFreezeScale == 0x00;

		internal static bool IsAnamnesis => IsAnamnesisPositionFrozen || IsAnamnesisRotationFrozen || IsAnamnesisScalingFrozen;

		public bool IsInPosingMode => IsAnamnesis; //Can't detect Ktisis for now

		public override unsafe void Start()
		{
			AnamnesisFreezePosition = (byte*)DalamudServices.SigScanner.ScanText("41 0F 29 24 12");
			AnamnesisFreezeRotation = (byte*)DalamudServices.SigScanner.ScanText("41 0F 29 5C 12 10");
			AnamnesisFreezeScale = (byte*)DalamudServices.SigScanner.ScanText("41 0F 29 44 12 20");
			base.Start();
		}
	}
}
