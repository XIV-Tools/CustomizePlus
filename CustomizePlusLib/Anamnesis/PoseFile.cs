// © Anamnesis.
// Developed by W and A Walsh.
// Licensed under the MIT license.

namespace CustomizePlusLib.Anamnesis
{
	using System;
	using System.Collections.Generic;
	using CustomizePlus.GameStructs;

	public class PoseFile
	{
		public Dictionary<string, Bone?>? Bones { get; set; }

		[Serializable]
		public class Bone
		{
			public Bone()
			{
			}

			public Bone(Transform trans)
			{
				this.Scale = trans.Scale;
			}

			public Vector? Scale { get; set; }
		}
	}
}
