// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Interface.LegacyConfiguration.Data
{
	internal interface ILegacyConfiguration
	{
		public Configuration ConvertToLatestVersion();
	}
}
