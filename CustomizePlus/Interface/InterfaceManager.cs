// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace CustomizePlus.Interface
{
    public class InterfaceManager : IDisposable
    {
        private readonly List<InterfaceBase> _interfaces = new();

        public void Dispose()
        {
            foreach (var iface in _interfaces)
            {
                iface.Close();
                iface.Dispose();
            }

            _interfaces.Clear();
        }

        public void Draw()
        {
            if (_interfaces.Count <= 0)
            {
                return;
            }

            for (var i = _interfaces.Count - 1; i >= 0; i--)
            {
                _interfaces[i].DoDraw(i);

                if (!_interfaces[i].IsOpen)
                {
                    _interfaces[i].Dispose();
                    _interfaces.RemoveAt(i);
                }
            }
        }

        public T Show<T>()
            where T : InterfaceBase
        {
            //todo: do not allow more than a single instance of the same window?
            var ui = Activator.CreateInstance<T>();
            ui.Open();

            if (ui.IsOpen)
            {
                _interfaces.Add(ui);
            }

            return ui;
        }

        // Added this so you can close with /customize. This may need a rework in the future to access the broader usecase of the rest of the methods. But this should serve for now?
        public void Toggle<T>()
            where T : InterfaceBase, new()
        {
            if (_interfaces.Count <= 0)
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
            foreach (var currentInterface in _interfaces)
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
            foreach (var ui in _interfaces)
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