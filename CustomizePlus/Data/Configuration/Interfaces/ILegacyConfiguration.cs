// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomizePlus.Data.Configuration;

namespace CustomizePlus.Data.Configuration.Interfaces
{
	internal interface ILegacyConfiguration
	{
		public PluginConfiguration ConvertToLatestVersion();
	}
}
