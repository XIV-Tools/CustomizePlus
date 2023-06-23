﻿// © Customize+.
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
        }

        protected override BoneTransform CustomizedTransform { get => PrimaryPartialBone.GetTransformation(); }
    }
}
