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
			//todo: do not allow more than a single instance of the same window?
			T ui = Activator.CreateInstance<T>();
			ui.Open();

			if (ui.IsOpen)
				this.interfaces.Add(ui);

			return ui;
		}

		// Added this so you can close with /customize. This may need a rework in the future to access the broader usecase of the rest of the methods. But this should serve for now?
		public void Toggle<T>()
		where T : InterfaceBase, new() {
			if (this.interfaces.Count <= 0)
				new InvalidOperationException("Interfaces is empty.");

			// Close all windows, if we closed any window set 'switchedOff' to true so we know we are hiding interfaces.
			// If we did not turn anything off, we probably want to show our window.
			if (!CloseAllInterfaces()) {
				Show<T>();
			}
		}

		// Closes all Interfaces, returns true if at least one Interface was closed.
		public bool CloseAllInterfaces() {
			bool interfaceWasClosed = false;
			foreach (var currentInterface in this.interfaces) {
				if (currentInterface.IsOpen) {
					currentInterface.Close();
					interfaceWasClosed = true;
				}
			}
			return interfaceWasClosed;
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
