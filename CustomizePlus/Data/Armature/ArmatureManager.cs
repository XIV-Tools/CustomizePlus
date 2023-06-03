// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using CustomizePlus.Helpers;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;

namespace CustomizePlus.Data.Armature
{
	public sealed class ArmatureManager
	{
		private readonly HashSet<Armature> armatures = new();

		public void RenderCharacterProfiles(params Profile.CharacterProfile[] profiles)
		{
			this.RefreshActiveArmatures(profiles);
			this.RefreshArmatureVisibility();
			this.ApplyArmatureTransforms();
		}

		public unsafe void RenderArmatureByObject(GameObject obj)
		{
			if (this.armatures.FirstOrDefault(x => x.CharacterBase == obj.ToCharacterBase()) is Armature arm && arm != null)
			{
				if (arm.Visible)
				{
					arm.ApplyTransformation();
				}
			}
		}

		private void RefreshActiveArmatures(params Profile.CharacterProfile[] profiles)
		{
			foreach (var prof in profiles)
			{
				if (!this.armatures.Any(x => x.Profile == prof))
				{
					var newArm = new Armature(prof);
					armatures.Add(newArm);
					PluginLog.LogDebug($"Added '{newArm}' to cache");
				}
			}

			foreach (var arm in this.armatures.Except(profiles.Select(x => x.Armature)))
			{
				if (arm != null)
				{
					this.armatures.Remove(arm);
					PluginLog.LogDebug($"Removed '{arm}' from cache");
				}
			}
		}

		private void RefreshArmatureVisibility()
		{
			foreach (var arm in this.armatures)
			{
				//TODO this is yucky
				arm.Visible = Plugin.ProfileManager.GetEnabledProfiles().Contains(arm.Profile) && arm.TryLinkSkeleton();
			}
		}

		private void ApplyArmatureTransforms()
		{
			foreach (Armature arm in this.armatures.Where(x => x.Visible))
			{
				if (arm.GetReferenceSnap())
				{
					arm.OverrideWithReferencePose();
				}

				arm.ApplyTransformation();
			}
		}
	}
}
