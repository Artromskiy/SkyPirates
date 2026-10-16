using Delta.ECS;

namespace DVG.SkyPirates.Shared.IServices
{
    public interface IComponentDependenciesService
    {
        void Apply(Entity entity);
    }
}
