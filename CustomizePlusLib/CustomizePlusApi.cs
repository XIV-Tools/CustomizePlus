// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using CustomizePlus.GameStructs;
	using CustomizePlusLib.Memory;
	using CustomizePlusLib.Mods;

	public class CustomizePlusApi
	{
		#pragma warning disable CS8618
		internal static IMemory Memory;
		internal static ILogger Log;
		#pragma warning restore

		internal readonly Dictionary<string, List<ModBase>> Modifications = new Dictionary<string, List<ModBase>>();
		private Task? runTask;
		private bool run;

		public CustomizePlusApi(IMemory memory, ILogger log)
		{
			Memory = memory;
			Log = log;
		}

		public void Start()
		{
			this.run = true;
			this.runTask = Task.Run(this.Run);
		}

		public void Stop()
		{
			NopHookViewModel.ClearAll();

			Task.Run(this.StopAsync);
		}

		public async Task StopAsync()
		{
			NopHookViewModel.ClearAll();

			this.run = false;

			if (this.runTask != null)
			{
				await this.runTask;
			}
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

		internal async Task Run()
		{
			NopHookViewModel? freezeScale = null;

			try
			{
				freezeScale = new NopHookViewModel(Memory.FreezeScaleAddress, 6);
				freezeScale.Enabled = true;

				while (this.run)
				{
					int count = 424;
					IntPtr startAddress = Memory.ActorTableAddress;

					for (int i = 0; i < count; i++)
					{
						if (!this.run)
							return;

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
							if (!this.run)
								return;

							await mod.Apply(actor);
						}
					}

					if (!this.run)
						return;

					await Task.Delay(1000);
				}

				freezeScale?.SetEnabled(false);
			}
			catch (Exception ex)
			{
				this.run = false;
				Log.Error(ex, "Run failed");
			}
		}
	}
}