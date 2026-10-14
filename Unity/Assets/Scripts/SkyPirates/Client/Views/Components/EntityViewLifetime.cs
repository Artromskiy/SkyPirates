using Delta.ECS.Unity;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    internal static class EntityViewLifetime
    {
        public static bool TryEnterUpdate(MonoBehaviour view, EntityContext context)
        {
            if (!context.IsAlive)
            {
                Object.Destroy(view.gameObject);
                return false;
            }

            return true;
        }
    }
}
