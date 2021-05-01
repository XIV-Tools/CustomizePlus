using System;
using System.Collections.Generic;
using System.Threading;
using Anamnesis.Core.Memory;
using Anamnesis.Files;
using Anamnesis.Memory;
using Anamnesis.PoseModule;
using Anamnesis.Posing.Templates;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ConsoleApp1
{
	class Program
	{
		static bool running = true;
		static Dictionary<string, Modify> modifications = new Dictionary<string, Modify>();

		static void Main(string[] args)
		{
			LoggerConfiguration config = new LoggerConfiguration();
			config.WriteTo.Console();

			Log.Logger = config.CreateLogger();

			Log.Information("Initializing");

			try
			{
				MemoryService.GetProcess();
				AddressService.Scan();
				SkeletonService.LoadSkeletons();
				CustomizeService.LoadCustomizers();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Init failed");
				Console.ReadKey();
				return;
			}

			Log.Information("Running");

			NopHookViewModel freezeScale = new NopHookViewModel(AddressService.SkeletonFreezeScale, 6);
			NopHookViewModel freezeScale2 = new NopHookViewModel(AddressService.SkeletonFreezeScale2, 6);
			////freezeScale.Enabled = true;
			freezeScale2.Enabled = true;

			new Thread(new ThreadStart(Run)).Start();

			Console.WriteLine("Press return to terminate");
			Console.ReadLine();
			running = false;

			Log.Information("Shutting Down");

			freezeScale.Enabled = false;
			freezeScale2.Enabled = false;
		}

		private static void Run()
		{
			try
			{
				while (running)
				{
					int count = 424;
					IntPtr startAddress = AddressService.ActorTable;

					for (int i = 0; i < count; i++)
					{
						IntPtr ptr = MemoryService.ReadPtr(startAddress + (i * 8));

						if (ptr == IntPtr.Zero)
							continue;

						Actor actor = MemoryService.Read<Actor>(ptr);

						if (actor.ModelObject == IntPtr.Zero)
							continue;

						Modify? modify = null;

						if (!modifications.ContainsKey(actor.Name))
						{
							PoseFile? pose = CustomizeService.GetPose(actor.Name);

							if (pose == null)
								continue;

							modify = new Modify(actor);
							modifications.Add(actor.Name, modify);
							Log.Information("Loading modifications for actor: " + actor.Name);
						}

						if (modify != null || modifications.TryGetValue(actor.Name, out modify))
						{
							// Bypass model datas
							Model model = MemoryService.Read<Model>(actor.ModelObject);

							if (model.Skeleton == IntPtr.Zero)
								continue;

							SkeletonWrapper skeletonWrapper = MemoryService.Read<SkeletonWrapper>(model.Skeleton);

							if (skeletonWrapper.Skeleton == IntPtr.Zero)
								continue;

							Skeleton skeleton = MemoryService.Read<Skeleton>(skeletonWrapper.Skeleton);

							modify.Apply(skeleton);
						}
					}

					Thread.Sleep(1000);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Run failed");
			}
		}

		private class Modify
		{
			private SkeletonFile? skeletonFile;
			private PoseFile? poseFile;

			public Modify(Actor actor)
			{
				this.skeletonFile = SkeletonService.GetSkeletonFile(actor.Customize);
				this.poseFile = CustomizeService.GetPose(actor.Name);
			}

			public void Apply(Skeleton skeleton)
			{
				if (this.skeletonFile == null || this.skeletonFile.BoneNames == null)
					return;

				if (this.poseFile == null || this.poseFile.Bones == null)
					return;

				if (skeleton.Body == IntPtr.Zero)
					return;

				Bones bodyBones = MemoryService.Read<Bones>(skeleton.Body);

				if (bodyBones.Count > 110 || bodyBones.Count < 100)
					return;

				string boneCategory = "Body_";
				for (int boneIndex = 0; boneIndex < bodyBones.Count; boneIndex++)
				{
					string boneName = skeletonFile.BoneNames[boneCategory + boneIndex];

					// Physics bones should not be messed with
					if (boneName.StartsWith("Breast") || boneName.StartsWith("Cloth"))
						continue;

					PoseFile.Bone? poseBone;
					if (!this.poseFile.Bones.TryGetValue(boneName, out poseBone))
						continue;

					if (poseBone == null || poseBone.Scale == null)
						continue;

					IntPtr bonePtr = bodyBones.TransformArray + (0x30 * boneIndex);
					Transform transform = MemoryService.Read<Transform>(bonePtr);
					transform.Scale = (Vector)poseBone.Scale;
					MemoryService.Write(bonePtr, transform);
				}
			}
		}
	}
}
