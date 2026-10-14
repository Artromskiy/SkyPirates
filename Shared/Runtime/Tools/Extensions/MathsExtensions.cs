using Delta;
namespace DVG.SkyPirates.Shared.Tools.Extensions
{
    public static class MathsExtensions
    {
        public static fix GetRotation(fix2 direction)
        {
            return -Maths.Atan2(-direction.x, direction.y);
        }

        public static fix2 RotatePoint(fix2 vec, fix radians)
        {
            var cs = Maths.Cos(radians);
            var sn = Maths.Sin(radians);
            fix x = vec.x * cs + vec.y * sn;
            fix y = -vec.x * sn + vec.y * cs;
            return new fix2(x, y);
        }
    }
}
