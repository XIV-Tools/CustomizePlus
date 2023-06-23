// © Customize+.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace CustomizePlus.Data
{
    /// <summary>
    /// Represents a container of editable bones.
    /// </summary>
    public interface IBoneContainer
    {
        /// <summary>
        /// For each bone in the container, retrieve the selected attribute within the given posing space.
        /// </summary>
        public IEnumerable<TransformInfo> GetBoneTransformValues(BoneAttribute attribute, Armature.PosingSpace space);

        /// <summary>
        /// Given updated transformation info for a given bone (for the specific attribute, in the given posing space),
        /// update that bone's transformation values to reflect the updated info.
        /// </summary>
        public void UpdateBoneTransformValue(TransformInfo newValue, BoneUpdateMode mode, bool mirrorChanges, bool propagateChanges = false);
    }
}
