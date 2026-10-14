using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Data
{
    public sealed class ComponentDependenciesConfig : List<ComponentDependenciesData> { }
    public class FramedComponentDependenciesConfig : List<ComponentDependenciesData> { }

    public class ComponentDependenciesData
    {
        public ComponentIdList Has = default!;
        public ComponentIdList Add = default!;
    }
}
