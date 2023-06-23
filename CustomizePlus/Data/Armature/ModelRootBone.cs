// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Extensions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    /// A fake model bone that doesn't actually correspond to a bone within a skeleton,
    /// but instead some other data that can be nonetheless be transformed LIKE a bone.
    /// </summary>
    internal unsafe class ModelRootBone : ModelBone
    {
        public ModelRootBone(Armature arm, string codeName) : base(arm, codeName, 0, 0) { }

        public override unsafe hkQsTransformf GetGameTransform(CharacterBase* cBase)
        {
            return new hkQsTransformf()
            {
                Translation = cBase->DrawObject.Object.Position.ToHavokVector(),
                Rotation = cBase->DrawObject.Object.Rotation.ToHavokQuaternion(),
                Scale = cBase->DrawObject.Object.Scale.ToHavokVector()
            };

            //return new hkQsTransformf()
            //{
            //    Translation = cBase->Skeleton->Transform.Position.ToHavokVector(),
            //    Rotation = cBase->Skeleton->Transform.Rotation.ToHavokQuaternion(),
            //    Scale = cBase->Skeleton->Transform.Scale.ToHavokVector()
            //};
        }

        protected override unsafe void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform)
        {
            cBase->DrawObject.Object.Position = transform.Translation.ToClientVector3();
            cBase->DrawObject.Object.Rotation = transform.Rotation.ToClientQuaternion();
            cBase->DrawObject.Object.Scale = transform.Scale.ToClientVector3();

            //cBase->Skeleton->Transform = new FFXIVClientStructs.FFXIV.Client.Graphics.Transform()
            //{
            //    Position = transform.Translation.ToClientVector3(),
            //    Rotation = transform.Rotation.ToClientQuaternion(),
            //    Scale = transform.Scale.ToClientVector3()
            //};
        }
    }
}
