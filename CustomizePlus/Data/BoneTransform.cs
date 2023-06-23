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
        Scale = 2
    }

    public enum BoneUpdateMode
    {
        Position, Rotation, Scale,
        //PositionIncludingScale, ScaleIncludingPosition
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
            Rotation = Vector3.Zero;
            Scaling = Vector3.One;
        }

        public BoneTransform(BoneTransform original) : this()
        {
            UpdateToMatch(original);
        }

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
            set => _rotation = ClampAngles(value);
        }

        private Vector3 _scaling;
        public Vector3 Scaling
        {
            get => _scaling;
            set => _scaling = ClampVector(value);
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            //Sanitize all values on deserialization
            _translation = BoneTransform.ClampVector(_translation);
            _rotation = ClampAngles(_rotation);
            _scaling = BoneTransform.ClampVector(_scaling);
        }

        public bool IsEdited()
        {
            return !Translation.IsApproximately(Vector3.Zero, 0.00001f)
                   || !Rotation.IsApproximately(Vector3.Zero, 0.1f)
                   || !Scaling.IsApproximately(Vector3.One, 0.00001f);
        }

        public BoneTransform DeepCopy()
        {
            return new BoneTransform
            {
                Translation = Translation,
                Rotation = Rotation,
                Scaling = Scaling
            };
        }

        public void UpdateAttribute(BoneAttribute which, Vector3 newValue)
        {
            if (which == BoneAttribute.Position)
            {
                Translation = newValue;
            }
            else if (which == BoneAttribute.Rotation)
            {
                Rotation = newValue;
            }
            else
            {
                Scaling = newValue;
            }
        }

        public void UpdateToMatch(BoneTransform newValues)
        {
            Translation = newValues.Translation;
            Rotation = newValues.Rotation;
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
            _rotation = ClampAngles(_rotation);
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

        public hkQsTransformf ModifyExistingTransform(hkQsTransformf tr)
        {
            return ModifyExistingTranslationWithRotation(ModifyExistingRotation(ModifyExistingScale(tr)));
        }

        public hkQsTransformf ModifyExistingScale(hkQsTransformf tr)
        {
            tr.Scale.X *= Scaling.X;
            tr.Scale.Y *= Scaling.Y;
            tr.Scale.Z *= Scaling.Z;

            return tr;
        }

        public hkQsTransformf ModifyExistingRotation(hkQsTransformf tr)
        {
            Quaternion newRotation = tr.Rotation.ToClientQuaternion() * Rotation.ToQuaternion();
            tr.Rotation.X = newRotation.X;
            tr.Rotation.Y = newRotation.Y;
            tr.Rotation.Z = newRotation.Z;
            tr.Rotation.W = newRotation.W;

            return tr;
        }

        public hkQsTransformf ModifyExistingTranslationWithRotation(hkQsTransformf tr)
        {
            var adjustedTranslation = Vector4.Transform(Translation, tr.Rotation.ToClientQuaternion());
            tr.Translation.X += adjustedTranslation.X;
            tr.Translation.Y += adjustedTranslation.Y;
            tr.Translation.Z += adjustedTranslation.Z;
            tr.Translation.W += adjustedTranslation.W;

            return tr;
        }

        public hkQsTransformf ModifyExistingTranslation(hkQsTransformf tr)
        {
            tr.Translation.X += Translation.X;
            tr.Translation.Y += Translation.Y;
            tr.Translation.Z += Translation.Z;

            return tr;
        }
    }
}