// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using CustomizePlus.Memory;
	using Dalamud.Game.ClientState.Objects.Types;
	using Dalamud.Game.ClientState.Objects.SubKinds;
	using Dalamud.Game.ClientState.Objects.Enums;
	using Dalamud.Logging;

	[Serializable]
	public class BodyScale
	{
		private readonly ConcurrentDictionary<int, PoseScale> poses = new();

		public string CharacterName { get; set; } = string.Empty;
		public string ScaleName { get; set; } = string.Empty;
		public bool BodyScaleEnabled { get; set; } = true;
		public Dictionary<string, HkVector4> Bones { get; } = new();
		public HkVector4 RootScale { get; set; } = HkVector4.One;

		public unsafe void Apply(GameObject character)
		{
			RenderObject* obj = null;
			//if (character.ObjectKind == ObjectKind.Player)
			//{
			//	obj = RenderObject.FromActor((Character)character);

			//	if (obj == null)
			//		return;
			//} else
			//{
				obj = RenderObject.FromActor(character);
				//RenderSkeleton* skel = RenderSkeleton.FromActor(character);
			//}
			
			for (int i = 0; i < obj->Skeleton->Length; i++)
			{
				if (!this.poses.ContainsKey(i))
					this.poses.TryAdd(i, new(this, i));

				this.poses[i].Update(obj->Skeleton->PartialSkeletons[i].Pose1);
			}

			HkVector4 rootScale = obj->Scale;
			rootScale.X = MathF.Max(this.RootScale.X, 0.01f);
			rootScale.Y = MathF.Max(this.RootScale.Y, 0.01f);
			rootScale.Z = MathF.Max(this.RootScale.Z, 0.01f);
			obj->Scale = rootScale;
		}

		public void ClearCache()
		{
			this.poses.Clear();
		}

		public class PoseScale
		{
			public readonly BodyScale BodyScale;
			public readonly int Index;

			private readonly Dictionary<int, HkVector4> scaleCache = new();

			private bool isInitialized = false;

			public PoseScale(BodyScale bodyScale, int index)
			{
				this.BodyScale = bodyScale;
				this.Index = index;
			}

			public unsafe void Initialize(HkaPose* pose)
			{
				if (pose == null)
					return;

				lock (this.scaleCache)
				{
					this.scaleCache.Clear();

					int count = pose->Transforms.Count;
					for (int index = 0; index < count; index++)
					{
						HkaBone bone = pose->Skeleton->Bones[index];

						string? boneName = bone.GetName();

						if (boneName == null)
							continue;

						if (this.BodyScale.Bones.TryGetValue(boneName, out var boneScale))
						{
							Transform transform = pose->Transforms[index];

							if (transform.Scale.IsApproximately(boneScale, false))
								continue;

							this.scaleCache.Add(index, boneScale);
						}
					}
				}

				this.isInitialized = true;
			}

			public unsafe void Update(HkaPose* pose)
			{
				if (pose == null)
					return;

				if (!this.isInitialized)
					this.Initialize(pose);

				int count = pose->Transforms.Count;
				for (int index = 0; index < count; index++)
				{
					HkaBone bone = pose->Skeleton->Bones[index];

					if (this.scaleCache.TryGetValue(index, out var boneScale))
					{
						Transform transform = pose->Transforms[index];

						transform.Scale.X = boneScale.X;
						transform.Scale.Y = boneScale.Y;
						transform.Scale.Z = boneScale.Z;

						pose->Transforms[index] = transform;
					}/*
					else if (bone != 1 || )
					{
						Transform transform = pose->Transforms[index];

						transform.Scale.X = 1;
						transform.Scale.Y = 1;
						transform.Scale.Z = 1;

						pose->Transforms[index] = transform;
					}*/
				}
			}
		}
	}
}
