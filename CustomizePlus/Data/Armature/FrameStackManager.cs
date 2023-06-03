// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace CustomizePlus.Data.Armature
{
	public class FrameStackManager
	{
		private readonly Armature Source;

		private readonly Helpers.DropoutStack<DeltaFrame> undoStack;
		private readonly Helpers.DropoutStack<DeltaFrame> redoStack;

		public enum Axis { X, Y, Z };

		public FrameStackManager(Armature armRef)
		{
			this.Source = armRef;

			this.undoStack = new Helpers.DropoutStack<DeltaFrame>(Constants.MaxUndoFrames);
			this.redoStack = new Helpers.DropoutStack<DeltaFrame>(Constants.MaxUndoFrames);
		}

		public void Do(string codename, BoneAttribute ba, Axis ax, Vector3 before, Vector3 after)
		{
			this.AddFrame(codename, ba, ax, before, after);
		}
		public void Undo()
		{
			FlipTop(this.undoStack, false, this.redoStack);
		}
		public void Redo()
		{
			FlipTop(this.redoStack, true, this.undoStack);
		}

		public bool UndoPossible() => this.undoStack.Any();
		public bool RedoPossible() => this.redoStack.Any();

		private void AddFrame(string codename, BoneAttribute ba, Axis ax, Vector3 before, Vector3 after)
		{
			//if we're editing the same value we just edited, then simply
			//adjust the effect of the last frame instead of adding a new one
			if (undoStack.TryPeek(out var peekLastFrame)
				&& peekLastFrame.UpdatedBoneName == codename
				&& peekLastFrame.UpdatedAttribute == ba
				&& peekLastFrame.UpdatedAxis == ax
				&& this.undoStack.TryPop(out var popLastFrame))
			{
				popLastFrame.ValueAfter = after;
				this.undoStack.Push(popLastFrame);
			}
			else
			{
				var newFrame = new DeltaFrame()
				{
					UpdatedBoneName = codename,
					UpdatedAttribute = ba,
					UpdatedAxis = ax,
					ValueBefore = before,
					ValueAfter = after
				};

				undoStack.Push(newFrame);
			}

			//editing the top undo frame doesn't really necessitate clearing the redo stack
			//but I don't think it's intuitive for it to do otherwise?
			redoStack.Clear();
		}

		private void FlipTop(Helpers.DropoutStack<DeltaFrame> from, bool forward, Helpers.DropoutStack<DeltaFrame> to)
		{
			if (from.TryPop(out DeltaFrame frame)
				&& this.Source.Bones.TryGetValue(frame.UpdatedBoneName, out var mb)
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
