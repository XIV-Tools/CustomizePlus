// © Customize+.
// Licensed under the MIT license.

using System;

namespace CustomizePlus.UI
{
    public abstract class UserInterfaceBase : IDisposable
    {
        public bool IsOpen { get; private set; }

        protected int Index { get; private set; }
        protected UserInterfaceManager Manager => Plugin.InterfaceManager;

        protected virtual bool SingleInstance => false;

        public virtual void Dispose()
        {
        }

        public virtual void Open()
        {
            if (SingleInstance)
            {
                var instance = Manager.GetUserInterface(GetType());
                if (instance != null)
                {
                    instance?.Focus();
                    return;
                }
            }

            IsOpen = true;
        }

        public virtual void Focus()
        {
            // ??
        }

        public abstract void Draw();

        public virtual void Close()
        {
            IsOpen = false;
        }

        internal void DoDraw(int index)
        {
            Index = index;
            Draw();
        }
    }
}