// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib.Mods
{
	using System.Threading.Tasks;
	using CustomizePlus.GameStructs;

	public abstract class ModBase
	{
		public readonly string ActorName;

		public ModBase(string actorName)
		{
			this.ActorName = actorName;
		}

		internal abstract Task Apply(Actor actor);
	}
}
