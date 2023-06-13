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
                && Services.PosingModeDetectService.Instance.IsInPosingMode;
        }

    }
}
