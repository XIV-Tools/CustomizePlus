using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CustomizePlus.Data.Armature;
using Lumina.Excel.GeneratedSheets;

namespace CustomizePlus.Extensions
{
    //Thanks to Ktisis contributors for discovering some of these previously-undocumented class members.
    public static class CharacterBaseExtensions
    {
        public static unsafe float* Height(this CharacterBase cBase)
        {
            return (float*)(new nint(&cBase) + 0x274);
        }

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
    }
}
