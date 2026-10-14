using DVG.Components;

namespace DVG.SkyPirates.Shared.Tools.TraceHelpers
{
    internal static class Tracing
    {
        public static string NotCreatedEntityCommand(SyncId target)
        {
            return $"Attempt to use command for entity {target}, which is not created";
        }
    }
}
