// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace CustomizePlus.Interface
{
    public class InterfaceManager : IDisposable
    {
        private readonly List<InterfaceBase> interfaces = new();

        public void Dispose()
        {
            foreach (var iface in interfaces)
            {
                iface.Close();
                iface.Dispose();
            }

            interfaces.Clear();
        }

        public void Draw()
        {
            if (interfaces.Count <= 0)
            {
                return;
            }

            for (var i = interfaces.Count - 1; i >= 0; i--)
            {
                interfaces[i].DoDraw(i);

                if (!interfaces[i].IsOpen)
                {
                    interfaces[i].Dispose();
                    interfaces.RemoveAt(i);
                }
            }
        }

        public T Show<T>()
            where T : InterfaceBase, new()
        {
            //todo: do not allow more than a single instance of the same window?
            var ui = Activator.CreateInstance<T>();
            ui.Open();

            if (ui.IsOpen)
            {
                interfaces.Add(ui);
            }

            return ui;
        }

        // Added this so you can close with /customize. This may need a rework in the future to access the broader usecase of the rest of the methods. But this should serve for now?
        public void Toggle<T>()
            where T : InterfaceBase, new()
        {
            if (interfaces.Count <= 0)
            {
                new InvalidOperationException("Interfaces is empty.");
            }

            // Close all windows, if we closed any window set 'switchedOff' to true so we know we are hiding interfaces.
            // If we did not turn anything off, we probably want to show our window.
            if (!CloseAllInterfaces())
            {
                Show<T>();
            }
        }

        // Closes all Interfaces, returns true if at least one Interface was closed.
        public bool CloseAllInterfaces()
        {
            var interfaceWasClosed = false;
            foreach (var currentInterface in interfaces)
            {
                if (currentInterface.IsOpen)
                {
                    currentInterface.Close();
                    interfaceWasClosed = true;
                }
            }

            return interfaceWasClosed;
        }

        public InterfaceBase? GetInterface(Type type)
        {
            foreach (var ui in interfaces)
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