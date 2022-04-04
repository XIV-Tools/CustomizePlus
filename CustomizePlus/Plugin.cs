// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Text;
	using CustomizePlus.Memory;
	using Dalamud.Game;
	using Dalamud.Game.ClientState;
	using Dalamud.Game.ClientState.Objects.SubKinds;
	using Dalamud.Game.Command;
	using Dalamud.Game.Gui;
	using Dalamud.Hooking;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		public static readonly StringBuilder Status = new StringBuilder();

		private static Plugin? instance;

		private readonly DalamudPluginInterface pluginInterface;
		private readonly CommandManager commandManager;
		private readonly Configuration configuration;
		private readonly Interface userInterface;
		private readonly ChatGui chatGui;

		private readonly ClientState clientState;
		private readonly Hook<RenderDelegate> renderManagerHook;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager,
			ClientState clientState,
			SigScanner sigScanner,
			ChatGui chatGui)
        {
			instance = this;

			this.pluginInterface = pluginInterface;
			this.commandManager = commandManager;
			this.userInterface = new Interface();
			this.chatGui = chatGui;
			this.configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			CommandManager.AddCommand((s, t) => UserInterface.Show(), "/customize", "Opens the customize plus window");

			PluginInterface.UiBuilder.Draw += UserInterface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi += UserInterface.Show;

			this.clientState = clientState;

			try
			{
				// "Render::Manager::Render"
				this.renderManagerHook = new Hook<RenderDelegate>(sigScanner.ScanText("40 53 55 57 41 56 41 57 48 83 EC 60"), this.OnRender);
				this.renderManagerHook.Enable();
			}
			catch (Exception e)
			{
				PluginLog.Error($"Failed to hook Render::Manager::Render {e}");
				throw;
			}

			chatGui.Print("Cusotmize+ started");
		}

		private delegate IntPtr RenderDelegate(IntPtr renderManager);

		public static Plugin Instance
		{
			get
			{
				if (instance == null)
					throw new Exception("Plugin is not loaded");

				return instance;
			}
		}

		public static DalamudPluginInterface PluginInterface => Instance.pluginInterface;
		public static CommandManager CommandManager => Instance.commandManager;
		public static Configuration Configuration => Instance.configuration;
		public static Interface UserInterface => Instance.userInterface;
		public static ChatGui ChatGui => Instance.chatGui;

		public string Name => "Customize Plus";

		public void Dispose()
        {
			Files.Dispose();
			CommandManagerExtensions.Dispose();

			PluginInterface.UiBuilder.Draw -= UserInterface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi -= UserInterface.Show;

			this.renderManagerHook?.Disable();
			this.renderManagerHook?.Dispose();
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

		private IntPtr OnRender(IntPtr manager)
		{
			if (this.renderManagerHook == null)
				throw new Exception();

			// if this gets disposed while running we crash calling Original's getter, so get it at start
			RenderDelegate original = this.renderManagerHook.Original;

			try
			{
				this.Update();
			}
			catch (Exception e)
			{
				PluginLog.Error($"Error in CustomizePlus render hook {e}");
				this.renderManagerHook?.Disable();
			}

			return original(manager);
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
