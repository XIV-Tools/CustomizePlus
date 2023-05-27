// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Extensions;
using CustomizePlus.Interface;
using CustomizePlus.Memory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Data
{
	[Serializable]
	public class BoneEditsContainer
	{
		private Vector3 _position;
		public Vector3 Position
		{
			get
			{
				return _position;
			}
			set
			{
				ClampToDefaultLimits(ref value);
				_position = value;
			}
		}

		private Vector3 _rotation;
		public Vector3 Rotation
		{
			get
			{
				return _rotation;
			}
			set
			{
				ClampRotation(ref value);
				_rotation = value;
			}
		}

		private Vector3 _scale;
		public Vector3 Scale
		{
			get
			{
				return _scale;
			}
			set
			{
				ClampToDefaultLimits(ref value);
				_scale = value;
			}
		}

		public BoneEditsContainer()
		{
			Position = new Vector3 { X = 0, Y = 0, Z = 0 };
			Rotation = new Vector3 { X = 0, Y = 0, Z = 0 };
			Scale = new Vector3 { X = 1, Y = 1, Z = 1 };
		}

		[OnDeserialized]
		internal void OnDeserialized(StreamingContext context)
		{
			Sanitize();
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
				this.Position = newVec;
			else if (which == EditMode.Rotation)
				this.Rotation = newVec;
			else
				this.Scale = newVec;
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

		/// <summary>
		/// Sanitize all vectors inside of this container
		/// </summary>
		private void Sanitize()
		{
			ClampToDefaultLimits(ref _position);
			ClampRotation(ref _rotation);
			ClampToDefaultLimits(ref _scale);
		}

		/// <summary>
		/// Clamp all vector values to be within allowed limits
		/// </summary>
		/// <param name="vector"></param>
		private void ClampToDefaultLimits(ref Vector3 vector)
		{
			vector.X = Math.Clamp(vector.X, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit);
			vector.Y = Math.Clamp(vector.Y, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit);
			vector.Z = Math.Clamp(vector.Z, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit);
		}

		private void ClampRotation(ref Vector3 vector)
		{
			static void Clamp(ref float angle)
			{
				if (angle > 180)
					angle = angle - 360;
				else if (angle < -180)
					angle =angle + 360;
			}

			Clamp(ref vector.X);
			Clamp(ref vector.Y);
			Clamp(ref vector.Z);
		}
	}
}
