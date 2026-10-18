using Delta;
using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Config;
using UnityEngine;

namespace SlyPirates.Client.Views.Components.Animators
{
    public class MoveAnimatorComponentView : MonoBehaviour, IEntityView, IVisualInstanceLoaded
    {
        private const string LocomotionBlendGroup = "Locomotion";

        private float3 _prevPosition;
        private AnimView _anim;
        private Transform _cachedTransform;
        private EntityContext _context;

        public void OnBind(EntityContext context)
        {
            _cachedTransform = transform;
            _prevPosition = _cachedTransform.position;
            _context = context;
        }

        public void OnVisualInstanceLoaded(GameObject visual)
        {
            _anim = visual.GetComponentInChildren<AnimView>(true);
            _prevPosition = _cachedTransform.position;
        }

        private void Update()
        {
            if (_anim == null || !_anim.IsReady)
            {
                return;
            }

            Transform cachedTransform = _cachedTransform;
            float3 currentPosition = cachedTransform.position;
            var worldVelocity = (currentPosition - _prevPosition) / Time.deltaTime;
            _prevPosition = currentPosition;

            var localVelocity = cachedTransform.InverseTransformVector(worldVelocity) / (float)_context.Get<MaxSpeed>().Value;
            _anim.Blend2D(LocomotionBlendGroup, new Vector2(localVelocity.x, localVelocity.z), 0f);
        }
    }
}
