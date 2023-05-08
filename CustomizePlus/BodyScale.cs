//// © Customize+.
//// Licensed under the MIT license.

//namespace CustomizePlus
//{
//	using System;
//	using System.Collections.Concurrent;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Numerics;
//	using System.Windows.Forms;
//	using System.Xml.Linq;
//	using CustomizePlus.Data;
//	using CustomizePlus.Extensions;
//	using CustomizePlus.Memory;
//	using CustomizePlus.Services;
//	using Dalamud.Game.ClientState.Objects.Types;
//	using Dalamud.Logging;
//	using Dalamud.Utility;
//	using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

//	[Serializable]
//	public class BodyScale_
//	{
//		private readonly ConcurrentDictionary<int, PoseScale> poses = new();

//		public string CharacterName;
//		public string ScaleName;
//		public bool BodyScaleEnabled;

//		public Dictionary<string, BoneTransform> Bones { get; private set; } = new();
//		public static Dictionary<string, bool> BoneVisibility = new();

//		/// <summary>
//		/// Initializes a new instance of the <see cref="BodyScale"/> class.
//		/// Constructs a blank BodyScale object with no bones and mostly-empty properties.
//		/// </summary>
//		public BodyScale()
//		{
//			this.poses = new ConcurrentDictionary<int, PoseScale>();

//			this.CharacterName = String.Empty;
//			this.ScaleName = String.Empty;
//			this.BodyScaleEnabled = true;

//			this.Bones = new Dictionary<string, BoneTransform>();
//		}

//		/// <summary>
//		/// Initializes a new instance of the <see cref="BodyScale"/> class.
//		/// Copy Constructor. Performs deep copies of individual bones and poses.
//		/// </summary>
//		public BodyScale(BodyScale original)
//		{
//			this.poses = new(original.poses.ToDictionary(x => x.Key, x => x.Value.DeepCopy(this)));

//			this.CharacterName = original.CharacterName;
//			this.ScaleName = original.ScaleName;
//			this.BodyScaleEnabled = original.BodyScaleEnabled;

//			this.Bones = original.Bones.ToDictionary(x => x.Key, x => x.Value.DeepCopy());
//		}

//		private BodyScale(BodyScale original, Dictionary<string, BoneTransform> newBones) : this(original)
//		{
//			this.Bones = newBones;
//		}

//		/// <summary>
//		/// Constructs a "default" bodyscale with standard collection of bones.
//		/// </summary>
//		public static BodyScale BuildDefault()
//		{
//			BodyScale output = new BodyScale();
//			output.CharacterName = "Default";
//			output.ScaleName = "Default";
//			output.BodyScaleEnabled = false;

//			foreach (string codename in BoneData.GetBoneCodenames())
//			{
//				output.Bones.Add(codename, new BoneTransform());
//			}

//			return output;
//		}

//		/// <summary>
//		/// Returns whether or not this BodyScale contains a bone with the given codename.
//		/// If it does, the BoneEditsContainer is passed out by reference.
//		/// </summary>
//		public bool TryGetBone(string codename, out BoneTransform? result)
//		{
//			return this.Bones.TryGetValue(codename, out result);
//		}

//		/// <summary>
//		/// Returns whether or not this BodyScale contains a bone that is the mirror-image of a bone with the given codename.
//		/// If it does, the BoneEditsContainer is passed out by reference.
//		/// </summary>
//		public bool TryGetMirror(string codename, out BoneTransform? result)
//		{
//			string? mirrorName = BoneData.GetBoneMirror(codename);
//			if (mirrorName == null)
//			{
//				result = null;
//				return false;
//			}
//			else
//			{
//				return this.TryGetBone(mirrorName, out result);
//			}
//		}


//		/// <summary>
//		/// Fill this BodyScale's bone list with every Default bone it doesn't already have.
//		/// </summary>
//		public void CreateDefaultBoneList()
//		{
//			foreach (string codename in BoneData.GetBoneCodenames().Except(this.Bones.Keys).Where(x => BoneData.IsDefaultBone(x)))
//			{
//				this.Bones[codename] = new BoneTransform();
//			}
//		}

//		/// <summary>
//		/// Attempts to fill this BodyScale's bone list by scraping bone info from the game using
//		/// the <see cref="CharacterName"/>. Returns whether doing so was successful.
//		/// </summary>
//		public bool TryRepopulateBoneList()
//		{
//			GameObject? obj = Helpers.GameDataHelper.FindModelByName(this.CharacterName);
//			if (obj == null)
//			{
//				return false;
//			}

//			Dictionary<string, BoneTransform> newBoneList = new();
//			HashSet<string> unknownBoneNames = new();

//			try
//			{
//				unsafe
//				{
//					RenderObject* render = RenderObject.FromActor(obj);

//					for (int i = 0; i < render->Skeleton->Length; ++i)
//					{
//						var partialSkele = render->Skeleton->PartialSkeletons[i];

//						HkaPose*[] poses = new HkaPose*[]
//						{
//							partialSkele.Pose1,
//							partialSkele.Pose2,
//							partialSkele.Pose3,
//							partialSkele.Pose4
//						};

