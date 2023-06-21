// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Extensions;

using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok;
using System.Transactions;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    /// A fake model bone that doesn't actually correspond to a bone within a skeleton,
    /// but instead some other data that can be nonetheless be transformed LIKE a bone.
    /// </summary>
    internal unsafe class AliasedBone : ModelBone
    {
        private delegate hkQsTransformf TransformGetter(CharacterBase* cBase, PosingSpace refFrame);
        private delegate void TransformSetter(CharacterBase* cBase, hkQsTransformf transform, PosingSpace refFrame);

        private TransformGetter _getTransform;
        private TransformSetter _setTransform;

        private AliasedBone(Armature arm, string codeName, TransformGetter tg, TransformSetter ts) : base(arm, codeName, 0, 0)
        {
            _getTransform = tg;
            _setTransform = ts;
        }

        public static AliasedBone CreateRootBone(Armature arm, string codename)
        {
            return new AliasedBone(arm, codename, GetWholeskeletonTransform, SetWholeSkeletonTransform);
        }

        public static AliasedBone CreateWeaponBone(Armature arm, string codename)
        {
            return new AliasedBone(arm, codename, GetChildObjectTransform, SetChildObjectTransform);
        }

        public override unsafe hkQsTransformf GetGameTransform(CharacterBase* cBase, PosingSpace refFrame)
        {
            return _getTransform(cBase, refFrame);
        }

        protected override unsafe void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform, PosingSpace refFrame)
        {
            _setTransform(cBase, transform, refFrame);
        }

        //public override unsafe void ApplyModelTransform(CharacterBase* cBase)
        //{
        //    if (cBase != null
        //        && CustomizedTransform.IsEdited())
        //    {
        //        hkQsTransformf originalTransform = new hkQsTransformf()
        //        {
        //            Translation = CustomizedTransform.Translation.ToHavokVector(),
        //            Rotation = CustomizedTransform.Rotation.ToQuaternion().ToHavokRotation(),
        //            Scale = CustomizedTransform.Scaling.ToHavokVector()
        //        };

        //        cBase->Skeleton->PartialSkeletons[0]
        //            .GetHavokPose(Constants.TruePoseIndex)->ModelPose.Data[0]
        //            .Translation.Y *= originalTransform.Scale.Y;

        //        cBase->Skeleton->Transform.Position.X += CustomizedTransform.Translation.X;
        //        cBase->Skeleton->Transform.Position.Y += CustomizedTransform.Translation.Y;
        //        cBase->Skeleton->Transform.Position.Z += CustomizedTransform.Translation.Z;

        //        Quaternion newRot = cBase->DrawObject.Object.Rotation.ToHavokRotation().ToQuaternion()
        //            * CustomizedTransform.Rotation.ToQuaternion();

        //        cBase->Skeleton->Transform.Rotation.X = newRot.X;
        //        cBase->Skeleton->Transform.Rotation.Y = newRot.Y;
        //        cBase->Skeleton->Transform.Rotation.Z = newRot.Z;
        //        cBase->Skeleton->Transform.Rotation.W = newRot.W;

        //        cBase->Skeleton->Transform.Scale.X = CustomizedTransform.Scaling.X;
        //        cBase->Skeleton->Transform.Scale.Y = CustomizedTransform.Scaling.Y;
        //        cBase->Skeleton->Transform.Scale.Z = CustomizedTransform.Scaling.Z;

        //        //i.e. check to see if the scale has been modified externally

        //        //Vector3 currentScale = cBase->DrawObject.Object.Scale;
        //        //Vector3 expectedScale = _cachedScale?.HadamardMultiply(CustomizedTransform.Scaling) ?? Vector3.NegativeInfinity;


        //    }
        //}

        #region Stock accessor functions

        private static hkQsTransformf GetWholeskeletonTransform(CharacterBase* cBase, PosingSpace refFrame)
        {
            return new hkQsTransformf()
            {
                Translation = cBase->Skeleton->Transform.Position.ToHavokVector(),
                Rotation = cBase->Skeleton->Transform.Rotation.ToHavokRotation(),
                Scale = cBase->Skeleton->Transform.Scale.ToHavokVector()
            };
        }

        private static void SetWholeSkeletonTransform(CharacterBase* cBase, hkQsTransformf transform, PosingSpace refFrame)
        {
            BoneTransform original = new(GetWholeskeletonTransform(cBase, refFrame));
            BoneTransform modified = new(transform);
            BoneTransform delta = modified - original;

            BoneTransform baseTransform = new BoneTransform()
            {
                Translation = cBase->DrawObject.Object.Position,
                Rotation = cBase->DrawObject.Object.Rotation.EulerAngles,
                Scaling = cBase->DrawObject.Object.Scale
            };

            cBase->Skeleton->Transform = new FFXIVClientStructs.FFXIV.Client.Graphics.Transform()
            {
                Position = transform.Translation.GetAsNumericsVector().ToClientVector(),
                Rotation = transform.Rotation.ToQuaternion(),
                Scale = transform.Scale.GetAsNumericsVector().ToClientVector()
            };
        }

        private static hkQsTransformf GetChildObjectTransform(CharacterBase* cBase, PosingSpace refFrame)
        {
            Object* obj = cBase->DrawObject.Object.ChildObject;

            if (obj->GetObjectType() != ObjectType.CharacterBase)
            {
                return Constants.NullTransform;
            }

            Weapon* wBase = (Weapon*)obj->NextSiblingObject;

            if (wBase == null) return Constants.NullTransform;

            return new hkQsTransformf()
            {
                Translation = wBase->CharacterBase.Skeleton->Transform.Position.ToHavokVector(),
                Rotation = wBase->CharacterBase.Skeleton->Transform.Rotation.ToHavokRotation(),
                Scale = wBase->CharacterBase.Skeleton->Transform.Scale.ToHavokVector()
            };
        }
        private static void SetChildObjectTransform(CharacterBase* cBase, hkQsTransformf transform, PosingSpace refFrame)
        {
            Object* obj = cBase->DrawObject.Object.ChildObject;

            if (obj->GetObjectType() != ObjectType.CharacterBase)
                return;

            Weapon* wBase = (Weapon*)obj;

            if (wBase != null)
            {
                wBase->CharacterBase.Skeleton->Transform = new FFXIVClientStructs.FFXIV.Client.Graphics.Transform()
                {
                    Position = transform.Translation.GetAsNumericsVector().ToClientVector(),
                    Rotation = transform.Rotation.ToQuaternion(),
                    Scale = transform.Scale.GetAsNumericsVector().ToClientVector()
                };
            }
        }

        #endregion
    }
}
