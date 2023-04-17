// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Data
{
	[Serializable]
	public class BoneEditsContainer
	{
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }
		public Vector3 Scale { get; set; }

		public BoneEditsContainer()
		{
			Position = new Vector3 { X = 0, Y = 0, Z = 0 };
			Rotation = new Vector3 { X = 0, Y = 0, Z = 0 };
			Scale = new Vector3 { X = 1, Y = 1, Z = 1 };
		}

		public BoneEditsContainer DeepCopy()
		{
			return new BoneEditsContainer { Position = Position, Rotation = Rotation, Scale = Scale };
		}
	}
}
