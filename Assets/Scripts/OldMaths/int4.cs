using System;
using static DVG.Maths.math;

namespace DVG.Maths
{
    public struct int4
    {
        public int x;
        public int y;
        public int z;
        public int w;

        public const int length = 4;

        public int4(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public int4 minMax(int4 compare)
        {
            var temp = this;
            temp[0] = min(temp[0], compare[0]);
            temp[1] = min(temp[1], compare[1]);
            temp[2] = max(temp[2], compare[2]);
            temp[3] = max(temp[3], compare[3]);
            return temp;
        }

        public int this[int i]
        {
            readonly get => i switch
            {
                0 => x,
                1 => y,
                2 => z,
                3 => w,
                _ => throw new IndexOutOfRangeException()
            };
            set
            {
                switch (i)
                {
                    case 0:
                        x = value; break;
                    case 1:
                        y = value; break;
                    case 2:
                        z = value; break;
                    case 3:
                        w = value; break;
                };
            }
        }
    }
}