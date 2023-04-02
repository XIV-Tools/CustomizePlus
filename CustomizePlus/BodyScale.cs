// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Numerics;
	using CustomizePlus.Helpers;
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
		public Dictionary<string, BoneEditsContainer> Bones { get; } = new();
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

			private readonly Dictionary<int, BoneEditsContainer> scaleCache = new();


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

							//TODO: this check needs to be re-written to support translation as well as rotation.
							//Or removed altogether
							/*if (transform.Translation.IsApproximately(boneScale.Position, false))
								continue;

							if (transform.Rotation.IsApproximately(boneScale.Rotation, false))
								continue;*/

							/*if (transform.Scale.IsApproximately(boneScale.Scale, false))
								continue;*/

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

					string? boneName = bone.GetName();

					if (this.scaleCache.TryGetValue(index, out var boneScale))
					{
						Transform transform = pose->Transforms[index];

						transform.Scale.X = boneScale.Scale.X;
						transform.Scale.Y = boneScale.Scale.Y;
						transform.Scale.Z = boneScale.Scale.Z;

						Vector4 originalRotationVector = transform.Rotation.GetAsNumericsVector(false);
						Quaternion originalBoneRotationQuaternion = new Quaternion(originalRotationVector.X, originalRotationVector.Y, originalRotationVector.Z, originalRotationVector.W);
						Quaternion rotationOffsetQuaternion = MathHelpers.EulerToQuaternion(new Vector3(boneScale.Rotation.X, boneScale.Rotation.Y, boneScale.Rotation.Z));
						Quaternion newRotation = rotationOffsetQuaternion * originalBoneRotationQuaternion;

						transform.Rotation.X = newRotation.X;
						transform.Rotation.Y = newRotation.Y;
						transform.Rotation.Z = newRotation.Z;
						transform.Rotation.W = newRotation.W;

						Vector4 adjustedPositionOffset = Vector4.Transform(boneScale.Position.GetAsNumericsVector(), newRotation);

						transform.Translation.X += adjustedPositionOffset.X;
						transform.Translation.Y += adjustedPositionOffset.Y;
						transform.Translation.Z += adjustedPositionOffset.Z;

						pose->Transforms[index] = transform;
					}
				}
			}
		}
	}
}