//						for (int j = 0; j < poses.Length && poses[j] != null; ++j)
//						{
//							HkaSkeleton* hkaSkele = poses[j]->Skeleton;

//							for (int k = 0; k < hkaSkele->Bones.Count; ++k)
//							{
//								HkaBone bone = hkaSkele->Bones[k];

//								if (bone.GetName() is string name && name != null)
//								{
//									if (this.Bones.TryGetValue(name, out var bec) && bec != null)
//									{
//										newBoneList[name] = bec;
//									}
//									else
//									{
//										newBoneList[name] = new BoneTransform();
//									}

//									if (BoneData.IsNewBone(name))
//									{
//										unknownBoneNames.Add(name);
//									}
//								}
//							}
//						}
//					}
//				}

//				foreach(var kvp in newBoneList)
//				{
//					this.Bones[kvp.Key] = kvp.Value;
//				}

//				BoneVisibility = this.Bones.Keys.ToDictionary(x => x, newBoneList.ContainsKey);

//				BoneData.LogNewBones(unknownBoneNames.ToArray());

//				return true;
//			}
//			catch (Exception ex)
//			{
//				PluginLog.Error($"Failed to get bones from skeleton by name: {ex}");
//			}

//			return false;
//		}

//		public Dictionary<BoneData.BoneFamily, string[]> GetBonesByFamily()
//		{
//			Dictionary<BoneData.BoneFamily, string[]> output = new();

//			foreach(BoneData.BoneFamily bf in BoneData.DisplayableFamilies.Keys)
//			{
//				string[] newEntry = this.Bones.Keys
//					.Where(x => BoneVisibility[x])
//					.Where(x => BoneData.GetBoneFamily(x) == bf)
//					.Order()
//					.ToArray();

//				if (newEntry.Any())
//				{
//					output.Add(bf, newEntry.ToArray());
//				}
//			}

//			return output;
//		}

//		/// <summary>
//		/// Returns a compact BodyScale including only bones that have edits applied to them,
//		/// except for the Root bone, which is included regardless.
//		/// </summary>
//		public BodyScale GetPrunedScale()
//		{
//			Dictionary<string, BoneTransform> pruned = new();

//			foreach(var kvp in this.Bones)
//			{
//				if (kvp.Value != null && (kvp.Value.IsEdited() || kvp.Key == "n_root"))
//				{
//					pruned.Add(kvp.Key, kvp.Value);
//				}
//			}

//			return new BodyScale(this, pruned);
//		}

//		public bool SameNamesAs(BodyScale other)
//		{
//			return this.CharacterName == other.CharacterName
//				&& this.ScaleName == other.ScaleName;
//		}

//		//Makes it easier to get a sense of what we're looking at while debugging
//		public override string ToString()
//		{
//			return $"'{this.ScaleName}' on {this.CharacterName}, {this.Bones.Count} bones, {(this.BodyScaleEnabled ? "ACTIVE" : "NOT active")}";
//		}

//		public override bool Equals(object? obj)
//		{
//			if (obj is BodyScale bs)
//			{
//				return bs.CharacterName == this.CharacterName
//					&& bs.ScaleName == this.ScaleName;
//			}

//			return false;
//		}

//		public override int GetHashCode()
//		{
//			return String.Concat(this.CharacterName, this.ScaleName).GetHashCode();
//		}


//		#region pose stuff

//		// This works fine on generic GameObject if previously checked for correct types.
//		public unsafe void ApplyNonRootBonesAndRootScale(GameObject character, bool applyRootScale)
//		{
//			RenderObject* obj = null;
//			obj = RenderObject.FromActor(character);
//			if (obj == null)
//			{
//				//PluginLog.Debug($"{character.Address} missing skeleton!");
//				return;
//			}
//			for (int i = 0; i < obj->Skeleton->Length; i++)
//			{
//				if (!this.poses.ContainsKey(i))
//					this.poses.TryAdd(i, new(this, i));

//				this.poses[i].Update(obj->Skeleton->PartialSkeletons[i].Pose1);
//			}

//			if (!Bones.ContainsKey("n_root"))
//				return;

//			BoneTransform rootEditsContainer = Bones["n_root"];

//			// Don't apply the root scale if its not set to anything.
//			if (rootEditsContainer.Scaling.X != 0 || rootEditsContainer.Scaling.Y != 0 || rootEditsContainer.Scaling.Z != 0)
//			{
//				HkVector4 rootScale = obj->Scale;
//				if (applyRootScale)
//				{
//					rootScale.X = MathF.Max(rootEditsContainer.Scaling.X, 0.01f);
//					rootScale.Y = MathF.Max(rootEditsContainer.Scaling.Y, 0.01f);
//					rootScale.Z = MathF.Max(rootEditsContainer.Scaling.Z, 0.01f);
//				}
//				else
//				{
//					rootScale.X = obj->Scale.X;
//					rootScale.Y = obj->Scale.Y;
//					rootScale.Z = obj->Scale.Z;
//				}
//				obj->Scale = rootScale;
//			}
//		}

