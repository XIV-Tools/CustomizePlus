// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Interface
{
	using System;
	using System.Collections.Generic;

	public class InterfaceManager : IDisposable
	{
		private readonly List<InterfaceBase> interfaces = new List<InterfaceBase>();

		public void Draw()
		{
			if (this.interfaces.Count <= 0)
				return;

			for(int i = this.interfaces.Count - 1; i >= 0; i--)
			{
				this.interfaces[i].DoDraw(i);

				if (!this.interfaces[i].IsOpen)
				{
					this.interfaces[i].Dispose();
					this.interfaces.RemoveAt(i);
				}
			}
		}

		public void Dispose()
		{
			foreach (InterfaceBase iface in this.interfaces)
			{
				iface.Close();
				iface.Dispose();
			}

			this.interfaces.Clear();
		}

		public T Show<T>()
			where T : InterfaceBase, new()
		{
			T ui = Activator.CreateInstance<T>();
			ui.Open();

			if (ui.IsOpen)
				this.interfaces.Add(ui);

			return ui;
		}

		public InterfaceBase? GetInterface(Type type)
		{
			foreach (InterfaceBase ui in this.interfaces)
			{
				if (ui.GetType().IsAssignableTo(type))
				{
					return ui;
				}
			}

			return null;
		}
	}
}
