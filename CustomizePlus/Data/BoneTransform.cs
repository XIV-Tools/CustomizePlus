// © Customize+.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;
using CustomizePlus.Extensions;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace CustomizePlus.Data
{
    //not the correct terms but they double as user-visible labels so ¯\_(ツ)_/¯
    public enum BoneAttribute
    {
        //hard-coding the backing values for legacy purposes
        BonePosition = 0,
        BoneRotation = 1,
        Scale = 2,
        LimbPosition = 3,
        LimbRotation = 4
    }

    [Serializable]
    public class BoneTransform
    {
        //TODO if if ever becomes a point of concern, I might be able to marginally speed things up
        //by natively storing translation and scaling values as their own vector4s
        //that way the cost of translating back and forth to vector3s would be frontloaded
        //to when the user is updating things instead of during the render loop

        public BoneTransform()
        {
            BoneTranslation = Vector3.Zero;
            BoneRotation = Vector3.Zero;
            Scaling = Vector3.One;
        }

        public BoneTransform(BoneTransform original) : this()
        {
            UpdateToMatch(original);
        }

        private Vector3 _boneTranslation;
        public Vector3 BoneTranslation
        {
            get => _boneTranslation;
            set => _boneTranslation = ClampVector(value);
        }

        private Vector3 _boneRotation;
        public Vector3 BoneRotation
        {
            get => _boneRotation;
            set => _boneRotation = ClampAngles(value);
        }

        private Vector3 _scaling;
        public Vector3 Scaling
        {
            get => _scaling;
            set => _scaling = ClampVector(value);
        }

        private Vector3 _limbTranslation;
        public Vector3 LimbTranslation
        {
            get => _limbTranslation;
            set => _limbTranslation = ClampVector(value);
        }

        private Vector3 _limbRotation;
        public Vector3 LimbRotation
        {
            get => _limbRotation;
            set => _limbRotation = value;
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            //Sanitize all values on deserialization
            _boneTranslation = ClampVector(_boneTranslation);
            _boneRotation = ClampAngles(_boneRotation);
            _scaling = ClampVector(_scaling);

            _limbTranslation = ClampVector(_limbTranslation);
            _limbRotation = ClampAngles(_limbRotation);
        }

        private const float VectorUnitEpsilon = 0.00001f;
        private const float AngleUnitEpsilon = 0.1f;

        public bool IsEdited()
        {
            return !BoneTranslation.IsApproximately(Vector3.Zero, VectorUnitEpsilon)
                   || !BoneRotation.IsApproximately(Vector3.Zero, AngleUnitEpsilon)
                   || !Scaling.IsApproximately(Vector3.One, VectorUnitEpsilon)
                   || !LimbTranslation.IsApproximately(Vector3.Zero, VectorUnitEpsilon)
                   || !LimbRotation.IsApproximately(Vector3.Zero, AngleUnitEpsilon);
        }

        public BoneTransform DeepCopy()
        {
            return new BoneTransform
            {
                BoneTranslation = BoneTranslation,
                BoneRotation = BoneRotation,
                Scaling = Scaling,
                LimbTranslation = LimbTranslation,
                LimbRotation = LimbRotation
            };
        }

        public void UpdateAttribute(BoneAttribute which, Vector3 newValue)
        {
            switch (which)
            {
                case BoneAttribute.BonePosition:
                    BoneTranslation = newValue;
                    break;

                case BoneAttribute.LimbPosition:
                    LimbTranslation = newValue;
                    break;

                case BoneAttribute.BoneRotation:
                    BoneRotation = newValue;
                    break;

                case BoneAttribute.LimbRotation:
                    LimbRotation = newValue;
                    break;

                case BoneAttribute.Scale:
                    Scaling = newValue;
                    break;

                default:
                    throw new Exception("Invalid bone attribute!?");
            }
        }

        public void UpdateToMatch(BoneTransform newValues)
        {
            BoneTranslation = newValues.BoneTranslation;
            LimbTranslation = newValues.LimbTranslation;

            BoneRotation = newValues.BoneRotation;
            LimbRotation = newValues.LimbRotation;

            Scaling = newValues.Scaling;
        }

        /// <summary>
        ///     Flip a bone's transforms from left to right, so you can use it to update its sibling.
        ///     IVCS bones need to use the special reflection instead.
        /// </summary>
        public BoneTransform GetStandardReflection()
        {
            return new BoneTransform
            {
                BoneTranslation = new Vector3(BoneTranslation.X, BoneTranslation.Y, -1 * BoneTranslation.Z),
                BoneRotation = new Vector3(-1 * BoneRotation.X, -1 * BoneRotation.Y, BoneRotation.Z),
                Scaling = Scaling
            };
        }

        /// <summary>
        ///     Flip a bone's transforms from left to right, so you can use it to update its sibling.
        ///     IVCS bones are oriented in a system with different symmetries, so they're handled specially.
        /// </summary>
        public BoneTransform GetSpecialReflection()
        {
            return new BoneTransform
            {
                BoneTranslation = new Vector3(BoneTranslation.X, -1 * BoneTranslation.Y, BoneTranslation.Z),
                BoneRotation = new Vector3(BoneRotation.X, -1 * BoneRotation.Y, -1 * BoneRotation.Z),
                Scaling = Scaling
            };
        }

        /// <summary>
        /// Sanitize all vectors inside of this container.
        /// </summary>
        private void Sanitize()
        {
            _boneTranslation = ClampVector(_boneTranslation);
            _limbTranslation = ClampVector(_limbTranslation);

            _boneRotation = ClampAngles(_boneRotation);
            _limbRotation = ClampAngles(_limbRotation);

            _scaling = ClampVector(_scaling);
        }

        /// <summary>
        /// Clamp all vector values to be within allowed limits.
        /// </summary>
        private static Vector3 ClampVector(Vector3 vector)
        {
            return new Vector3
            {
                X = Math.Clamp(vector.X, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit),
                Y = Math.Clamp(vector.Y, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit),
                Z = Math.Clamp(vector.Z, Constants.MinVectorValueLimit, Constants.MaxVectorValueLimit)
            };
        }

        private static Vector3 ClampAngles(Vector3 rotVec)
        {
            static float Clamp_Helper(float angle)
            {
                while (angle > 180)
                    angle -= 360;

                while (angle < -180)
                    angle += 360;
                
                return angle;
            }

            rotVec.X = Clamp_Helper(rotVec.X);
            rotVec.Y = Clamp_Helper(rotVec.Y);
            rotVec.Z = Clamp_Helper(rotVec.Z);

            return rotVec;
        }

        public hkQsTransformf ModifyScale(hkQsTransformf tr)
        {
            tr.Scale.X *= Scaling.X;
            tr.Scale.Y *= Scaling.Y;
            tr.Scale.Z *= Scaling.Z;

            return tr;
        }

        public hkQsTransformf ModifyBoneRotation(hkQsTransformf tr)
        {
            Quaternion newRotation = tr.Rotation.ToClientQuaternion() * BoneRotation.ToQuaternion();
            tr.Rotation.X = newRotation.X;
            tr.Rotation.Y = newRotation.Y;
            tr.Rotation.Z = newRotation.Z;
            tr.Rotation.W = newRotation.W;

            return tr;
        }

        public hkQsTransformf TransformLimbRotation(hkQsTransformf tr)
        {
            Quaternion newRotation = tr.Rotation.ToClientQuaternion() * LimbRotation.ToQuaternion();
            tr.Rotation.X = newRotation.X;
            tr.Rotation.Y = newRotation.Y;
            tr.Rotation.Z = newRotation.Z;
            tr.Rotation.W = newRotation.W;

            return tr;
        }

        //public hkQsTransformf ModifyExistingRotationWithOffset(hkQsTransformf tr)
        //{
        //    Vector3 offset = BoneTranslation;
        //    tr.Translation = (tr.Translation.ToClientVector3() - offset).ToHavokVector();

        //    tr = ModifyBoneRotation(tr);

        //    Vector3 modifiedOffset = Vector3.Transform(offset, BoneRotation.ToQuaternion());
        //    tr.Translation = (tr.Translation.ToClientVector3() + modifiedOffset).ToHavokVector();

        //    return tr;
        //}

        public hkQsTransformf ModifyBoneTranslationWithRotation(hkQsTransformf tr)
        {
            var adjustedTranslation = Vector4.Transform(BoneTranslation, tr.Rotation.ToClientQuaternion());
            tr.Translation.X += adjustedTranslation.X;
            tr.Translation.Y += adjustedTranslation.Y;
            tr.Translation.Z += adjustedTranslation.Z;
            tr.Translation.W += adjustedTranslation.W;

            return tr;
        }

        public hkQsTransformf ModifyLimbTranslation(hkQsTransformf tr)
        {
            tr.Translation.X += LimbTranslation.X;
            tr.Translation.Y += LimbTranslation.Y;
            tr.Translation.Z += LimbTranslation.Z;

            return tr;
        }

        public hkQsTransformf ModifyBoneTranslation(hkQsTransformf tr)
        {
            tr.Translation.X += BoneTranslation.X;
            tr.Translation.Y += BoneTranslation.Y;
            tr.Translation.Z += BoneTranslation.Z;

            return tr;
        }
    }
}