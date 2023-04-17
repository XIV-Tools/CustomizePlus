// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
