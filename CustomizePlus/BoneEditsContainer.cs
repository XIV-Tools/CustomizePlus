// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus
{
	[Serializable]
	public class BoneEditsContainer
	{
		public HkVector4 Position { get; set; }
		public HkVector4 Rotation { get; set; }
		public HkVector4 Scale { get; set; }

		public BoneEditsContainer()
		{
			Position = new HkVector4 { X = 0, Y = 0, W = 0, Z = 0 };
			Rotation = new HkVector4 { X = 0, Y = 0, W = 0, Z = 0 };
			Scale = new HkVector4 { X = 1, Y = 1, W = 1, Z = 1 };
		}
	}
}
