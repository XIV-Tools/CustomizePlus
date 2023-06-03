// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Extensions;
using System;
using System.Numerics;

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
			set => _translation = ClampVector(value);
		}

		private Vector3 _rotation;
		public Vector3 Rotation
		{
			get => _rotation;
			set => _rotation = ClampRotation(value);
		}

		private Vector3 _scaling;
		public Vector3 Scaling
		{
			get => _scaling;
			set => _scaling = ClampVector(value);
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
		/// Sanitize all vectors inside of this container.
		/// </summary>
		private void Sanitize()
		{
			_translation = ClampVector(_translation);
			_rotation = ClampRotation(_rotation);
			_scaling = ClampVector(_scaling);
		}

		/// <summary>
		/// Clamp all vector values to be within allowed limits.
		/// </summary>
		private Vector3 ClampVector(Vector3 vector)
		{
			return new Vector3()
			{
				X = Math.Clamp(vector.X, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit),
				Y = Math.Clamp(vector.Y, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit),
				Z = Math.Clamp(vector.Z, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit)
			};
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

		/// <summary>
		/// Adjust the transformation to reorient the bone in space as a result of kinematic movement.
		/// Returns a new aggregate transform that can be passed on to any of the bone's kinematic descendants.
		/// </summary>
		public void ReorientKinematically(BoneTransform delta, Vector3 pointPos)
		{
			var offset = this.Translation - pointPos;
			offset = Vector3.Transform(offset, delta.Rotation.ToQuaternion());

			this.Scaling *= delta.Scaling;
			this.Rotation = Quaternion.Multiply(this.Rotation.ToQuaternion(), delta.Rotation.ToQuaternion()).ToEulerAngles();
			this.Translation = delta.Translation + pointPos + offset;

			//make a record of what this bone's original transformation values were
			//Vector3 originalTranslation = this.Translation;
			//Vector3 originalRotation = this.Rotation;
			//Vector3 originalScaling = this.Scaling;

			////undo the aggregate transformation to put the bone transform in the origin's local space
			////this.Scaling /= aggregate.Scaling;
			//this.Rotation -= aggregate.Rotation;
			//this.Translation -= aggregate.Translation;

			////perform the original transformation
			//this.Translation += pointPos;
			//this.Rotation += pointRot;
			////this.Scaling *= pointScale;

			////re-apply the aggregate to move the bone back into its original local space
			////this.Scaling *= aggregate.Scaling;
			//this.Rotation += aggregate.Rotation;
			//this.Translation += aggregate.Translation;

			////create a modified aggregate by composing it with this bone's original transformation values
			//return new BoneTransform()
			//{
			//	Translation = aggregate.Translation + originalTranslation,
			//	Rotation = aggregate.Rotation + originalRotation,
			//	Scaling = aggregate.Scaling * originalScaling
			//};
		}

		/// <summary>
		/// Given a transformation represented by the given parameters, apply this transform's
		/// operations to further modify them.
		/// </summary>
		public FFXIVClientStructs.Havok.hkQsTransformf ModifyExistingTransformation(FFXIVClientStructs.Havok.hkQsTransformf tr)
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
	}
}
