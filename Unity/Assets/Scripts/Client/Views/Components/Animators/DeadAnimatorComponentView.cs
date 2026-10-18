using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SlyPirates.Client.Views.Components.Animators
{
    public class DeadAnimatorComponentView : MonoBehaviour, IEntityView, IVisualInstanceLoaded
    {
        private bool _animViewState;
        private bool _hasAnimViewState;
        private EntityContext _context;
        private AnimView _animView;

        private const string DeadLayerName = "DeadLayer";

        public void OnBind(EntityContext context)
        {
            _context = context;
        }

        public void OnVisualInstanceLoaded(GameObject visual)
        {
            _animView = visual.GetComponentInChildren<AnimView>(true);
            _hasAnimViewState = false;
        }

        private void Update()
        {
            bool dead = !_context.Has<Alive>();

            if (_animView == null || !_animView.IsReady)
            {
                return;
            }

            if (!_hasAnimViewState || _animViewState != dead)
            {
                _animViewState = dead;
                _hasAnimViewState = true;
                if (dead)
                {
                    _animView.Play("Life.Dead");
                }
                else
                {
                    _animView.StopLayer(DeadLayerName);
                }
            }
        }

    }
}
