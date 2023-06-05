// © Customize+.
// Licensed under the MIT license.

using System;

using FFXIVClientStructs.Havok;
//using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace CustomizePlus.Extensions
{
    internal static class TransformExtensions
    {
        public static bool Equals(this hkQsTransformf first, hkQsTransformf second)
        {
            return first.Translation.Equals(second.Translation)
                && first.Rotation.Equals(second.Rotation)
                && first.Scale.Equals(second.Scale);
        }

        public static bool IsNull(this hkQsTransformf t)
        {
            return t.Equals(Data.Constants.NullTransform);
        }

        public static hkQsTransformf ToHavokTransform(this Data.BoneTransform bt)
        {
            return new hkQsTransformf()
            {
                Translation = bt.Translation.ToHavokTranslation(),
                Rotation = bt.Rotation.ToQuaternion().ToHavokRotation(),
                Scale = bt.Scaling.ToHavokScaling()
            };
        }

        public static Data.BoneTransform ToBoneTransform(this hkQsTransformf t)
        {
            var rotVec = System.Numerics.Quaternion.Divide(t.Translation.ToQuaternion(), t.Rotation.ToQuaternion());

            return new Data.BoneTransform()
            {
                Translation = new(rotVec.X / rotVec.W, rotVec.Y / rotVec.W, rotVec.Z / rotVec.W),
                Rotation = t.Rotation.ToQuaternion().ToEulerAngles(),
                Scaling = new(t.Scale.X, t.Scale.Y, t.Scale.Z)
            };
        }

        public static hkVector4f GetAttribute(this hkQsTransformf t, Data.BoneAttribute att)
        {
            return att switch
            {
                Data.BoneAttribute.Position => t.Translation,
                Data.BoneAttribute.Rotation => t.Rotation.ToQuaternion().GetAsNumericsVector().ToHavokVector(),
                Data.BoneAttribute.Scale => t.Scale,
                _ => throw new NotImplementedException()
            };
        }
    }
}
