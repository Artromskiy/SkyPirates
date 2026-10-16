using Delta;
using System.Runtime.CompilerServices;

namespace SlyPirates.Shared.Core.Physics
{
	public static class Spatial
	{
		public static bool Intersects(Segment segment, fix2 from, fix2 to, ref fix bestSqrDist, ref Collision collision)
		{
			if (!Intersects(segment.Start, segment.End, from, to, out var currentCollision))
				return false;

			var sqrDist = fix2.SqrDistance(from, currentCollision.Contact);
			CombineCollision(sqrDist, currentCollision, ref bestSqrDist, ref collision);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Intersects(fix2 position, fix radius, fix2 from, fix2 to, ref fix bestSqrDist, ref Collision collision)
		{
			if (!Intersects(position, radius, from, to, out var currentCollision))
				return false;

			var sqrDist = fix2.SqrDistance(from, currentCollision.Contact);
			CombineCollision(sqrDist, currentCollision, ref bestSqrDist, ref collision);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CombineCollision(fix sqrDist, Collision currentCollision, ref fix bestSqrDist, ref Collision collision)
		{
			if (sqrDist > bestSqrDist)
				return;

			if (sqrDist == bestSqrDist)
			{
				collision.Normal = (collision.Normal + currentCollision.Normal) / 2;
				collision.Contact = (collision.Contact + currentCollision.Contact) / 2;
			}
			else
			{
				collision = currentCollision;
			}
			bestSqrDist = sqrDist;
		}

		public static bool Intersects(fix2 position, fix radius, fix2 from, fix2 to, out Collision collision)
		{
			collision = default;

			var sqrRadius = radius * radius;
			var proj = Projection(position, from, to);
			if (fix2.SqrDistance(proj, position) > sqrRadius)
				return false;

			var d = to - from;
			var f = from - position;
			var a = fix2.Dot(d, d);
			var b = fix2.Dot(f, d);
			var sqrF = fix2.Dot(f, f);
			var c = sqrF - sqrRadius;

			fix discriminant = (b * b - a * c);
			if (discriminant < 0)
				return false;

			discriminant = Maths.Sqrt(discriminant);
			var t1 = (-b - discriminant) / (a);
			var t2 = (-b + discriminant) / (a);
			if (t1 >= 0 && t1 <= 1)
			{
				var contact = from + t1 * d;
				var normal = fix2.Normalize(contact - position);
				collision = new()
				{
					Contact = contact,
					Normal = normal,
				};
				return true;
			}
			if (t2 >= 0 && t2 <= 1)
			{
				fix2 normal = sqrF <= 0 ? -d * Maths.InverseSqrt(a) : f * Maths.InverseSqrt(sqrF);
				var contact = position + normal * radius;

				collision = new()
				{
					Contact = contact,
					Normal = normal,
				};
				return true;
			}
			return false;
		}

		public static bool Intersects(fix2 a, fix2 b, fix2 from, fix2 to, out Collision collision)
		{
			collision = default;

			var ab = b - a;
			var cd = to - from;

			var d1 = Cross(cd, a - from);
			var d2 = Cross(cd, b - from);
			var d3 = Cross(ab, from - a);
			var d4 = Cross(ab, to - a);

			var normal = new fix2(-ab.y, ab.x);
			if (fix2.Dot(normal, from - a) < fix.Zero)
				normal = -normal;

			var sqrLength = fix2.SqrLength(normal);
			if (sqrLength <= fix.Zero)
				return false;

			collision.Normal = normal * Maths.InverseSqrt(sqrLength);

			if (d1 == fix.Zero && OnSegment(from, to, a))
			{
				collision.Contact = a;
				return true;
			}

			if (d2 == fix.Zero && OnSegment(from, to, b))
			{
				collision.Contact = b;
				return true;
			}

			if (d3 == fix.Zero && OnSegment(a, b, from))
			{
				collision.Contact = from;
				return true;
			}

			if (d4 == fix.Zero && OnSegment(a, b, to))
			{
				collision.Contact = to;
				return true;
			}

			if ((d1 > fix.Zero && d2 > fix.Zero) || (d1 < fix.Zero && d2 < fix.Zero))
				return false;

			if ((d3 > fix.Zero && d4 > fix.Zero) || (d3 < fix.Zero && d4 < fix.Zero))
				return false;

			var denom = d2 - d1;

			if (denom == 0)
			{
				if (d1 != fix.Zero)
					return false;

				bool useX = Maths.Abs(b.x - a.x) > Maths.Abs(b.y - a.y);

				fix a0 = useX ? a.x : a.y;
				fix a1 = useX ? b.x : b.y;
				fix c0 = useX ? from.x : from.y;
				fix c1 = useX ? to.x : to.y;

				if (a0 > a1)
					(a0, a1) = (a1, a0);
				if (c0 > c1)
					(c0, c1) = (c1, c0);

				if (a1 < c0 || c1 < a0)
					return false;

				collision.Contact = from;
				return true;
			}

			collision.Contact = (a * d2 - b * d1) / (denom);

			return true;
		}



		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool OnSegment(fix2 a, fix2 b, fix2 p)
		{
			return
				p.x >= Maths.Min(a.x, b.x) && p.x <= Maths.Max(a.x, b.x) &&
				p.y >= Maths.Min(a.y, b.y) && p.y <= Maths.Max(a.y, b.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static fix Cross(fix2 a, fix2 b) => a.x * b.y - a.y * b.x;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static fix2 Projection(fix2 point, fix2 start, fix2 end)
		{
			var ab = end - start;
			var sqrLen = fix2.SqrLength(ab);

			if (sqrLen <= fix.Zero)
				return start;

			var t = Maths.Clamp(fix2.Dot(point - start, ab) / sqrLen, 0, 1);

			return start + ab * t;
		}
	}
}
