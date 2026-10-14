using Delta.ECS;

namespace SkyPirates.Shared.IServices
{
    public interface IComponentDefaultsService
    {
        void Apply(Entity entity);
    }
}
