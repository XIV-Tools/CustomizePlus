// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using ImGuiNET;

	public abstract class InterfaceBase : IDisposable
	{
		public bool IsOpen { get; private set; } = false;

		protected int Index { get; private set; }
		protected InterfaceManager Manager => Plugin.InterfaceManager;

		protected virtual bool SingleInstance => false;

		public virtual void Open()
		{
			if (this.SingleInstance)
			{
				InterfaceBase? instance = this.Manager.GetInterface(this.GetType());
				if (instance != null)
				{
					instance?.Focus();
					return;
				}
			}

			this.IsOpen = true;
		}

		public virtual void Focus()
		{
			// ??
		}

		public abstract void Draw();

		public virtual void Close()
		{
			this.IsOpen = false;
		}

		public virtual void Dispose()
		{
		}

		internal void DoDraw(int index)
		{
			this.Index = index;
			this.Draw();
		}
	}
}
