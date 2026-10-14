using DVG.Ids;

namespace DVG.SkyPirates.Client.IFactories
{
    public static class IdPathFormatter
    {
        private const string ViewPrefabPathFormat = "Prefabs/{0}";
        private const string VisualPrefabPathFormat = "Prefabs/Visual/{0}/{1}";

        private static string NonIdName<T>() => typeof(T).Name[..^2];
        private static string GetFolderName<T>() => NonIdName<T>();
        public static string FormatVisualPath<T>(T id) where T : IId =>
            string.Format(VisualPrefabPathFormat, GetFolderName<T>(), id.Value);

        public static string FormatViewPath<T>() where T : IId =>
            string.Format(ViewPrefabPathFormat, NonIdName<T>());
    }
}
