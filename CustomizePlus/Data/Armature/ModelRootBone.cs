// © Customize+.
// Licensed under the MIT license.

using System;
using CustomizePlus.Extensions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.Havok;

namespace CustomizePlus.Data.Armature
{
    /// <summary>
    /// A fake model bone that doesn't actually correspond to a bone within a skeleton,
    /// but instead some other data that can be nonetheless be transformed LIKE a bone.
    /// </summary>
    internal unsafe class ModelRootBone : ModelBone
    {
        //private Vector3 _cachedGamePosition = Vector3.Zero;
        //private Quaternion _cachedGameRotation = Quaternion.Identity;
        //private Vector3 _cachedGameScale = Vector3.One;

        //private Vector3 _moddedPosition;
        //private Quaternion _moddedRotation;
        //private Vector3 _moddedScale;

        public ModelRootBone(Armature arm, string codeName) : base(arm, codeName, 0, 0)
        {
            //_moddedPosition = _cachedGamePosition;
            //_moddedRotation = _cachedGameRotation;
            //_moddedScale = _cachedGameScale;
        }

        public override unsafe hkQsTransformf GetGameTransform(CharacterBase* cBase, bool? modelSpace = null)
        {
            //return new hkQsTransformf()
            //{
            //    Translation = objPosition.ToHavokVector(),
            //    Rotation = objRotation.ToHavokQuaternion(),
            //    Scale = objScale.ToHavokVector()
            //};

            return new hkQsTransformf()
            {
                Translation = cBase->Skeleton->Transform.Position.ToHavokVector(),
                Rotation = cBase->Skeleton->Transform.Rotation.ToHavokQuaternion(),
                Scale = cBase->Skeleton->Transform.Scale.ToHavokVector()
            };
        }

        protected override unsafe void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform, bool propagate)
        {
            //if (_moddedPosition != transform.Translation.ToClientVector3())
            //{
            //    _moddedPosition = transform.Translation.ToClientVector3();
            //    cBase->DrawObject.Object.Position = _moddedPosition;
            //}

            //cBase->DrawObject.Object.Position = transform.Translation.ToClientVector3();
            //cBase->DrawObject.Object.Rotation = transform.Rotation.ToClientQuaternion();
            //cBase->DrawObject.Object.Scale = transform.Scale.ToClientVector3();

            FFXIVClientStructs.FFXIV.Client.Graphics.Transform tr = new FFXIVClientStructs.FFXIV.Client.Graphics.Transform()
            {
                Position = transform.Translation.ToClientVector3(),
                Rotation = transform.Rotation.ToClientQuaternion(),
                Scale = transform.Scale.ToClientVector3()
            };

            cBase->Skeleton->Transform = tr;

            //if (cBase->AttachType() == 4 && cBase->AttachCount() == 1)
            //{
            //    cBase->Skeleton->Transform.Scale *= cBase->AttachBoneScale() * cBase->AttachParent()->Owner->Height();
            //}

            //CharacterBase* child1 = (CharacterBase*)cBase->DrawObject.Object.ChildObject;
            //if (child1 != null && child1->GetModelType() == CharacterBase.ModelType.Weapon)
            //{
            //    child1->Skeleton->Transform = tr;

            //    CharacterBase* child2 = (CharacterBase*)child1->DrawObject.Object.NextSiblingObject;
            //    if (child2 != child1 && child2 != null && child2->GetModelType() == CharacterBase.ModelType.Weapon)
            //    {
            //        child2->Skeleton->Transform = tr;
            //    }
            //}

            //?
            //cBase->VfxScale = MathF.Max(MathF.Max(transform.Scale.X, transform.Scale.Y), transform.Scale.Z);
        }
    }
}