//		public unsafe void ApplyRootPosition(GameObject character)
//		{
//			RenderObject* obj = null;
//			obj = RenderObject.FromActor(character);
//			if (obj == null)
//			{
//				//PluginLog.Debug($"{character.Address} missing skeleton!");
//				return;
//			}

//			if (!Bones.ContainsKey("n_root"))
//				return;

//			BoneTransform rootEditsContainer = Bones["n_root"];

//			// Don't apply the root position if its not set to anything.
//			if (rootEditsContainer.Translation.X != 0 || rootEditsContainer.Translation.Y != 0 || rootEditsContainer.Translation.Z != 0)
//			{
//				HkVector4 rootPos = obj->Position;

//				rootPos.X += MathF.Max(rootEditsContainer.Translation.X, 0.01f);
//				rootPos.Y += MathF.Max(rootEditsContainer.Translation.Y, 0.01f);
//				rootPos.Z += MathF.Max(rootEditsContainer.Translation.Z, 0.01f);

//				obj->Position = rootPos;
//			}
//		}

//		public void ClearCache()
//		{
//			this.poses.Clear();
//		}

//		public class PoseScale
//		{
//			public readonly BodyScale BodyScale;
//			public readonly int Index;

//			private readonly Dictionary<int, BoneTransform> scaleCache = new();


//			private bool isInitialized = false;

//			public PoseScale(BodyScale bodyScale, int index)
//			{
//				this.BodyScale = bodyScale;
//				this.Index = index;
//			}

//			public PoseScale DeepCopy(BodyScale replacement)
//			{
//				PoseScale output = new PoseScale(replacement, this.Index);
//				foreach(var pair in this.scaleCache)
//				{
//					output.scaleCache.Add(pair.Key, pair.Value.DeepCopy());
//				}
//				output.isInitialized = this.isInitialized;

//				return output;
//			}

//			public unsafe void Initialize(HkaPose* pose)
//			{
//				if (pose == null)
//					return;

//				lock (this.scaleCache)
//				{
//					this.scaleCache.Clear();

//					int count = pose->Transforms.Count;
//					for (int index = 0; index < count; index++)
//					{
//						HkaBone bone = pose->Skeleton->Bones[index];

//						string? boneName = bone.GetName();

//						if (boneName == null || boneName == "n_root") //root bone transforms are applied separately
//							continue;

//						if (this.BodyScale.Bones.TryGetValue(boneName, out var boneScale))
//						{
//							Transform transform = pose->Transforms[index];

//							if (transform.Scale.IsApproximately(boneScale.Scaling) &&
//								boneScale.Translation.IsApproximately(Vector3.Zero) &&
//								boneScale.EulerRotation.IsApproximately(Vector3.Zero))
//								continue;

//							this.scaleCache.Add(index, boneScale);
//						}
//					}
//				}

//				this.isInitialized = true;
//			}

//			public unsafe void Update(HkaPose* pose)
//			{
//				if (pose == null)
//					return;

//				if (!this.isInitialized)
//					this.Initialize(pose);

//				int count = pose->Transforms.Count;
//				for (int index = 0; index < count; index++)
//				{
//					HkaBone bone = pose->Skeleton->Bones[index];

//					string? boneName = bone.GetName();


//					if (boneName != null && BodyScale.BoneVisibility[boneName] && this.scaleCache.TryGetValue(index, out var boneScale))
//					{
//						Transform transform = pose->Transforms[index];

//						// Only apply bones that are scaled, those that have a value of 1 can be left untouched to not interfere with Animations.
//						if (boneScale.Scaling.X != 1 || boneScale.Scaling.Y != 1 || boneScale.Scaling.Z != 1) {
//							transform.Scale.X = boneScale.Scaling.X;
//							transform.Scale.Y = boneScale.Scaling.Y;
//							transform.Scale.Z = boneScale.Scaling.Z;
//						}

//						//Apply position and rotation only when PosingModeDetectService does not detect posing mode
//						if (GPoseService.Instance.GPoseState != GPoseState.Inside || !PosingModeDetectService.Instance.IsInPosingMode)
//						{
//							Quaternion newRotation =
//								Quaternion.Multiply(new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W), 
//								Quaternion.CreateFromYawPitchRoll(boneScale.EulerRotation.X * MathF.PI / 180, boneScale.EulerRotation.Y * MathF.PI / 180, boneScale.EulerRotation.Z * MathF.PI / 180));
//							transform.Rotation.X = newRotation.X;
//							transform.Rotation.Y = newRotation.Y;
//							transform.Rotation.Z = newRotation.Z;
//							transform.Rotation.W = newRotation.W;

//							Vector4 adjustedPositionOffset = Vector4.Transform(boneScale.Translation, newRotation);

//							transform.Translation.X += adjustedPositionOffset.X;
//							transform.Translation.Y += adjustedPositionOffset.Y;
//							transform.Translation.Z += adjustedPositionOffset.Z;
//							transform.Translation.W += adjustedPositionOffset.W;
//						}

//						pose->Transforms[index] = transform;
//					}
//				}
//			}
//		}

//		#endregion
//	}
//}
