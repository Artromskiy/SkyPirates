namespace DVG
{
    public static class float2Extensions
    {
        public static float2 Rotate(this float2 vec, float degress)
        {
            float radians = Maths.Radians(degress);
            var cs = Maths.Cos(radians);
            var sn = Maths.Sin(radians);
            float x = vec.x * cs + vec.y * sn;
            float y = -vec.x * sn + vec.y * cs;
            return new float2(x, y);
        }
    }
}