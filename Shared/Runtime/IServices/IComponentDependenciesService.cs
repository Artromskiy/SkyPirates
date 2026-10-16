using Delta.ECS;

namespace SlyPirates.Shared.IServices
{
    public interface IComponentDependenciesService
    {
        void Apply(Entity entity);
    }
}
