// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System.Text;
	using CustomizePlus.Memory;
	using Dalamud.Game.ClientState;
	using Dalamud.Game.ClientState.Objects.SubKinds;
	using Dalamud.Game.Command;
	using Dalamud.IoC;
	using Dalamud.Logging;
	using Dalamud.Plugin;

	public sealed class Plugin : IDalamudPlugin
    {
		public static readonly StringBuilder Status = new StringBuilder();

		private readonly ClientState clientState;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager,
			ClientState clientState)
        {
			PluginInterface = pluginInterface;
			CommandManager = commandManager;

			Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

			Interface = new Interface();

			Commands.Add((s, t) => Interface.Show(), "/customize", "Opens the customize plus window");

			PluginInterface.UiBuilder.Draw += this.Update;
			PluginInterface.UiBuilder.Draw += Interface.Draw;
			PluginInterface.UiBuilder.OpenConfigUi += Interface.Show;

			this.clientState = clientState;
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
        }

		public unsafe void Update()
		{
			PlayerCharacter? player = this.clientState.LocalPlayer;

			Status.Clear();
			Status.AppendLine(player?.ToString());

			if (player == null)
				return;

			RenderSkeleton* skel = RenderSkeleton.FromActor(player);

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
			}

			Status.AppendLine();
			Status.AppendLine();
		}
    }
}
