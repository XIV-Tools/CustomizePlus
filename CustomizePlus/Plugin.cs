// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Text;
	using CustomizePlus.Memory;
	using Dalamud.Game;
	using Dalamud.Game.Gui;
	using Dalamud.Game.ClientState;
	using Dalamud.Game.ClientState.Objects.SubKinds;
	using Dalamud.Game.Command;
	using Dalamud.IoC;
	using Dalamud.Hooking;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		public static readonly StringBuilder Status = new StringBuilder();

		private readonly ClientState clientState;

		private delegate IntPtr RenderDelegate(IntPtr renderManager);
		private readonly Hook<RenderDelegate>? renderManagerHook;
		private bool updateFailed;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager,
			ClientState clientState,
			SigScanner sigScanner,
			ChatGui chatGui)
        {
			PluginInterface = pluginInterface;
			CommandManager = commandManager;

			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			Interface = new Interface();

			Commands.Add((s, t) => Interface.Show(), "/customize", "Opens the customize plus window");

			PluginInterface.UiBuilder.Draw += Interface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi += Interface.Show;

			this.clientState = clientState;

			try
			{
				// "Render::Manager::Render"
				renderManagerHook = new Hook<RenderDelegate>(sigScanner.ScanText("40 53 55 57 41 56 41 57 48 83 EC 60"), manager =>
				{
					// if this gets disposed while running we crash calling Original's getter, so get it at start
					var original = renderManagerHook.Original;
					try
					{
						if (!updateFailed)
						{
							Update();
						}
					}
					catch (Exception e)
					{
						chatGui.PrintError("Failed to run CustomizePlus render hook, disabling.");
						PluginLog.Error($"Error in CustomizePlus render hook {e}");

						updateFailed = true;
					}

					return original(manager);
				});
				// Because scales all get set to 0 below, the character will be very messed up
				renderManagerHook.Enable();
			}
			catch (Exception e)
			{
				PluginLog.Error($"Failed to hook Render::Manager::Render {e}");
			}
		}

#pragma warning disable CS8618
		public static DalamudPluginInterface PluginInterface { get; private set; }
		public static CommandManager CommandManager { get; private set; }
		public static Configuration Configuration { get; private set; }
		public static Interface Interface { get; private set; }
#pragma warning restore

		public string Name => "Customize Plus";

		public void Dispose()
        {
			Files.Dispose();
			Commands.Dispose();
			PluginInterface.UiBuilder.Draw -= Interface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi -= Interface.Show;

			renderManagerHook?.Disable();
			renderManagerHook?.Dispose();
        }

		public unsafe void Update()
		{
			PlayerCharacter? player = this.clientState.LocalPlayer;

			Status.Clear();
			Status.AppendLine(player?.ToString());

			if (player == null)
				return;

			RenderSkeleton* skel = RenderSkeleton.FromActor(player);

			if (skel == null)
				return;

			for (int i = 0; i < skel->Length; i++)
			{
				this.Update(skel->PartialSkeletons[i].Pose1);
			}
		}

		private unsafe void Update(HkaPose* pose)
		{
			if (pose == null)
				return;

			Status.Append(pose->Transforms.Count.ToString());
			Status.Append(" - ");
			Status.AppendLine(pose->Skeleton->Bones.Count.ToString());

			int count = pose->Transforms.Count;

			for (int index = 0; index < count; index++)
			{
				HkaBone bone = pose->Skeleton->Bones[index];
				Transform transform = pose->Transforms[index];

				string name = bone.GetName() ?? "???";
				Status.Append("    ");
				Status.Append(index);
				Status.Append(": ");
				Status.Append(name);
				Status.Append(" - ");
				Status.AppendLine(transform.Scale.ToString());

				transform.Scale.X = 0;
				transform.Scale.Y = 0;
				transform.Scale.Z = 0;

				pose->Transforms[index] = transform;
			}

			Status.AppendLine();
			Status.AppendLine();
		}
    }
}
