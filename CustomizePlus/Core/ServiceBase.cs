// © Customize+.
// Licensed under the MIT license.

using System;

namespace CustomizePlus.Core
{
    //Borrowed from Brio
    internal abstract class ServiceBase<T> : IService
        where T : ServiceBase<T>
    {
        private static T? _instance;

        public static T Instance => _instance ?? throw new Exception($"No service found: {typeof(T)}");

        public void AssignInstance()
        {
            _instance = (T?)this;
        }

        public void ClearInstance()
        {
            _instance = null;
        }

        public virtual void Start()
        {
            _instance = (T)this;
        }

        public virtual void Tick(float delta)
        {
        }

        public virtual void Stop()
        {
        }

        public virtual void Dispose()
        {
        }
    }
}