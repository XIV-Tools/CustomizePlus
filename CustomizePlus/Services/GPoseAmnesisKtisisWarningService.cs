// © Customize+.
// Licensed under the MIT license.

using System.Numerics;

using CustomizePlus.Core;
using CustomizePlus.Interface;

namespace CustomizePlus.Services
{
    internal class GPoseAmnesisKtisisWarningService : ServiceBase<GPoseAmnesisKtisisWarningService>
    {
        public override void Start()
        {
            GPoseService.Instance.OnGPoseStateChange += OnGPoseStateChange;
            base.Start();
        }

        private void OnGPoseStateChange(GPoseState gposeState)
        {
            if (gposeState != GPoseState.Inside)
            {
                return;
            }

            MessageWindow.Show(
                "Several Customize+ features are not compatible with Anamnesis and Ktisis, namely bone position and rotation offsets.\n" +
                "If you are using Anamnesis, Customize+ will automatically disable them for you when needed.\n" +
                "If you are using Ktisis you will need to create separate body scale without position and rotation offsets to be used in GPose.",
                new Vector2(715, 125), null, "ana_ktisis_gpose_pos_rot_warning");
        }

        public override void Dispose()
        {
            GPoseService.Instance.OnGPoseStateChange -= OnGPoseStateChange;
        }
    }
}