using Delta.ECS.Unity;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    internal static class EntityViewLifetime
    {
        public static bool TryEnterUpdate(MonoBehaviour view, EntityContext context)
        {
            if (context.World!.IsAlive(context.Entity))
                return true;

            Object.Destroy(view.gameObject);
            return false;
        }
    }
}
