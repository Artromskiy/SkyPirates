using Delta.ECS;

namespace SlyPirates.Shared.IServices
{
    public interface IComponentDefaultsService
    {
        void Apply(Entity entity);
    }
}
