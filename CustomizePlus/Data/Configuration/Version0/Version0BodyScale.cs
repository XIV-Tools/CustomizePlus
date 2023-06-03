// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using CustomizePlus.Memory;

namespace CustomizePlus.Data.Configuration.Version0
{
    [Serializable]
    public class Version0BodyScale
    {
        public string CharacterName { get; set; } = string.Empty;
        public string ScaleName { get; set; } = string.Empty;
        public bool BodyScaleEnabled { get; set; } = true;
        public Dictionary<string, HkVector4> Bones { get; } = new();
        public HkVector4 RootScale { get; set; } = HkVector4.Zero;
    }
}