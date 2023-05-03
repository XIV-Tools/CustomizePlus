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

namespace CustomizePlus.Data
{
	[Serializable]
	public class BoneTransform
	{
		//TODO if if ever becomes a point of concern, I might be able to marginally speed things up
		// by natively storing translation and scaling values as their own vector4s
		//that way the cost of translating back and forth to vector3s would be frontloaded
		//	to when the user is updating things instead of during the render loop

		public Vector3 Translation { get; set; }
		public Quaternion Rotation { get; set; }
		public Vector3 Scaling { get; set; }

		public Vector3 EulerRotation
		{
			//Use the quaternion rotation as the backing value
			//For the sake of avoiding precision errors, swap with identities when it makes sense to
			get => this.Rotation == Quaternion.Identity
				? Vector3.Zero
				: this.Rotation.ToEulerAngles();
			set => this.Rotation = value == Vector3.Zero
				? Quaternion.Identity
				: ClampRotation(value).ToQuaternion();
		}

		public BoneTransform()
		{
			Translation = Vector3.Zero;
			Rotation = Quaternion.Identity;
			Scaling = Vector3.One;
		}

		public bool IsEdited()
		{
			return this.Translation != Vector3.Zero
				|| this.Rotation != Quaternion.Identity
				|| this.Scaling != Vector3.One;
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

		public void UpdateToMatch(BoneTransform newValues)
		{
			this.Translation = newValues.Translation;
			this.Rotation = newValues.Rotation;
			this.Scaling = newValues.Scaling;
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
		/// Flip a bone's transforms from left to right, so you can use it to update its sibling.
		/// IVCS bones need to use the special reflection instead.
		/// </summary>
		public BoneTransform GetStandardReflection()
		{
			return new BoneTransform()
			{
				Translation = new Vector3(this.Translation.X, this.Translation.Y, -1 * this.Translation.Z),
				EulerRotation = new Vector3(-1 * this.EulerRotation.X, -1 * this.EulerRotation.Y, this.EulerRotation.Z),
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
				EulerRotation = new Vector3(this.EulerRotation.X, -1 * this.EulerRotation.Y, -1 * this.EulerRotation.Z),
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
		public BoneTransform ReorientKinematically(BoneTransform aggregate, Vector3 pointPos, Quaternion pointRot, Vector3 inheritedScale)
		{
			//record the initial values for later
			Vector3 originalTranslation = this.Translation - pointPos;
			Quaternion originalRotation = this.Rotation / pointRot;
			Vector3 originalScaling = this.Scaling / inheritedScale;

			//place the bone back at the origin of the transformation (in effect "undoing" those initial values)
			this.Translation = pointPos;
			this.Rotation = pointRot;

			//apply the aggregate transformation
			//canonical ordering is Scale, Rotation, Transformation
			Vector3 newScaling = Vector3.Multiply(aggregate.Scaling, this.Scaling);
			Quaternion newrotation = Quaternion.Multiply(aggregate.Rotation, this.Rotation);
			Vector3 newTranslation = Vector3.Transform(aggregate.Translation, newrotation);

			//re-apply the original transforms
			//also apply the inherited scale to the translation to represent the changed offset
			this.Scaling *= originalScaling;
			this.Rotation *= originalRotation;
			this.Translation += Vector3.Multiply(originalTranslation, inheritedScale);

			//record the new aggregated transform
			return new BoneTransform()
			{
				Translation = this.Translation,
				EulerRotation = this.EulerRotation,
				Scaling = this.Scaling
			};
		}

		/// <summary>
		/// Given a transformation represented by the given parameters, apply this transform's
		/// operations to further modify them.
		/// </summary>
		/// <param name="translation">The original translation value.</param>
		/// <param name="rotation">The original rotation value.</param>
		/// <param name="scaling">The original scalar values.</param>
		public void ModifyExistingTransformation(ref HkVector4 translation, ref HkVector4 rotation, ref HkVector4 scaling)
		{
			scaling.X *= this.Scaling.X;
			scaling.Y *= this.Scaling.Y;
			scaling.Z *= this.Scaling.Z;

			Quaternion newRotation = Quaternion.Multiply(scaling.ToQuaternion(), this.Rotation);
			rotation.X = newRotation.X;
			rotation.Y = newRotation.Y;
			rotation.Z = newRotation.Z;
			rotation.W = newRotation.W;

			Vector4 adjustedTranslation = Vector4.Transform(this.Translation, newRotation);
			translation.X += adjustedTranslation.X;
			translation.Y += adjustedTranslation.Y;
			translation.Z += adjustedTranslation.Z;
			translation.W += adjustedTranslation.W;
		}
	}
}
