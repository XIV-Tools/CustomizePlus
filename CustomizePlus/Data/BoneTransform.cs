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
        Position = 0,
        Rotation = 1,
        Scale = 2,
        FKPosition = 3,
        FKRotation = 4
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
            Translation = Vector3.Zero;
            KinematicTranslation = Vector3.Zero;

            Rotation = Vector3.Zero;
            KinematicRotation = Vector3.Zero;

            Scaling = Vector3.One;
        }

        public BoneTransform(BoneTransform original) : this()
        {
            UpdateToMatch(original);
        }

        private Vector3 _translation = Vector3.Zero;
        public Vector3 Translation
        {
            get => _translation;
            set => _translation = ClampVector(value);
        }

        private Vector3 _rotation = Vector3.Zero;
        public Vector3 Rotation
        {
            get => _rotation;
            set => _rotation = ClampAngles(value);
        }

        private Vector3 _scaling = Vector3.One;
        public Vector3 Scaling
        {
            get => _scaling;
            set => _scaling = ClampVector(value);
        }

        private Vector3 _kinematicTranslation = Vector3.Zero;
        public Vector3 KinematicTranslation
        {
            get => _kinematicTranslation;
            set => _kinematicTranslation = ClampVector(value);
        }

        private Vector3 _kinematicRotation = Vector3.Zero;
        public Vector3 KinematicRotation
        {
            get => _kinematicRotation;
            set => _kinematicRotation = ClampAngles(value);
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            //Sanitize all values on deserialization
            _translation = ClampVector(_translation);
            _rotation = ClampAngles(_rotation);
            _scaling = ClampVector(_scaling);

            _kinematicTranslation = ClampVector(_kinematicTranslation);
            _kinematicRotation = ClampAngles(_kinematicRotation);
        }

        private const float VectorUnitEpsilon = 0.00001f;
        private const float AngleUnitEpsilon = 0.1f;

        public bool IsEdited()
        {
            return !Translation.IsApproximately(Vector3.Zero, VectorUnitEpsilon)
                   || !Rotation.IsApproximately(Vector3.Zero, AngleUnitEpsilon)
                   || !Scaling.IsApproximately(Vector3.One, VectorUnitEpsilon)
                   || !KinematicTranslation.IsApproximately(Vector3.Zero, VectorUnitEpsilon)
                   || !KinematicRotation.IsApproximately(Vector3.Zero, AngleUnitEpsilon);
        }

        public BoneTransform DeepCopy()
        {
            return new BoneTransform
            {
                Translation = _translation,
                Rotation = _rotation,
                Scaling = _scaling,
                KinematicTranslation = _kinematicTranslation,
                KinematicRotation = _kinematicRotation
            };
        }

        public void UpdateAttribute(BoneAttribute which, Vector3 newValue)
        {
            switch (which)
            {
                case BoneAttribute.Position:
                    Translation = newValue;
                    break;

                case BoneAttribute.FKPosition:
                    KinematicTranslation = newValue;
                    break;

                case BoneAttribute.Rotation:
                    Rotation = newValue;
                    break;

                case BoneAttribute.FKRotation:
                    KinematicRotation = newValue;
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
            Translation = newValues.Translation;
            KinematicTranslation = newValues.KinematicTranslation;

            Rotation = newValues.Rotation;
            KinematicRotation = newValues.KinematicRotation;

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
                Translation = new Vector3(Translation.X, Translation.Y, -1 * Translation.Z),
                Rotation = new Vector3(-1 * Rotation.X, -1 * Rotation.Y, Rotation.Z),
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
                Translation = new Vector3(Translation.X, -1 * Translation.Y, Translation.Z),
                Rotation = new Vector3(Rotation.X, -1 * Rotation.Y, -1 * Rotation.Z),
                Scaling = Scaling
            };
        }

        /// <summary>
        /// Sanitize all vectors inside of this container.
        /// </summary>
        private void Sanitize()
        {
            _translation = ClampVector(_translation);
            _kinematicTranslation = ClampVector(_kinematicTranslation);

            _rotation = ClampAngles(_rotation);
            _kinematicRotation = ClampAngles(_kinematicRotation);

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

        public hkQsTransformf ModifyRotation(hkQsTransformf tr)
        {
            Quaternion newRotation = tr.Rotation.ToClientQuaternion() * Rotation.ToQuaternion();
            tr.Rotation.X = newRotation.X;
            tr.Rotation.Y = newRotation.Y;
            tr.Rotation.Z = newRotation.Z;
            tr.Rotation.W = newRotation.W;

            return tr;
        }

        public hkQsTransformf ModifyKinematicRotation(hkQsTransformf tr)
        {
            Quaternion newRotation = tr.Rotation.ToClientQuaternion() * KinematicRotation.ToQuaternion();
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

        public hkQsTransformf ModifyTranslationWithRotation(hkQsTransformf tr)
        {
            var adjustedTranslation = Vector4.Transform(Translation, tr.Rotation.ToClientQuaternion());
            tr.Translation.X += adjustedTranslation.X;
            tr.Translation.Y += adjustedTranslation.Y;
            tr.Translation.Z += adjustedTranslation.Z;
            tr.Translation.W += adjustedTranslation.W;

            return tr;
        }

        public hkQsTransformf ModifyTranslationAsIs(hkQsTransformf tr)
        {
            tr.Translation.X += Translation.X;
            tr.Translation.Y += Translation.Y;
            tr.Translation.Z += Translation.Z;

            return tr;
        }

        public hkQsTransformf ModifyKineTranslationWithRotation(hkQsTransformf tr)
        {
            var adjustedTranslation = Vector4.Transform(KinematicTranslation, tr.Rotation.ToClientQuaternion());
            tr.Translation.X += adjustedTranslation.X;
            tr.Translation.Y += adjustedTranslation.Y;
            tr.Translation.Z += adjustedTranslation.Z;
            tr.Translation.W += adjustedTranslation.W;

            return tr;
        }

        public hkQsTransformf ModifyKineTranslationAsIs(hkQsTransformf tr)
        {
            tr.Translation.X += KinematicTranslation.X;
            tr.Translation.Y += KinematicTranslation.Y;
            tr.Translation.Z += KinematicTranslation.Z;

            return tr;
        }
    }
}