using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CustomizePlus.Api
{
    public class VectorContractResolver : DefaultContractResolver
    {
        public static VectorContractResolver Instance { get; } = new VectorContractResolver();

        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (typeof(FFXIVClientStructs.FFXIV.Common.Math.Vector3).IsAssignableFrom(member.DeclaringType)
                && member.Name != nameof(FFXIVClientStructs.FFXIV.Common.Math.Vector3.X)
                && member.Name != nameof(FFXIVClientStructs.FFXIV.Common.Math.Vector3.Y)
                && member.Name != nameof(FFXIVClientStructs.FFXIV.Common.Math.Vector3.Z))
            {
                property.Ignored = true;
            }
            return property;
        }
    }
}
