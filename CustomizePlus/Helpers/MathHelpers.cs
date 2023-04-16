// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Helpers
{
	internal class MathHelpers
	{
		public static readonly Vector3 ZeroVector = new Vector3(0, 0, 0);
		public static readonly Vector3 OneVector = new Vector3(1, 1, 1);

		//Borrowed from Anamnesis/Ktisis
		public static readonly float Deg2Rad = ((float)Math.PI * 2) / 360;
		public static readonly float Rad2Deg = 360 / ((float)Math.PI * 2);

		public static Quaternion EulerToQuaternion(Vector3 euler)
		{
			double yaw = euler.Y * Deg2Rad;
			double pitch = euler.X * Deg2Rad;
			double roll = euler.Z * Deg2Rad;

			double c1 = Math.Cos(yaw / 2);
			double s1 = Math.Sin(yaw / 2);
			double c2 = Math.Cos(pitch / 2);
			double s2 = Math.Sin(pitch / 2);
			double c3 = Math.Cos(roll / 2);
			double s3 = Math.Sin(roll / 2);

			double c1c2 = c1 * c2;
			double s1s2 = s1 * s2;

			double x = (c1c2 * s3) + (s1s2 * c3);
			double y = (s1 * c2 * c3) + (c1 * s2 * s3);
			double z = (c1 * s2 * c3) - (s1 * c2 * s3);
			double w = (c1c2 * c3) - (s1s2 * s3);

			return new Quaternion((float)x, (float)y, (float)z, (float)w);
		}
	}
}
