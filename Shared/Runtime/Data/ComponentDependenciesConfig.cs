using System.Collections.Generic;

namespace SlyPirates.Shared.Data
{
    public sealed class ComponentDependenciesConfig : List<ComponentDependenciesData> { }

    public class ComponentDependenciesData
    {
        public ComponentIdList Has = default!;
        public ComponentIdList Add = default!;
    }
}
