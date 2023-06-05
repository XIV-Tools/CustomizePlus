// © Customize+.
// Licensed under the MIT license.

using System.Linq;
using System.Numerics;

using CustomizePlus.Helpers;

namespace CustomizePlus.Data.Armature
{
    public class FrameStackManager
    {
        public enum Axis
        {
            X,
            Y,
            Z
        }

        private readonly DropoutStack<DeltaFrame> _redoStack;
        private readonly Armature _source;

        private readonly DropoutStack<DeltaFrame> _undoStack;

        public FrameStackManager(Armature armRef)
        {
            _source = armRef;

            _undoStack = new DropoutStack<DeltaFrame>(Constants.MaxUndoFrames);
            _redoStack = new DropoutStack<DeltaFrame>(Constants.MaxUndoFrames);
        }

        public void Do(string codename, BoneAttribute ba, Axis ax, Vector3 before, Vector3 after)
        {
            AddFrame(codename, ba, ax, before, after);
        }

        public void Undo()
        {
            FlipTop(_undoStack, false, _redoStack);
        }

        public void Redo()
        {
            FlipTop(_redoStack, true, _undoStack);
        }

        public bool UndoPossible()
        {
            return _undoStack.Any();
        }

        public bool RedoPossible()
        {
            return _redoStack.Any();
        }

        private void AddFrame(string codename, BoneAttribute ba, Axis ax, Vector3 before, Vector3 after)
        {
            //if we're editing the same value we just edited, then simply
            //adjust the effect of the last frame instead of adding a new one
            if (_undoStack.TryPeek(out var peekLastFrame)
                && peekLastFrame.UpdatedBoneName == codename
                && peekLastFrame.UpdatedAttribute == ba
                && peekLastFrame.UpdatedAxis == ax
                && _undoStack.TryPop(out var popLastFrame))
            {
                popLastFrame.ValueAfter = after;
                _undoStack.Push(popLastFrame);
            }
            else
            {
                var newFrame = new DeltaFrame
                {
                    UpdatedBoneName = codename,
                    UpdatedAttribute = ba,
                    UpdatedAxis = ax,
                    ValueBefore = before,
                    ValueAfter = after
                };

                _undoStack.Push(newFrame);
            }

            //editing the top undo frame doesn't really necessitate clearing the redo stack
            //but I don't think it's intuitive for it to do otherwise?
            _redoStack.Clear();
        }

        private void FlipTop(DropoutStack<DeltaFrame> from, bool forward, DropoutStack<DeltaFrame> to)
        {
            if (from.TryPop(out var frame)
                && _source.Bones.TryGetValue(frame.UpdatedBoneName, out var mb)
                && mb != null)
            {
                if (frame.UpdatedAttribute == BoneAttribute.Position)
                {
                    mb.PluginTransform.Translation = forward ? frame.ValueAfter : frame.ValueBefore;
                }
                else if (frame.UpdatedAttribute == BoneAttribute.Rotation)
                {
                    mb.PluginTransform.Rotation = forward ? frame.ValueAfter : frame.ValueBefore;
                }
                else //Scale
                {
                    mb.PluginTransform.Scaling = forward ? frame.ValueAfter : frame.ValueBefore;
                }

                to.Push(frame);
            }
        }
    }

    internal struct DeltaFrame
    {
        public string UpdatedBoneName;
        public BoneAttribute UpdatedAttribute;
        public FrameStackManager.Axis UpdatedAxis;
        public Vector3 ValueBefore;
        public Vector3 ValueAfter;
    }
}