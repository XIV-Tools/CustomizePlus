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
        private delegate hkQsTransformf TransformGetter(CharacterBase* cBase, ModelBone mb, PoseType refFrame);
        private delegate void TransformSetter(CharacterBase* cBase, hkQsTransformf transform, PoseType refFrame);

        private TransformGetter _getTransform;
        private TransformSetter _setTransform;

        private AliasedBone(Armature arm, string codeName, TransformGetter tg, TransformSetter ts) : base(arm, codeName, 0, 0)
        {
            _getTransform = tg;
            _setTransform = ts;
        }

        public static AliasedBone CreateRootBone(Armature arm, string codename)
        {
            return new AliasedBone(arm, codename, GetFakeRootTransform, SetFakeRootTransform);
        }

        public override unsafe hkQsTransformf GetGameTransform(CharacterBase* cBase, PoseType refFrame)
        {
            return _getTransform(cBase, this, refFrame);
        }

        protected override unsafe void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform, PoseType refFrame)
        {
            _setTransform(cBase, transform, refFrame);
        }

        public override unsafe void ApplyModelTransform(CharacterBase* cBase)
        {
            if (cBase != null
                && CustomizedTransform.IsEdited()
                && GetGameTransform(cBase, PoseType.Model) is hkQsTransformf gameTransform
                && !gameTransform.Equals(Constants.NullTransform))
            {

            }
        }


        #region Stock accessor functions

        private static hkQsTransformf GetFakeRootTransform(CharacterBase* cBase, ModelBone mb, PoseType refFrame)
        {
            return new hkQsTransformf()
            {
                Translation = cBase->Skeleton->Transform.Position.ToHavokVector(),
                Rotation = cBase->Skeleton->Transform.Rotation.ToHavokRotation(),
                Scale = cBase->Skeleton->Transform.Scale.ToHavokVector()
            };
        }

        private static void SetFakeRootTransform(CharacterBase* cBase, hkQsTransformf transform, PoseType refFrame)
        {
            //move the camera upward to adjust for rescaling of the character's height...?
            //cBase->Skeleton->PartialSkeletons[0].GetHavokPose(Constants.TruePoseIndex)->ModelPose.Data[0].Translation =
            //    new Vector3(transform.Scale.X, 1, 1).ToHavokVector();

            cBase->DrawObject.Object.Position = new Vector3()
            {
                X = transform.Translation.X,
                Y = transform.Translation.Y,
                Z = transform.Translation.Z,
            };
            //cBase->DrawObject.Object.Rotation =
            //    new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);
            //cBase->DrawObject.Object.Scale =
            //    new Vector3(transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
        }

        #endregion
    }
}
