using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Helpers
{
    internal static class GameStateHelper
    {
        public static bool GameInPosingMode()
        {
            return Services.GPoseService.Instance.GPoseState == Services.GPoseState.Inside
                || Services.PosingModeDetectService.Instance.IsInPosingMode;
        }

        public static bool GameInPosingModeWithFrozenRotation()
        {
            return Services.GPoseService.Instance.GPoseState == Services.GPoseState.Inside
            || Services.PosingModeDetectService.IsAnamnesisRotationFrozen;
        }

        public static bool GameInPosingModeWithFrozenPosition()
        {
            return Services.GPoseService.Instance.GPoseState == Services.GPoseState.Inside
                || Services.PosingModeDetectService.IsAnamnesisPositionFrozen;
        }
    }
}
