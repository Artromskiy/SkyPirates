using Delta.ECS;

namespace SkyPirates.Shared.IServices
{
    public interface IComponentDependenciesService
    {
        void Apply(Entity entity);
    }
}
