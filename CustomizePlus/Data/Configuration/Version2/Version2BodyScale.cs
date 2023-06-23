// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace CustomizePlus.Data.Configuration.Version2
{
    [Serializable]
    public class Version2BodyScale
    {
        public static Dictionary<string, bool> BoneVisibility = new();
        public string CharacterName { get; set; } = string.Empty;
        public string ScaleName { get; set; } = string.Empty;
        public bool BodyScaleEnabled { get; set; } = true;
        public Dictionary<string, BoneEditsContainer> Bones { get; set; } = new();
    }

    [Serializable]
    public struct BoneEditsContainer
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;

        public BoneEditsContainer()
        {
        }
    }
}