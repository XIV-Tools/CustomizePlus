// © Customize+.
// Licensed under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Serialization;
using CustomizePlus.Extensions;
using CustomizePlus.Memory;
using Newtonsoft.Json;

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
        [JsonIgnore] private Vector3 _eulerRotation;

        private Vector3 _scaling;
        //TODO if if ever becomes a point of concern, I might be able to marginally speed things up
        // by natively storing translation and scaling values as their own vector4s
        //that way the cost of translating back and forth to vector3s would be frontloaded
        //	to when the user is updating things instead of during the render loop

        private Vector3 _translation;

        public BoneTransform()
        {
            Translation = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scaling = Vector3.One;
        }

        public BoneTransform(BoneTransform original)
        {
            UpdateToMatch(original);
        }

        public Vector3 Translation
        {
            get => _translation;
            set => _translation = ClampToDefaultLimits(value);
        }

        public Vector3 Rotation
        {
            get => _eulerRotation;
            set => _eulerRotation = ClampRotation(value);
        }

        public Vector3 Scaling
        {
            get => _scaling;
            set => _scaling = ClampToDefaultLimits(value);
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
        ///     Adjust the transformation to reorient the bone in space as a result of kinematic movement.
        ///     Returns a new aggregate transform that can be passed on to any of the bone's kinematic descendants.
        /// </summary>
        /// <param name="aggregate">
        ///     The aggregate transformation of every link in the kinematic chain from the origin up to this
        ///     one.
        /// </param>
        /// <param name="pointPos">The spacial origin of the transformation.</param>
        /// <param name="pointRot">The spacial orientation of the transformation at the origin.</param>
        /// <param name="inheritedScale">
        ///     The scaling values from the previous link in the kinematic chain, which will apply to this
        ///     link's translation.
        /// </param>
        public BoneTransform ReorientKinematically(BoneTransform aggregate, Vector3 pointPos, Vector3 pointRot,
            Vector3 inheritedScale)
        {
            Translation = aggregate.Translation;
            Rotation = aggregate.Rotation;
            Scaling = aggregate.Scaling;
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
        ///     Given a transformation represented by the given parameters, apply this transform's
        ///     operations to further modify them.
        /// </summary>
        public Transform ModifyExistingTransformation(Transform tr)
        {
            tr.Scale.X *= Scaling.X;
            tr.Scale.Y *= Scaling.Y;
            tr.Scale.Z *= Scaling.Z;

            var newRotation = Quaternion.Multiply(tr.Rotation.ToQuaternion(), Rotation.ToQuaternion());
            tr.Rotation.X = newRotation.X;
            tr.Rotation.Y = newRotation.Y;
            tr.Rotation.Z = newRotation.Z;
            tr.Rotation.W = newRotation.W;

            var adjustedTranslation = Vector4.Transform(Translation, newRotation);
            tr.Translation.X += adjustedTranslation.X;
            tr.Translation.Y += adjustedTranslation.Y;
            tr.Translation.Z += adjustedTranslation.Z;
            tr.Translation.W += adjustedTranslation.W;

            return tr;
        }

        /// <summary>
        ///     Clamp all vector values to be within allowed limits
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
                if (angle > 180)
                {
                    angle -= 360;
                }
                else if (angle < -180)
                {
                    angle += 360;
                }

                return angle;
            }

            rotVec.X = Clamp(rotVec.X);
            rotVec.Y = Clamp(rotVec.Y);
            rotVec.Z = Clamp(rotVec.Z);

            return rotVec;
        }
    }
}