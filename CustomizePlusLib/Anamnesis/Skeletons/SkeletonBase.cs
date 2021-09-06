// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib.Anamnesis.Skeletons
{
	using System;

	public abstract class SkeletonBase
	{
		public enum SkeletonParts
		{
			Body,
			Head,
		}

		public abstract string[] AnamnesisHeadBoneNames { get; }
		public abstract string[] AnamnesisBodyBoneNames { get; }

		public string GetBoneName(SkeletonParts part, int index)
		{
			string[] list = part switch
			{
				SkeletonParts.Body => this.AnamnesisBodyBoneNames,
				SkeletonParts.Head => this.AnamnesisHeadBoneNames,

				_ => throw new NotImplementedException(),
			};

			if (index >= 0 && index < list.Length)
			{
				return list[index];
			}

			throw new Exception($"Unknown bone index: {index} for part: {part} in {this.GetType().Name}");
		}
	}
}