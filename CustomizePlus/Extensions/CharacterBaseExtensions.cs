using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using CustomizePlus.Data.Armature;
using Lumina.Excel.GeneratedSheets;

namespace CustomizePlus.Extensions
{
    //Thanks to Ktisis contributors for discovering some of these previously-undocumented class members.
    public static class CharacterBaseExtensions
    {
        public static unsafe CharacterBase* GetChild1(this CharacterBase cBase)
        {
            if (cBase.DrawObject.Object.ChildObject != null)
            {
                CharacterBase* child1 = (CharacterBase*)cBase.DrawObject.Object.ChildObject;

                if (child1 != null
                    && child1->GetModelType() == CharacterBase.ModelType.Weapon
                    && child1->Skeleton->PartialSkeletonCount > 0)
                {
                    return child1;
                }
            }

            return null;
        }

        public static unsafe CharacterBase* GetChild2(this CharacterBase cBase)
        {
            CharacterBase* child1 = cBase.GetChild1();

            if (child1 != null)
            {
                CharacterBase* child2 = (CharacterBase*)child1->DrawObject.Object.NextSiblingObject;

                if (child2 != null
                    && child1 != child2
                    && child2->GetModelType() == CharacterBase.ModelType.Weapon
                    && child2->Skeleton->PartialSkeletonCount > 0)
                {
                    return child2;
                }
            }

            return null;
        }
        public static unsafe float Height(this CharacterBase cBase)
        {
            return *(float*)(new nint(&cBase) + 0x274);
        }

        private unsafe static nint GetAttachPtr(CharacterBase cBase)
        {
            return new nint(&cBase) + 0x0D0;
        }

        private unsafe static nint GetBoneAttachPtr(CharacterBase cBase)
        {
            return *(nint*)(GetAttachPtr(cBase) + 0x70);
        }

        public static unsafe uint AttachType(this CharacterBase cBase) => *(uint*)(GetAttachPtr(cBase) + 0x50);
        public static unsafe Skeleton* AttachTarget(this CharacterBase cBase) => *(Skeleton**)(GetAttachPtr(cBase) + 0x58);
        public static unsafe Skeleton* AttachParent(this CharacterBase cBase) => *(Skeleton**)(GetAttachPtr(cBase) + 0x60);
        public static unsafe uint AttachCount(this CharacterBase cBase) => *(uint*)(GetAttachPtr(cBase) + 0x68);
        public static unsafe ushort AttachBoneID(this CharacterBase cBase) => *(ushort*)(GetBoneAttachPtr(cBase) + 0x02);
        public static unsafe float AttachBoneScale(this CharacterBase cBase) => *(float*)(GetBoneAttachPtr(cBase) + 0x30);
    }
}
