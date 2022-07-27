// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using CustomizePlus.Memory;
	using Dalamud.Game.ClientState.Objects.Types;
	using Dalamud.Game.ClientState.Objects.SubKinds;
	using Dalamud.Game.ClientState.Objects.Enums;
	using Dalamud.Logging;

	[Serializable]
	public class BodyScale
	{
		private readonly ConcurrentDictionary<int, PoseScale> poses = new();

		public string CharacterName { get; set; } = string.Empty;
		public Dictionary<string, HkVector4> Bones { get; } = new();

		public unsafe void Apply(GameObject character)
		{
			RenderSkeleton* skel = RenderSkeleton.FromActor(character);

			if (skel == null)
				return;

			for (int i = 0; i < skel->Length; i++)
			{
				if (!this.poses.ContainsKey(i))
					this.poses.TryAdd(i, new(this, i));

				this.poses[i].Update(skel->PartialSkeletons[i].Pose1);
			}
		}

		public void ClearCache()
		{
			this.poses.Clear();
		}

		public class PoseScale
		{
			public readonly BodyScale BodyScale;
			public readonly int Index;

			private readonly Dictionary<int, HkVector4> scaleCache = new();

			private bool isInitialized = false;

			public PoseScale(BodyScale bodyScale, int index)
			{
				this.BodyScale = bodyScale;
				this.Index = index;
			}

			public unsafe void Initialize(HkaPose* pose)
			{
				if (pose == null)
					return;

				lock (this.scaleCache)
				{
					this.scaleCache.Clear();

					int count = pose->Transforms.Count;
					for (int index = 0; index < count; index++)
					{
						HkaBone bone = pose->Skeleton->Bones[index];

						string? boneName = bone.GetName();

						if (boneName == null)
							continue;

						if (this.BodyScale.Bones.TryGetValue(boneName, out var boneScale))
						{
							Transform transform = pose->Transforms[index];

							if (transform.Scale.IsApproximately(boneScale, false))
								continue;

							this.scaleCache.Add(index, boneScale);
						}
					}
				}

				this.isInitialized = true;
			}

			public unsafe void Update(HkaPose* pose)
			{
				if (pose == null)
					return;

				if (!this.isInitialized)
					this.Initialize(pose);

				int count = pose->Transforms.Count;
				for (int index = 0; index < count; index++)
				{
					HkaBone bone = pose->Skeleton->Bones[index];

					if (this.scaleCache.TryGetValue(index, out var boneScale))
					{
						Transform transform = pose->Transforms[index];

						transform.Scale.X = boneScale.X;
						transform.Scale.Y = boneScale.Y;
						transform.Scale.Z = boneScale.Z;

						pose->Transforms[index] = transform;
					}
				}
			}
		}
	}
}
