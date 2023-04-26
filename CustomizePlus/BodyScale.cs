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
			//this.poses = new ConcurrentDictionary<int, PoseScale>();

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
			//this.poses = new(original.poses.ToDictionary(x => x.Key, x => x.Value.DeepCopy(this)));

			this.CharacterName = original.CharacterName;
			this.ScaleName = original.ScaleName;
			this.BodyScaleEnabled = original.BodyScaleEnabled;

			this.InclHroth = original.InclHroth;
			this.InclViera = original.InclViera;
			this.InclIVCS = original.InclIVCS;

			this.Bones = original.Bones.ToDictionary(x => x.Key, x => x.Value.DeepCopy());
		}

		private BodyScale(BodyScale original, Dictionary<string, BoneEditsContainer> newBones) : this(original)
		{
			this.Bones = newBones;

			this.InclHroth = newBones.Keys.Any(BoneData.IsHrothgarBone);
			this.InclViera = newBones.Keys.Any(BoneData.IsVieraBone);
			this.InclIVCS = newBones.Keys.Any(BoneData.IsIVCSBone);
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

			foreach (string codename in BoneData.GetStandardBoneCodenames())
			{
				output.Bones.Add(codename, new BoneEditsContainer());
			}

			return output;
		}

		/// <summary>
		/// Changes state of <see cref="InclHroth"/> property.
		/// Toggling on has side-effect of toggling <see cref="InclViera"/> off.
		/// </summary>
		public void ToggleHrothgarFeatures(bool active) => ToggleExclusive(active, ref this.InclHroth, ref this.InclViera);

		/// <summary>
		/// Changes state of <see cref="InclViera"/> property.
		/// Toggling on has side-effect of toggling <see cref="InclHroth"/> off.
		/// </summary>
		public void ToggleVieraFeatures(bool active) => ToggleExclusive(active, ref this.InclViera, ref this.InclHroth);

		/// <summary>
		/// Changes state of <see cref="InclIVCS"/> property.
		/// </summary>
		public void ToggleIVCSFeatures(bool active)
		{
			if (this.InclIVCS != active)
			{
				this.InclIVCS = active;
				UpdateBoneList();
			}
		}

		private void ToggleExclusive(bool toggleState, ref bool toggledOption, ref bool exOption)
		{
			if (toggleState != toggledOption)
			{
				if (toggleState)
				{
					toggledOption = true;
					exOption = false;
				}
				else
				{
					toggledOption = false;
				}
				this.UpdateBoneList();
			}
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
			foreach (string codename in BoneData.GetFilteredBoneCodenames(this, true).Except(this.Bones.Keys))
			{
				this.Bones[codename] = new BoneEditsContainer();
			}
		}

		public IEnumerable<BoneData.BoneFamily> GetUniqueFamilies()
		{
			//this seems less than ideal, but if it works?
			return BoneData.DisplayableFamilies.Where(x => BoneData.GetFilteredBoneCodenames(this).Any(y => BoneData.GetBoneFamily(y) == x));

			//return this.Bones.Select(x => BoneData.GetBoneFamily(x.Key)).Distinct();
		}

		/// <summary>
		/// Returns a compact BodyScale with redundant bones removed.
		/// Bones not required by inclusions are removed, as well as any unaltered bones.
		/// </summary>
		public BodyScale GetPrunedScale()
		{
			Dictionary<string, BoneEditsContainer> pruned = new();

			foreach(string codename in BoneData.GetFilteredBoneCodenames(this, true))
			{
				if (this.Bones.TryGetValue(codename, out BoneEditsContainer? bEC) && bEC != null && (bEC.IsEdited() || codename == "n_root"))
				{
					pruned[codename] = bEC;
				}
			}

			return new BodyScale(this, pruned);
		}

		public bool SameNamesAs(BodyScale other)
		{
			return this.CharacterName == other.CharacterName
				&& this.ScaleName == other.ScaleName;
		}

		//Makes it easier to get a sense of what we're looking at while debugging
		public override string ToString()
		{
			return $"'{this.ScaleName}' on {this.CharacterName}, {this.Bones.Count} bones, {(this.BodyScaleEnabled ? "ACTIVE" : "NOT active")}";
		}

		public override bool Equals(object? obj)
		{
			if (obj is BodyScale bs)
			{
				return bs.CharacterName == this.CharacterName
					&& bs.ScaleName == this.ScaleName;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return String.Concat(this.CharacterName, this.ScaleName).GetHashCode();
		}

	}
}
