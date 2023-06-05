// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using CustomizePlus.Services;

namespace CustomizePlus.Core
{
    //Borrowed from Brio
    public class ServiceManager
    {
        private readonly List<IService> _services = new();
        private readonly Stopwatch _tickTimer = new();

        public ServiceManager()
        {
            Add<GPoseService>();
            Add<GPoseAmnesisKtisisWarningService>();
            Add<PosingModeDetectService>();
        }

        public bool IsStarted { get; private set; }

        //--------

        private void Add<T>() where T : ServiceBase<T>, IService
        {
            var newType = typeof(T);

            var service = (T?)Activator.CreateInstance(newType);
            if (service != null)
            {
                _services.Add(service);
                service.AssignInstance();
            }
        }

        public void Start()
        {
            if (IsStarted)
            {
                throw new Exception("Services already running");
            }

            foreach (var service in _services)
            {
                service.Start();
            }

            IsStarted = true;

            _tickTimer.Reset();
            _tickTimer.Start();
        }

        public void Tick()
        {
            if (!IsStarted)
            {
                return;
            }

            var delta = (float)_tickTimer.Elapsed.TotalSeconds;
            _tickTimer.Restart();

            foreach (var service in _services)
            {
                service.Tick(delta);
            }
        }

        public void Dispose()
        {
            _tickTimer.Stop();
            _tickTimer.Reset();

            var reversed = _services.ToList();
            reversed.Reverse();

            if (IsStarted)
            {
                foreach (var service in reversed)
                {
                    service.Stop();
                }
            }

            IsStarted = false;

            foreach (var service in reversed)
            {
                service.Dispose();
                service.ClearInstance();
            }

            _services.Clear();
        }
    }
}