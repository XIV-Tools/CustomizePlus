// © Customize+.
// Licensed under the MIT license.

using System;

namespace CustomizePlus.Core
{
    //Borrowed from Brio
    internal interface IService : IDisposable
    {
        void AssignInstance();
        void ClearInstance();
        void Start();
        void Tick(float delta);
        void Stop();
    }
}