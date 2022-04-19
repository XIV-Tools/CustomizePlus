// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Generic;
	using CustomizePlus.Memory;
	using Dalamud.Game.ClientState.Objects.Types;
	using Dalamud.Logging;

	[Serializable]
	public class BodyScale
	{
		public string CharacterName { get; set; } = string.Empty;
		public Dictionary<string, HkVector4> Bones { get; } = new Dictionary<string, HkVector4>();

		public unsafe void Apply(Character character)
		{
			RenderSkeleton* skel = RenderSkeleton.FromActor(character);

			if (skel == null)
				return;

			for (int i = 0; i < skel->Length; i++)
			{
				this.Update(skel->PartialSkeletons[i].Pose1);
			}
		}

		private unsafe void Update(HkaPose* pose)
		{
			if (pose == null)
				return;

			int count = pose->Transforms.Count;
			for (int index = 0; index < count; index++)
			{
				HkaBone bone = pose->Skeleton->Bones[index];
				Transform transform = pose->Transforms[index];

				string? boneName = bone.GetName();

				if (boneName == null)
					continue;

				if (this.Bones.TryGetValue(boneName, out var boneScale))
				{
					transform.Scale.X = boneScale.X;
					transform.Scale.Y = boneScale.Y;
					transform.Scale.Z = boneScale.Z;

					pose->Transforms[index] = transform;
				}
			}
		}
	}
}
