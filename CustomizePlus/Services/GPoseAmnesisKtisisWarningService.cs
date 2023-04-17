// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Core;
using CustomizePlus.Interface;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Services
{
	internal class GPoseAmnesisKtisisWarningService : ServiceBase<GPoseAmnesisKtisisWarningService>
	{
		public override unsafe void Start()
		{
			GPoseService.Instance.OnGPoseStateChange += OnGPoseStateChange;
			base.Start();
		}

		private void OnGPoseStateChange(GPoseState gposeState)
		{
			if (gposeState != GPoseState.Inside)
				return;

			MessageWindow.Show("Several Customize+ features are not compatible with Anamnesis and Ktisis, namely bone position and rotation offsets.\n" +
				"If you are using Anamnesis, Customize+ will automatically disable them for you when needed.\n" +
				"If you are using Ktisis you will need to create separate body scale without position and rotation offsets to be used in GPose.", new Vector2(715, 125), null, "ana_ktisis_gpose_pos_rot_warning");
		}

		public override void Dispose()
		{
			GPoseService.Instance.OnGPoseStateChange -= OnGPoseStateChange;
		}
	}
}
