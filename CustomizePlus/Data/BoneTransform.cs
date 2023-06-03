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
using CustomizePlus.Extensions;
using System.Transactions;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace CustomizePlus.Data
{
	//not the correct terms but they double as user-visible labels so ¯\_(ツ)_/¯
	public enum BoneAttribute
	{
		//hard-coding the backing values for legacy purposes
		Position = 0,
		Rotation = 1,
		Scale = 2
	}

	[Serializable]
	public class BoneTransform
	{
		//TODO if if ever becomes a point of concern, I might be able to marginally speed things up
		// by natively storing translation and scaling values as their own vector4s
		//that way the cost of translating back and forth to vector3s would be frontloaded
		//	to when the user is updating things instead of during the render loop

		private Vector3 _translation;
		public Vector3 Translation
		{
			get => _translation;
			set => _translation = ClampToDefaultLimits(value);
		}

		[JsonIgnore]
		private Vector3 _eulerRotation;
		public Vector3 Rotation
		{
			get => this._eulerRotation;
			set => this._eulerRotation = ClampRotation(value);
		}

		private Vector3 _scaling;
		public Vector3 Scaling
		{
			get => _scaling;
			set => _scaling = ClampToDefaultLimits(value);
		}

		public BoneTransform()
		{
			Translation = Vector3.Zero;
			Rotation = Vector3.Zero;
			Scaling = Vector3.One;
		}

		public BoneTransform(BoneTransform original)
		{
			this.UpdateToMatch(original);
		}

		[OnDeserialized]
		internal void OnDeserialized(StreamingContext context)
		{
			//Sanitize all values on deserialization
			_translation = ClampToDefaultLimits(_translation);
			_eulerRotation = ClampRotation(_eulerRotation);
			_scaling = ClampToDefaultLimits(_scaling);
		}

		public bool IsEdited()
		{
			return !this.Translation.IsApproximately(Vector3.Zero, 0.00001f)
				|| !this.Rotation.IsApproximately(Vector3.Zero, 0.1f)
				|| !this.Scaling.IsApproximately(Vector3.One, 0.00001f);
		}

		public BoneTransform DeepCopy()
		{
			return new BoneTransform()
			{
				Translation = this.Translation,
				Rotation = this.Rotation,
				Scaling = this.Scaling
			};
		}

		public void UpdateAttribute(BoneAttribute which, Vector3 newValue)
		{
			if (which == BoneAttribute.Position)
			{
				this.Translation = newValue;
			}
			else if (which == BoneAttribute.Rotation)
			{
				this.Rotation = newValue;
			}
			else
			{
				this.Scaling = newValue;
			}
		}

		public void UpdateToMatch(BoneTransform newValues)
		{
			this.Translation = newValues.Translation;
			this.Rotation = newValues.Rotation;
			this.Scaling = newValues.Scaling;
		}

		/// <summary>
		/// Flip a bone's transforms from left to right, so you can use it to update its sibling.
		/// IVCS bones need to use the special reflection instead.
		/// </summary>
		public BoneTransform GetStandardReflection()
		{
			return new BoneTransform()
			{
				Translation = new Vector3(this.Translation.X, this.Translation.Y, -1 * this.Translation.Z),
				Rotation = new Vector3(-1 * this.Rotation.X, -1 * this.Rotation.Y, this.Rotation.Z),
				Scaling = this.Scaling
			};
		}

		/// <summary>
		/// Flip a bone's transforms from left to right, so you can use it to update its sibling.
		/// IVCS bones are oriented in a system with different symmetries, so they're handled specially.
		/// </summary>
		public BoneTransform GetSpecialReflection()
		{
			return new BoneTransform()
			{
				Translation = new Vector3(this.Translation.X, -1 * this.Translation.Y, this.Translation.Z),
				Rotation = new Vector3(this.Rotation.X, -1 * this.Rotation.Y, -1 * this.Rotation.Z),
				Scaling = this.Scaling
			};
		}

		/// <summary>
		/// Adjust the transformation to reorient the bone in space as a result of kinematic movement.
		/// Returns a new aggregate transform that can be passed on to any of the bone's kinematic descendants.
		/// </summary>
		/// <param name="aggregate">The aggregate transformation of every link in the kinematic chain from the origin up to this one.</param>
		/// <param name="pointPos">The spacial origin of the transformation.</param>
		/// <param name="pointRot">The spacial orientation of the transformation at the origin.</param>
		/// <param name="inheritedScale">The scaling values from the previous link in the kinematic chain, which will apply to this link's translation.</param>
		public BoneTransform ReorientKinematically(BoneTransform aggregate, Vector3 pointPos, Vector3 pointRot, Vector3 inheritedScale)
		{
			this.Translation = aggregate.Translation;
			this.Rotation = aggregate.Rotation;
			this.Scaling = aggregate.Scaling;
			return aggregate;

			////record the initial values for later
			//Vector3 originalTranslation = this.Translation - pointPos;
			//Vector3 originalRotation = this.Rotation - pointRot;
			//Vector3 originalScaling = this.Scaling / inheritedScale;

			////place the bone back at the origin of the transformation (in effect "undoing" those initial values)
			//this.Translation = pointPos;
			//this.Rotation = pointRot;

			////apply the aggregate transformation to get new SRT values
			//Vector3 newScaling = Vector3.Multiply(aggregate.Scaling, this.Scaling);
			//Vector3 newrotation = aggregate.Rotation + this.Rotation;
			//Vector3 newTranslation = aggregate.Translation + this.Translation;

			////re-apply the original transforms
			////also apply the inherited scale to the translation to represent the changed offset
			//this.Scaling *= originalScaling;
			//this.Rotation += originalRotation;
			//this.Translation += Vector3.Multiply(originalTranslation, inheritedScale);

			//record the new aggregated transform
			//return new BoneTransform()
			//{
			//	Translation = this.Translation,
			//	Rotation = this.Rotation,
			//	Scaling = this.Scaling
			//};
		}

		/// <summary>
		/// Given a transformation represented by the given parameters, apply this transform's
		/// operations to further modify them.
		/// </summary>
		public Transform ModifyExistingTransformation(Transform tr)
		{
			tr.Scale.X *= this.Scaling.X;
			tr.Scale.Y *= this.Scaling.Y;
			tr.Scale.Z *= this.Scaling.Z;

			Quaternion newRotation = Quaternion.Multiply(tr.Rotation.ToQuaternion(), this.Rotation.ToQuaternion());
			tr.Rotation.X = newRotation.X;
			tr.Rotation.Y = newRotation.Y;
			tr.Rotation.Z = newRotation.Z;
			tr.Rotation.W = newRotation.W;

			Vector4 adjustedTranslation = Vector4.Transform(this.Translation, newRotation);
			tr.Translation.X += adjustedTranslation.X;
			tr.Translation.Y += adjustedTranslation.Y;
			tr.Translation.Z += adjustedTranslation.Z;
			tr.Translation.W += adjustedTranslation.W;

			return tr;
		}

		/// <summary>
		/// Clamp all vector values to be within allowed limits
		/// </summary>
		/// <param name="vector"></param>
		private static Vector3 ClampToDefaultLimits(Vector3 vector)
		{
			vector.X = Math.Clamp(vector.X, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit);
			vector.Y = Math.Clamp(vector.Y, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit);
			vector.Z = Math.Clamp(vector.Z, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit);

			return vector;
		}

		private static Vector3 ClampRotation(Vector3 rotVec)
		{
			static float Clamp(float angle)
			{
				if (angle > 180) angle -= 360;
				else if (angle < -180) angle += 360;

				return angle;
			}

			rotVec.X = Clamp(rotVec.X);
			rotVec.Y = Clamp(rotVec.Y);
			rotVec.Z = Clamp(rotVec.Z);

			return rotVec;
		}
	}
}
