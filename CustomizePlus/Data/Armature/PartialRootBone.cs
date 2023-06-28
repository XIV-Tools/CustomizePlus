// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok;

namespace CustomizePlus.Data.Armature
{
    internal unsafe class PartialRootBone : ModelBone
    {
        private ModelBone PrimaryPartialBone;

        public PartialRootBone(Armature arm, ModelBone primaryBone, string codeName, int partialIdx) : base(arm, codeName, partialIdx, 0)
        {
            PrimaryPartialBone = primaryBone;

            //partial roots don't have ACTUAL parents, but for the sake of simplicty let's
            //pretend that they're parented the same as their duplicates
            if (PrimaryPartialBone.ParentBone is ModelBone pBone && pBone != null)
            {
                AddParent(pBone.PartialSkeletonIndex, pBone.BoneIndex);
            }
        }

        protected override BoneTransform CustomizedTransform { get => PrimaryPartialBone.GetTransformation(); }

        /// <summary>
        /// Reference this partial root bone's duplicate model bone and copy its model space transform
        /// wholesale. This presumes that the duplicate model bone has first completed its own spacial calcs.
        /// </summary>
        public void ApplyOriginalTransform(CharacterBase *cBase)
        {
            SetGameTransform(cBase, PrimaryPartialBone.GetGameTransform(cBase, true), true);
        }
    }
}
