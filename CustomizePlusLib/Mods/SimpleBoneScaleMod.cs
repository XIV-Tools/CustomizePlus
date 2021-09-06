// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib.Mods
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using CustomizePlus.GameStructs;

	public sealed class SimpleBoneScaleMod : BoneScaleModBase
	{
		public Dictionary<int, Vector> BodyBoneScales = new Dictionary<int, Vector>();
		public Dictionary<int, Vector> HeadBoneScales = new Dictionary<int, Vector>();

		public SimpleBoneScaleMod(string actorName)
			: base(actorName)
		{
		}

		protected override Task Apply(Actor actor, Skeleton skeleton)
		{
			if (skeleton.Body != IntPtr.Zero)
			{
				Apply(skeleton.Body, this.BodyBoneScales);
			}

			if (skeleton.Head != IntPtr.Zero)
			{
				Apply(skeleton.Head, this.HeadBoneScales);
			}

			return Task.CompletedTask;
		}

		private static void Apply(IntPtr bonesAddress, Dictionary<int, Vector> scales)
		{
			Bones bones = CustomizePlusApi.Memory.Read<Bones>(bonesAddress);

			Vector scale;
			for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
			{
				if (!scales.TryGetValue(boneIndex, out scale))
					continue;

				IntPtr bonePtr = bones.TransformArray + (0x30 * boneIndex);
				Transform transform = CustomizePlusApi.Memory.Read<Transform>(bonePtr);
				transform.Scale = scale;
				CustomizePlusApi.Memory.Write(bonePtr, transform);
			}
		}
	}
}
