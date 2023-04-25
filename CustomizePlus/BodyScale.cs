﻿// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Numerics;
	using CustomizePlus.Data;
	using CustomizePlus.Extensions;
	using CustomizePlus.Memory;
	using CustomizePlus.Services;
	using Dalamud.Game.ClientState.Objects.Types;

	[Serializable]
	public class BodyScale
	{
		private readonly ConcurrentDictionary<int, PoseScale> poses = new();

		public string CharacterName { get; set; } = string.Empty;
		public string ScaleName { get; set; } = string.Empty;
		public bool BodyScaleEnabled { get; set; } = true;
		public Dictionary<string, BoneEditsContainer> Bones { get; } = new();

		// This works fine on generic GameObject if previously checked for correct types.
		public unsafe void ApplyNonRootBonesAndRootScale(GameObject character, bool applyRootScale)
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

			if (!Bones.ContainsKey("n_root"))
				return;

			BoneEditsContainer rootEditsContainer = Bones["n_root"];

			// Don't apply the root scale if its not set to anything.
			if (rootEditsContainer.Scale.X != 0 || rootEditsContainer.Scale.Y != 0 || rootEditsContainer.Scale.Z != 0)
			{
				HkVector4 rootScale = obj->Scale;
				if (applyRootScale)
				{
					rootScale.X = MathF.Max(rootEditsContainer.Scale.X, 0.01f);
					rootScale.Y = MathF.Max(rootEditsContainer.Scale.Y, 0.01f);
					rootScale.Z = MathF.Max(rootEditsContainer.Scale.Z, 0.01f);
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

		public unsafe void ApplyRootPosition(GameObject character)
		{
			RenderObject* obj = null;
			obj = RenderObject.FromActor(character);
			if (obj == null)
			{
				//PluginLog.Debug($"{character.Address} missing skeleton!");
				return;
			}

			if (!Bones.ContainsKey("n_root"))
				return;

			BoneEditsContainer rootEditsContainer = Bones["n_root"];

			// Don't apply the root position if its not set to anything.
			if (rootEditsContainer.Position.X != 0 || rootEditsContainer.Position.Y != 0 || rootEditsContainer.Position.Z != 0)
			{
				HkVector4 rootPos = obj->Position;

				rootPos.X += MathF.Max(rootEditsContainer.Position.X, 0.01f);
				rootPos.Y += MathF.Max(rootEditsContainer.Position.Y, 0.01f);
				rootPos.Z += MathF.Max(rootEditsContainer.Position.Z, 0.01f);

				obj->Position = rootPos;
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

						if (boneName == null || boneName == "n_root") //root bone transforms are applied separately
							continue;

						if (this.BodyScale.Bones.TryGetValue(boneName, out var boneScale))
						{
							Transform transform = pose->Transforms[index];

							if (transform.Scale.IsApproximately(boneScale.Scale) &&
								boneScale.Position.IsApproximately(Constants.ZeroVector) &&
								boneScale.Rotation.IsApproximately(Constants.ZeroVector))
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

					string? boneName = bone.GetName();

					if (this.scaleCache.TryGetValue(index, out var boneScale))
					{
						Transform transform = pose->Transforms[index];

						// Only apply bones that are scaled, those that have a value of 1 can be left untouched to not interfere with Animations.
						if (boneScale.Scale.X != 1 || boneScale.Scale.Y != 1 || boneScale.Scale.Z != 1) {
							transform.Scale.X = boneScale.Scale.X;
							transform.Scale.Y = boneScale.Scale.Y;
							transform.Scale.Z = boneScale.Scale.Z;
						}

						//Apply position and rotation only when PosingModeDetectService does not detect posing mode
						if (GPoseService.Instance.GPoseState != GPoseState.Inside || !PosingModeDetectService.Instance.IsInPosingMode)
						{
							Quaternion newRotation =
								Quaternion.Multiply(new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W), 
								Quaternion.CreateFromYawPitchRoll(boneScale.Rotation.X * MathF.PI / 180, boneScale.Rotation.Y * MathF.PI / 180, boneScale.Rotation.Z * MathF.PI / 180));
							transform.Rotation.X = newRotation.X;
							transform.Rotation.Y = newRotation.Y;
							transform.Rotation.Z = newRotation.Z;
							transform.Rotation.W = newRotation.W;

							Vector4 adjustedPositionOffset = Vector4.Transform(boneScale.Position, newRotation);

							transform.Translation.X += adjustedPositionOffset.X;
							transform.Translation.Y += adjustedPositionOffset.Y;
							transform.Translation.Z += adjustedPositionOffset.Z;
							transform.Translation.W += adjustedPositionOffset.W;
						}

						pose->Transforms[index] = transform;
					}
				}
			}
		}
	}
}
