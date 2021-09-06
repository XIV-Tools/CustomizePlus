// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib.Mods
{
	using System;
	using System.Threading.Tasks;
	using CustomizePlus.GameStructs;
	using CustomizePlusLib.Anamnesis;
	using CustomizePlusLib.Anamnesis.Skeletons;

	public sealed class AnamnesisPoseBoneScaleMod : BoneScaleModBase
	{
		private readonly PoseFile pose;

		public AnamnesisPoseBoneScaleMod(string actorName, PoseFile pose)
			: base(actorName)
		{
			this.pose = pose;
		}

		protected override Task Apply(Actor actor, Skeleton skeleton)
		{
			if (this.pose.Bones == null)
				return Task.CompletedTask;

			Bones bodyBones = CustomizePlusApi.Memory.Read<Bones>(skeleton.Body);
			SkeletonBase anaSkeleton = SkeletonService.GetSkeleton(actor.Customize);

			for (int boneIndex = 0; boneIndex < bodyBones.Count; boneIndex++)
			{
				string boneName = anaSkeleton.GetBoneName(SkeletonBase.SkeletonParts.Body, boneIndex);

				// Physics bones should not be messed with
				if (boneName.StartsWith("Breast") || boneName.StartsWith("Cloth"))
					continue;

				Vector scale;

				// weapon bones forced to 1.
				if (boneName.StartsWith("Scabbard") || boneName.StartsWith("Sheathe") || boneName.StartsWith("Weapon"))
				{
					scale = Vector.One;
				}
				else
				{
					PoseFile.Bone? poseBone;
					if (!this.pose.Bones.TryGetValue(boneName, out poseBone))
						continue;

					if (poseBone == null || poseBone.Scale == null)
						continue;

					scale = (Vector)poseBone.Scale;
				}

				IntPtr bonePtr = bodyBones.TransformArray + (0x30 * boneIndex);
				Transform transform = CustomizePlusApi.Memory.Read<Transform>(bonePtr);
				transform.Scale = scale;
				CustomizePlusApi.Memory.Write(bonePtr, transform);
			}

			return Task.CompletedTask;
		}
	}
}
