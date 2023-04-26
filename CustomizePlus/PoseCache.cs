// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Data;
using CustomizePlus.Extensions;
using CustomizePlus.Memory;
using CustomizePlus.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CustomizePlus.BodyScale;
using Dalamud.Game.ClientState.Objects.Types;

namespace CustomizePlus
{
	public static class PoseCache
	{
		private static readonly ConcurrentDictionary<int, PoseScale> Poses = new();


		// This works fine on generic GameObject if previously checked for correct types.
		public static unsafe void ApplyNonRootBonesAndRootScale(GameObject character, BodyScale? bs, bool applyRootScale)
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
				if (bs != null && !Poses.ContainsKey(i))
					Poses.TryAdd(i, new(bs, i));

				Poses[i].Update(obj->Skeleton->PartialSkeletons[i].Pose1);
			}

			if (!bs.Bones.ContainsKey("n_root"))
				return;

			BoneEditsContainer rootEditsContainer = bs.Bones["n_root"];

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

		public static unsafe void ApplyRootPosition(GameObject character, BodyScale? bs)
		{
			RenderObject* obj = null;
			obj = RenderObject.FromActor(character);
			if (obj == null)
			{
				//PluginLog.Debug($"{character.Address} missing skeleton!");
				return;
			}

			if (bs == null || !bs.Bones.ContainsKey("n_root"))
				return;

			BoneEditsContainer rootEditsContainer = bs.Bones["n_root"];

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

		public static void ClearCache()
		{
			Poses.Clear();
		}

		private class PoseScale
		{
			public readonly BodyScale BodyScaleRef;
			public readonly int Index;

			private readonly Dictionary<int, BoneEditsContainer> scaleCache = new();


			private bool isInitialized = false;

			public PoseScale(BodyScale bodyScale, int index)
			{
				this.BodyScaleRef = bodyScale;
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

						if (this.BodyScaleRef.Bones.TryGetValue(boneName, out var boneScale))
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

					if (this.scaleCache.TryGetValue(index, out BoneEditsContainer? boneEdits) && boneEdits != null)
					{
						Transform transform = pose->Transforms[index];

						// Only apply bones that are scaled, those that have a value of 1 can be left untouched to not interfere with Animations.
						if (boneEdits.Scale.X != 1 || boneEdits.Scale.Y != 1 || boneEdits.Scale.Z != 1)
						{
							transform.Scale.X = boneEdits.Scale.X;
							transform.Scale.Y = boneEdits.Scale.Y;
							transform.Scale.Z = boneEdits.Scale.Z;
						}

						//Apply position and rotation only when PosingModeDetectService does not detect posing mode
						if (GPoseService.Instance.GPoseState != GPoseState.Inside || !PosingModeDetectService.Instance.IsInPosingMode)
						{
							Quaternion newRotation =
								Quaternion.Multiply(new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W),
								Quaternion.CreateFromYawPitchRoll(boneEdits.Rotation.X * MathF.PI / 180, boneEdits.Rotation.Y * MathF.PI / 180, boneEdits.Rotation.Z * MathF.PI / 180));
							transform.Rotation.X = newRotation.X;
							transform.Rotation.Y = newRotation.Y;
							transform.Rotation.Z = newRotation.Z;
							transform.Rotation.W = newRotation.W;

							Vector4 adjustedPositionOffset = Vector4.Transform(boneEdits.Position, newRotation);

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
