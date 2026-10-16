using System;

namespace SlyPirates.Shared.Components
{
    [Flags]
    public enum Group
    {
        History = 1 << 0,
        Temporary = 1 << 1,
        All = 1 << 2,
    }
}
