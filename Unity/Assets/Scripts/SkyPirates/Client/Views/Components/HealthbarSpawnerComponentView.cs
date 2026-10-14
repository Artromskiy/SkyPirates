using Delta.ECS.Unity;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    public sealed class HealthbarSpawnerComponentView : MonoBehaviour, IEntityView
    {
        private const string HealthbarPrefabPath = "Prefabs/UI/HealthbarView";

        public void OnBind(EntityContext context)
        {
            GameObject prefab = Resources.Load<GameObject>(HealthbarPrefabPath);
            if (prefab == null)
                return;

            GameObject healthbar = Instantiate(prefab);
            HealthbarComponentView healthbarView = healthbar.GetComponent<HealthbarComponentView>();
            if (healthbarView == null)
            {
                Destroy(healthbar);
                return;
            }

            MonoBehaviour[] behaviours = healthbar.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IEntityView entityView)
                    entityView.OnBind(context);
            }

            HealthbarCanvasView.RegisterSpawnedView(healthbar.transform);
        }
    }
}
