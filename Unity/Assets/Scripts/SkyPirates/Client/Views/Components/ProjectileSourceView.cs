using Delta.ECS.Unity;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public sealed class ProjectileSourceView : MonoBehaviour, IEntityView
    {
        private const string MuzzlePointName = "ProjectileMuzzle";
        private Transform? _muzzlePoint;

        internal Transform? MuzzlePoint => _muzzlePoint;

        public void OnBind(EntityContext context)
        {
            _muzzlePoint = null;
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name != MuzzlePointName)
                {
                    continue;
                }

                _muzzlePoint = transforms[i];
                return;
            }
        }
    }
}
