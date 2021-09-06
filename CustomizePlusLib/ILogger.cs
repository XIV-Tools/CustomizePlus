// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib
{
	using System;

	public interface ILogger
	{
		void Information(string message);
		void Error(Exception ex, string message);
	}
}
