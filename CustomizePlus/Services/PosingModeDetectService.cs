// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Core;

namespace CustomizePlus.Services
{
    /// <summary>
    ///     Service which detects if Anamnesis/Ktisis posing mode is enabled.
    /// </summary>
    internal class PosingModeDetectService : ServiceBase<PosingModeDetectService>
    {
        // Borrowed from Ktisis:
        // If this is NOP'd, Anam posing is enabled.
        private static unsafe byte* AnamnesisFreezePosition;
        private static unsafe byte* AnamnesisFreezeRotation;
        private static unsafe byte* AnamnesisFreezeScale;

        internal static unsafe bool IsAnamnesisPositionFrozen =>
            (AnamnesisFreezePosition != null && *AnamnesisFreezePosition == 0x90)
            || *AnamnesisFreezePosition == 0x00;

        internal static unsafe bool IsAnamnesisRotationFrozen =>
            (AnamnesisFreezeRotation != null && *AnamnesisFreezeRotation == 0x90)
            || *AnamnesisFreezeRotation == 0x00;

        internal static unsafe bool IsAnamnesisScalingFrozen =>
            (AnamnesisFreezeScale != null && *AnamnesisFreezeScale == 0x90)
            || *AnamnesisFreezeScale == 0x00;

        public bool IsInPosingMode => IsAnamnesisPositionFrozen || IsAnamnesisRotationFrozen || IsAnamnesisScalingFrozen;

        public override unsafe void Start()
        {
            AnamnesisFreezePosition = (byte*)DalamudServices.SigScanner.ScanText("41 0F 29 24 12");
            AnamnesisFreezeRotation = (byte*)DalamudServices.SigScanner.ScanText("41 0F 29 5C 12 10");
            AnamnesisFreezeScale = (byte*)DalamudServices.SigScanner.ScanText("41 0F 29 44 12 20");
            base.Start();
        }
    }
}