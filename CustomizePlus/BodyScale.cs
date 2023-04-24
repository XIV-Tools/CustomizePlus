// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Numerics;
	using Anamnesis.Posing;
	using CustomizePlus.Data;
	using CustomizePlus.Extensions;
	using CustomizePlus.Memory;
	using CustomizePlus.Services;
	using Dalamud.Game.ClientState.Objects.Types;

	[Serializable]
	public class BodyScale
	{
		private readonly ConcurrentDictionary<int, PoseScale> poses = new();

		public string CharacterName;
		public string ScaleName;
		public bool BodyScaleEnabled;

		/// <summary>
		/// Gets a value indicating whether or not this BodyScale contains hrothgar-exclusive bones.
		/// </summary>
		public bool InclHroth;
		/// <summary>
		/// Gets a value indicating whether or not this BodyScale contains viera-exclusive bones.
		/// </summary>
		public bool InclViera;
		/// <summary>
		/// Gets a value indicating whether or not this BodyScale contains IVCS-exclusive bones.
		/// </summary>
		public bool InclIVCS;

		public Dictionary<string, BoneEditsContainer> Bones { get; private set; } = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="BodyScale"/> class.
		/// Constructs a blank BodyScale object with no bones and mostly-empty properties.
		/// </summary>
		public BodyScale()
		{
			this.poses = new ConcurrentDictionary<int, PoseScale>();

			this.CharacterName = String.Empty;
			this.ScaleName = String.Empty;
			this.BodyScaleEnabled = true;

			this.InclHroth = false;
			this.InclViera = false;
			this.InclIVCS = false;

			this.Bones = new Dictionary<string, BoneEditsContainer>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BodyScale"/> class.
		/// Copy Constructor. Performs deep copies of individual bones and poses.
		/// </summary>
		public BodyScale(BodyScale original)
		{
			this.poses = new(original.poses.ToDictionary(x => x.Key, x => x.Value.DeepCopy(this)));

			this.CharacterName = original.CharacterName;
			this.ScaleName = original.ScaleName;
			this.BodyScaleEnabled = original.BodyScaleEnabled;

			this.InclHroth = original.InclHroth;
			this.InclViera = original.InclViera;
			this.InclIVCS = original.InclIVCS;

			this.Bones = original.Bones.ToDictionary(x => x.Key, x => x.Value.DeepCopy());
		}

		/// <summary>
		/// Constructs a "default" bodyscale with standard collection of bones.
		/// </summary>
		public static BodyScale BuildDefault()
		{
			BodyScale output = new BodyScale();
			output.CharacterName = "Default";
			output.ScaleName = "Default";
			output.BodyScaleEnabled = false;

			foreach(string codename in BoneData.GetFilteredBoneCodenames(false, false, false, true))
			{
				output.Bones.Add(codename, new BoneEditsContainer());
			}

			return output;
		}

		/// <summary>
		/// Changes state of <see cref="InclHroth"/> property.
		/// Toggling on has side-effect of toggling <see cref="InclViera"/> off.
		/// </summary>
		public void ToggleHrothgarFeatures(bool active)
		{
			if (active)
			{
				this.InclHroth = true;
				this.InclViera = false;
			}
			else
			{
				this.InclHroth = false;
			}
			UpdateBoneList();
		}

		/// <summary>
		/// Changes state of <see cref="InclViera"/> property.
		/// Toggling on has side-effect of toggling <see cref="InclHroth"/> off.
		/// </summary>
		public void ToggleVieraFeatures(bool active)
		{
			if (active)
			{
				this.InclViera = true;
				this.InclHroth = false;
			}
			else
			{
				this.InclViera = false;
			}
			UpdateBoneList();
		}

		/// <summary>
		/// Changes state of <see cref="InclIVCS"/> property.
		/// </summary>
		public void ToggleIVCSFeatures(bool active)
		{
			this.InclIVCS = active;
			UpdateBoneList();
		}

		/// <summary>
		/// Returns whether or not this BodyScale contains a bone with the given codename.
		/// If it does, the BoneEditsContainer is passed out by reference.
		/// </summary>
		public bool TryGetBone(string codename, out BoneEditsContainer? result)
		{
			return this.Bones.TryGetValue(codename, out result);
		}

		/// <summary>
		/// Returns whether or not this BodyScale contains a bone that is the mirror-image of a bone with the given codename.
		/// If it does, the BoneEditsContainer is passed out by reference.
		/// </summary>
		public bool TryGetMirror(string codename, out BoneEditsContainer? result)
		{
			string? mirrorName = BoneData.GetBoneMirror(codename);
			if (mirrorName == null)
			{
				result = null;
				return false;
			}
			else
			{
				return this.TryGetBone(mirrorName, out result);
			}
		}

		/// <summary>
		/// Internally updates this BodyScale's bone list in accordance with its specified inclusions.
		/// n_root is always included.
		/// </summary>
		public void UpdateBoneList()
		{
			Dictionary<string, BoneEditsContainer> updated = new();

			foreach (string codename in BoneData.GetFilteredBoneCodenames(this.InclHroth, this.InclViera, this.InclIVCS, true))
			{
				if (this.Bones.TryGetValue(codename, out BoneEditsContainer? bEC) && bEC != null)
				{
					updated[codename] = bEC;
				}
				else
				{
					updated[codename] = new BoneEditsContainer();
				}
			}

			this.Bones = updated;
		}

		/// <summary>
		/// Returns a compact BodyScale with redundant bones removed.
		/// Bones not required by inclusions are removed, as well as any unaltered bones.
		/// </summary>
		public BodyScale GetPrunedScale()
		{
			Dictionary<string, BoneEditsContainer> pruned = new();

			foreach(string codename in BoneData.GetFilteredBoneCodenames(this.InclHroth, this.InclViera, this.InclIVCS, true))
			{
				if (this.Bones.TryGetValue(codename, out BoneEditsContainer? bEC) && bEC != null && (bEC.IsEdited() || codename == "n_root"))
				{
					pruned[codename] = bEC;
				}
			}

			BodyScale output = new BodyScale(this);

			output.Bones = pruned;

			return output;
		}

		public bool SameNamesAs(BodyScale other)
		{
			return this.CharacterName == other.CharacterName
				&& this.ScaleName == other.ScaleName;
		}

		//Makes it easier to get a sense of what we're looking at while debugging
		public override string ToString()
		{
			return $"{this.ScaleName} on {this.CharacterName}, {this.Bones.Count} bones, {(this.BodyScaleEnabled ? "ACTIVE" : "NOT active")}";
		}

		public override int GetHashCode()
		{
			return String.Concat(this.CharacterName, this.ScaleName).GetHashCode();
		}

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

			public PoseScale DeepCopy(BodyScale replacement)
			{
				PoseScale output = new PoseScale(replacement, this.Index);
				foreach(var pair in this.scaleCache)
				{
					output.scaleCache.Add(pair.Key, pair.Value.DeepCopy());
				}
				output.isInitialized = this.isInitialized;

				return output;
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
