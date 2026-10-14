using Delta;
using System;
using System.Runtime.CompilerServices;

namespace SkyPirates.Shared.Core
{
	public static class Hex
	{
		private static readonly fix _sqrtThree = Maths.Sqrt((fix)3);
		private static readonly fix _sqrtThreeOverTwo = _sqrtThree / 2;

		public static readonly fix OuterRadius = fix.One;
		public static readonly fix InnerRadius = OuterRadius * _sqrtThreeOverTwo;

		private static readonly fix _oneAndHalf = fix.One + fix.One / 2;
		private static readonly fix _twoOverThreeOverOuterRadius = (fix)2 / 3 / OuterRadius;
		private static readonly fix _threeOuterRadius = OuterRadius * 3;
		private static readonly fix _oneAndHalfOuterRadius = OuterRadius * _oneAndHalf;

		private static readonly fix _threeOverFourOuterRadius = OuterRadius * 3 / 4;
		private static readonly fix _halfInnerRadius = InnerRadius / 2;

		private static readonly fix _hexHeight = fix.One / 2;

		private static readonly fix2[] _points = new fix2[]
		{
			new(OuterRadius / 2, InnerRadius),
			new(OuterRadius, 0),
			new(OuterRadius / 2, -InnerRadius),
			new(-OuterRadius / 2, -InnerRadius),
			new(-OuterRadius, 0),
			new(-OuterRadius / 2, InnerRadius),
		};

		private static readonly fix2[] _normals = new fix2[]
		{
			fix2.Normalize(new fix2(_threeOverFourOuterRadius, _halfInnerRadius)),
			fix2.Normalize(new fix2(_threeOverFourOuterRadius, -_halfInnerRadius)),
			fix2.Normalize(new fix2(0, -InnerRadius)),
			fix2.Normalize(new fix2(-_threeOverFourOuterRadius, -_halfInnerRadius)),
			fix2.Normalize(new fix2(-_threeOverFourOuterRadius, _halfInnerRadius)),
			fix2.Normalize(new fix2(0, InnerRadius)),
		};

		private static readonly int2[] _axialNear = new int2[]
		{
			new(0, 1),
			new(1, 0),
			new(1, -1),
			new(0, -1),
			new(-1, 0),
			new(-1, 1),
		};

		public static ReadOnlySpan<fix2> Points => _points;
		public static ReadOnlySpan<fix2> Normals => _normals;
		public static ReadOnlySpan<int2> AxialNear => _axialNear;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int2 AxialRound(fix q, fix r)
		{
			fix y = -q - r;

			int rx = (int)Maths.Round(q);
			int rz = (int)Maths.Round(r);
			int ry = (int)Maths.Round(y);

			fix dx = Maths.Abs(rx - q);
			fix dz = Maths.Abs(rz - r);
			fix dy = Maths.Abs(ry - y);

			if (dx > dy && dx > dz)
				rx = -ry - rz;
			else if (dy <= dz)
				rz = -rx - ry;
			//else
			//    ry = -rx - rz;

			return new int2(rx, rz);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 WorldToAxial(fix2 pos)
		{
			fix q = pos.x * _twoOverThreeOverOuterRadius;
			fix r = (_sqrtThree * pos.y - pos.x) / _threeOuterRadius;
			return AxialRound(q, r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int3 WorldToAxial(fix3 pos)
		{
			fix q = pos.x * _twoOverThreeOverOuterRadius;
			fix r = (_sqrtThree * pos.z - pos.x) / _threeOuterRadius;
			var axial = AxialRound(q, r).x_y;
			axial.y = WorldToAxialY(pos.y);
			return axial;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static fix2 AxialToWorld(int2 axial)
		{
			int q = axial.x;
			int r = axial.y;

			fix x = _oneAndHalfOuterRadius * q;
			fix y = InnerRadius * (r + r + q);
			return new fix2(x, y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 OffsetToAxial(int2 offset)
		{
			var parity = offset.x & 1;
			var q = offset.x;
			var r = offset.y - (offset.x - parity) / 2;
			return new int2(q, r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 AxialToOffset(int2 axial)
		{
			var parity = axial.x & 1;
			var col = axial.x;
			var row = axial.y + (axial.x - parity) / 2;
			return new int2(col, row);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static fix AxialToWorldY(int y) => (fix)y * _hexHeight;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int WorldToAxialY(fix y) => (int)Maths.Floor(y / _hexHeight);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static fix3 AxialToWorld(int3 axial)
		{
			var pos = AxialToWorld(axial.xz).x_y;
			pos.y = AxialToWorldY(axial.y);
			return pos;
		}
	}
}
