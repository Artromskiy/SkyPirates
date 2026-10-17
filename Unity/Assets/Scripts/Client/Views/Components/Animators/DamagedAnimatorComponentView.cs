using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Client.Views.Components;
using UnityEngine;

namespace SlyPirates.Client.Views.Components.Animators
{
    public class DamagedAnimatorComponentView : MonoBehaviour, IEntityView, IVisualInstanceLoaded
    {
        private float _prevHealth;
        private EntityContext _context;
        private AnimView _animView;

        private void Awake() => CacheComponents();

        public void OnBind(EntityContext context)
        {
            _context = context;
            CacheComponents();
            _prevHealth = (float)_context.Get<Health>().Value;
        }

        public void OnVisualInstanceLoaded()
        {
            CacheComponents();
            _prevHealth = (float)_context.Get<Health>().Value;
        }

        private void Update()
        {
            float health = (float)_context.Get<Health>().Value;
            bool healthChanged = health < _prevHealth;
            _prevHealth = health;
            if (!healthChanged)
            {
                return;
            }

            if (_animView != null && _animView.IsReady)
            {
                _animView.PlayOnce("Reaction.Damage");
            }
        }

        private void CacheComponents()
        {
            if (_animView == null)
            {
                _animView = GetComponentInChildren<AnimView>();
            }
        }

    }
}
