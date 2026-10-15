using Delta.ECS;
using DVG.SkyPirates.Shared.Components;

namespace DVG.SkyPirates.Shared.Ecs
{
    public static class WorldFactory
    {
        public static World Create() => Create(new World());

        public static World Create(World world)
        {
            RegisterComponents(world);
            return world;
        }

        private static void RegisterComponents(World world)
        {
            foreach (var registration in GeneratedComponentCatalog.GetRegistrations())
            {
                ComponentId componentId = world.Components.Register(registration);
                world.Components.AddGroup(Group.All, componentId);
            }
        }
    }
}
