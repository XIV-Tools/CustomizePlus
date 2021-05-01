// © Anamnesis.
// Developed by W and A Walsh.
// Licensed under the MIT license.

namespace Anamnesis.Files
{
	using System;
	using System.Collections.Generic;
	using Anamnesis.Memory;

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
				this.Position = trans.Position;
				this.Rotation = trans.Rotation;
				this.Scale = trans.Scale;
			}

			public Vector? Position { get; set; }
			public Quaternion? Rotation { get; set; }
			public Vector? Scale { get; set; }
		}
	}
}
