// © Customize+.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Data
{
	internal static class Constants
	{
		/// <summary>
		/// Version of the configuration file, when increased a converter should be implemented if necessary
		/// </summary>
		public const int ConfigurationVersion = 2;
		/// <summary>
		/// Version of the scales imported/exported via clipboard, usually should match ConfigurationVersion
		/// </summary>
		public const int ImportExportVersion = ConfigurationVersion;

		public static Vector3 ZeroVector = new Vector3(0, 0, 0);
		public static Vector3 OneVector = new Vector3(1, 1, 1);
	}
}
