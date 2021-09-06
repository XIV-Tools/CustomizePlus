// © Anamnesis.
// Licensed under the MIT license.

namespace CustomizePlusLib
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using CustomizePlus.GameStructs;
	using CustomizePlusLib.Mods;

	public class CustomizePlusApi
	{
		#pragma warning disable CS8618
		internal static IMemory Memory;
		internal static IFiles Files;
		internal static ILogger Log;
		#pragma warning restore

		internal readonly Dictionary<string, List<ModBase>> Modifications = new Dictionary<string, List<ModBase>>();

		public CustomizePlusApi(IMemory memory, IFiles files, ILogger log)
		{
			Memory = memory;
			Files = files;
			Log = log;
		}

		public void AddModification(ModBase mod)
		{
			List<ModBase> characterMods;
			if (!this.Modifications.TryGetValue(mod.ActorName, out characterMods))
			{
				characterMods = new List<ModBase>();
				this.Modifications.Add(mod.ActorName, characterMods);
			}

			characterMods.Add(mod);
		}

		public void RemoveModification(ModBase mod)
		{
			List<ModBase> characterMods;
			if (!this.Modifications.TryGetValue(mod.ActorName, out characterMods))
				return;

			characterMods.Remove(mod);
		}

		internal async Task Run(CancellationToken t)
		{
			try
			{
				while (!t.IsCancellationRequested)
				{
					int count = 424;
					IntPtr startAddress = Memory.ActorTableAddress;

					for (int i = 0; i < count; i++)
					{
						IntPtr ptr = Memory.ReadPtr(startAddress + (i * 8));

						if (ptr == IntPtr.Zero)
							continue;

						Actor actor = Memory.Read<Actor>(ptr);

						if (actor.ModelObject == IntPtr.Zero)
							continue;

						List<ModBase> mods;
						if (!this.Modifications.TryGetValue(actor.Name, out mods))
							continue;

						foreach (ModBase mod in mods)
						{
							await mod.Apply(actor);

							if (t.IsCancellationRequested)
							{
								return;
							}
						}

						if (t.IsCancellationRequested)
						{
							return;
						}
					}

					if (t.IsCancellationRequested)
						return;

					await Task.Delay(1000);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Run failed");
			}
		}
	}
}
