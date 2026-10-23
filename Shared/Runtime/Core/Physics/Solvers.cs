using Delta;
using System;

namespace SlyPirates.Shared.Core.Physics
{
	public static class Solvers
	{
		public static fix Skin = new(1024);
		public static int MaxIterations = 4;
		public static Memory<Segment> Segments = new();

		public static bool RayCast(fix2 from, fix2 to, out Collision collision)
		{
			fix bestSqrDist = fix.MaxValue;
			collision = default;
			bool found = false;
			foreach (var segment in Segments.Span)
            {
                found |= Spatial.Intersects(segment, from, to, ref bestSqrDist, ref collision);
            }

            return found;
		}

		public static bool CircleCast(fix2 from, fix2 to, fix radius, out Collision collision)
		{
			fix bestSqrDist = fix.MaxValue;
			var sqrRadius = radius * radius;
			var movement = to - from;
			collision = default;
			bool found = false;
			foreach (var segment in Segments.Span)
			{
				var projection = Spatial.Projection(from, segment.Start, segment.End);
				var separation = from - projection;
				var sqrDistance = fix2.SqrLength(separation);
				if (sqrDistance < sqrRadius && sqrDistance > fix.Zero)
				{
					// A drop can leave the circle overlapping the cliff behind it.
					// Allow it to leave the overlap; block only movement into the wall.
					if (fix2.Dot(movement, separation) < fix.Zero)
					{
						Spatial.CombineCollision(fix.Zero, new Collision
						{
							Contact = from,
							Normal = separation * Maths.InverseSqrt(sqrDistance),
						}, ref bestSqrDist, ref collision);
						found = true;
					}

					continue;
				}

				var dir = segment.End - segment.Start;

				var normal = new fix2(-dir.y, dir.x);
				var sqrLength = fix2.SqrLength(normal);

				bool skipLine = sqrLength <= fix.Zero;

				fix t = skipLine ? fix.Zero : fix2.Dot((from - segment.Start), dir) / sqrLength;
				if (skipLine || t < fix.Zero)
                {
                    found |= Spatial.Intersects(segment.Start, radius, from, to, ref bestSqrDist, ref collision);
                }

                if (skipLine || t > fix.One)
                {
                    found |= Spatial.Intersects(segment.End, radius, from, to, ref bestSqrDist, ref collision);
                }

                if (skipLine)
                {
                    continue;
                }

                normal *= Maths.InverseSqrt(sqrLength);
				var offset = normal * radius;
				var offsetted1 = new Segment(segment.Start + offset, segment.End + offset);
				var offsetted2 = new Segment(segment.Start - offset, segment.End - offset);
				found |= Spatial.Intersects(offsetted1, from, to, ref bestSqrDist, ref collision);
				found |= Spatial.Intersects(offsetted2, from, to, ref bestSqrDist, ref collision);
			}
			return found;
		}

		public static fix2 CircleSlide(fix2 position, fix2 velocity, fix radius)
		{
			var start = position;
			var end = start + velocity;
			fix sqrSkin = Skin * Skin;
			for (int i = 0; i < MaxIterations; i++)
			{
				if (velocity == fix2.zero)
                {
                    break;
                }

                if (!CircleCast(start, end, radius, out var hit))
				{
					start = end;
					break;
				}

				var targetDir = end - start;
				var realDir = hit.Contact - start;
				var distAlongTarget = fix2.Dot(realDir, targetDir);
				var realSqr = fix2.SqrLength(realDir);
				if (realSqr < sqrSkin)
                {
                    start = hit.Contact - Skin * fix2.Normalize(targetDir);
                }
                else
				{
					var skinOffset = Skin * Maths.InverseSqrt(realSqr) * realDir;
					if (distAlongTarget <= 0)
                    {
                        start = hit.Contact + skinOffset;
                    }
                    else
                    {
                        start += realDir - skinOffset;
                    }
                }

				velocity = end - start;
				var dot = fix2.Dot(velocity, hit.Normal);
				if (dot < fix.Zero)
                {
                    velocity -= hit.Normal * dot;
                }

                if (fix2.Length(velocity) < Skin)
                {
                    break;
                }

                end = start + velocity;
			}
			return start;
		}
	}
}
