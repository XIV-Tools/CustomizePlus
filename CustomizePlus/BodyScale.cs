// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Numerics;
	using CustomizePlus.Memory;
	using Dalamud.Game.ClientState.Objects.Types;
	using Dalamud.Logging;

	[Serializable]
	public class BodyScale
	{
		private readonly ConcurrentDictionary<int, PoseScale> poses = new();

		public string CharacterName { get; set; } = string.Empty;
		public string ScaleName { get; set; } = string.Empty;
		public bool BodyScaleEnabled { get; set; } = true;
		public Dictionary<string, HkVector4> Bones { get; } = new();
		public Dictionary<string, HkVector4> Offsets { get; } = new();
		public HkVector4 RootScale { get; set; } = HkVector4.Zero;

		// This works fine on generic GameObject if previously checked for correct types.
		public unsafe void Apply(GameObject character, bool applyRootScale)
		{
			RenderObject* obj = null;
			obj = RenderObject.FromActor(character);
			if (obj == null)
			{
				//PluginLog.Debug($"{character.Address} missing skeleton!");
				return;
			}
			for (int i = 0; i < obj->Skeleton->Length; i++)
			{
				if (!this.poses.ContainsKey(i))
					this.poses.TryAdd(i, new(this, i));

				this.poses[i].Update(obj->Skeleton->PartialSkeletons[i].Pose1);
			}

			// Don't apply the root scale if its not set to anything.
			if (this.RootScale.X != 0 && this.RootScale.Y != 0 && this.RootScale.Z != 0)
			{
				HkVector4 rootScale = obj->Scale;
				if (applyRootScale)
				{
					rootScale.X = MathF.Max(this.RootScale.X, 0.01f);
					rootScale.Y = MathF.Max(this.RootScale.Y, 0.01f);
					rootScale.Z = MathF.Max(this.RootScale.Z, 0.01f);
				}
				else
				{
					rootScale.X = obj->Scale.X;
					rootScale.Y = obj->Scale.Y;
					rootScale.Z = obj->Scale.Z;
				}
				obj->Scale = rootScale;
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
			private readonly Dictionary<int, HkVector4> offsetCache = new();

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

				lock (this.scaleCache) lock(this.offsetCache)
				{
					this.scaleCache.Clear();
					this.offsetCache.Clear();

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

							if (!transform.Scale.IsApproximately(boneScale, false))
									this.scaleCache.Add(index, boneScale);

						}

						if (this.BodyScale.Offsets.TryGetValue(boneName, out var boneOffset))
						{
							if (HkVector4.Zero.IsApproximately(boneOffset, false))
								continue;

							this.offsetCache.Add(index, boneOffset);
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
					Transform transform = pose->Transforms[index];

					if (this.scaleCache.TryGetValue(index, out var boneScale))
					{
						transform.Scale.X = boneScale.X;
						transform.Scale.Y = boneScale.Y;
						transform.Scale.Z = boneScale.Z;

						pose->Transforms[index] = transform;
					}

					if (this.offsetCache.TryGetValue(index, out var boneOffset))
					{
						Vector4 boneRotation = transform.Rotation.GetAsNumericsVector();
						Quaternion boneRotationQuat = new Quaternion(boneRotation.X, boneRotation.Y, boneRotation.Z, boneRotation.W);
						Vector4 adjustedBoneOffset = Vector4.Transform(boneOffset.GetAsNumericsVector(), boneRotationQuat);

						transform.Translation.X += adjustedBoneOffset.X;
						transform.Translation.Y += adjustedBoneOffset.Y;
						transform.Translation.Z += adjustedBoneOffset.Z;

						pose->Transforms[index] = transform;
					}
				}
			}
		}
	}
}
