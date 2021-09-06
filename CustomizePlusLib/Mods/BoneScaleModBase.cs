// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlusLib.Mods
{
	using System;
	using System.Threading.Tasks;
	using CustomizePlus.GameStructs;

	public abstract class BoneScaleModBase : ModBase
	{
		public BoneScaleModBase(string actorName)
			: base(actorName)
		{
		}

		internal sealed override Task Apply(Actor actor)
		{
			// Bypass model datas
			Model model = CustomizePlusApi.Memory.Read<Model>(actor.ModelObject);

			if (model.Skeleton == IntPtr.Zero)
				return Task.CompletedTask;

			SkeletonWrapper skeletonWrapper = CustomizePlusApi.Memory.Read<SkeletonWrapper>(model.Skeleton);

			if (skeletonWrapper.Skeleton == IntPtr.Zero)
				return Task.CompletedTask;

			Skeleton skeleton = CustomizePlusApi.Memory.Read<Skeleton>(skeletonWrapper.Skeleton);
			return this.Apply(actor, skeleton);
		}

		protected abstract Task Apply(Actor actor, Skeleton skeleton);
	}
}
