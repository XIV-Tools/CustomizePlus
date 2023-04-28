// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Interface;
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

		public bool IsEdited()
		{
			return
				this.Position.X != 0 || this.Position.Y != 0 || this.Position.Z != 0 ||
				this.Rotation.X != 0 || this.Rotation.Y != 0 || this.Rotation.Z != 0 ||
				this.Scale.X != 1 || this.Scale.Y != 1 || this.Scale.Z != 1;
		}

		public BoneEditsContainer DeepCopy()
		{
			return new BoneEditsContainer { Position = Position, Rotation = Rotation, Scale = Scale };
		}

		public void UpdateVector(EditMode which, Vector3 newVec)
		{
			if (which == EditMode.Position)
			{
				this.Position = newVec;
			}
			else if (which == EditMode.Rotation)
			{
				ClampRotation(ref newVec);
				this.Rotation = newVec;
			}
			else
			{
				this.Scale = newVec;
			}
		}

		private static void ClampRotation(ref Vector3 rotVec)
		{
			static void Clamp(ref float angle)
			{
				if (angle > 180) angle -= 360;
				else if (angle < -180) angle += 360;
			}

			Clamp(ref rotVec.X);
			Clamp(ref rotVec.Y);
			Clamp(ref rotVec.Z);
		}

		public BoneEditsContainer ReflectAcrossZPlane()
		{
			return new BoneEditsContainer()
			{
				Position = new Vector3(this.Position.X, this.Position.Y, -1 * this.Position.Z),
				Rotation = new Vector3(-1 * this.Rotation.X, -1 * this.Rotation.Y, this.Rotation.Z),
				Scale = this.Scale
			};
		}

		public BoneEditsContainer ReflectIVCS()
		{
			return new BoneEditsContainer()
			{
				Position = new Vector3(this.Position.X, -1 * this.Position.Y, this.Position.Z),
				Rotation = new Vector3(this.Rotation.X, -1 * this.Rotation.Y, -1 * this.Rotation.Z),
				Scale = this.Scale
			};
		}
	}
}
