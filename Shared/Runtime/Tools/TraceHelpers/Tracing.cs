using SkyPirates.Shared.Components;
using SkyPirates.Shared.Components.Runtime;

namespace SkyPirates.Shared.Tools.TraceHelpers
{
    internal static class Tracing
    {
        public static string NotCreatedEntityCommand(SyncId target)
        {
            return $"Attempt to use command for entity {target}, which is not created";
        }
    }
}
