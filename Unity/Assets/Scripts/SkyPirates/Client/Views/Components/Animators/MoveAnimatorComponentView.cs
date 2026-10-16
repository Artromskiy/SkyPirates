using Delta;
using Delta.ECS.Unity;
using DVG.SkyPirates.Shared.Components.Config;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components.Animators
{
    public class MoveAnimatorComponentView : MonoBehaviour, IEntityView
    {
        private const string LocomotionBlendGroup = "Locomotion";

        private float3 _prevPosition;
        private EntityContext _context;
        private AnimView _anim;
        private Transform _cachedTransform;
        private float _maxSpeed;

        private void Awake()
        {
            CacheComponents();
        }

        public void OnBind(EntityContext context)
        {
            _context = context;
            CacheComponents();
            _prevPosition = _cachedTransform.position;
            _maxSpeed = (float)context.Get<MaxSpeed>().Value;
        }

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            Transform cachedTransform = _cachedTransform;
            float3 currentPosition = cachedTransform.position;
            var worldVelocity = (currentPosition - _prevPosition) / Time.deltaTime;
            _prevPosition = currentPosition;
            var localVelocity = cachedTransform.InverseTransformVector(worldVelocity) / _maxSpeed;

            if (_anim != null && _anim.IsReady)
                _anim.Blend2D(LocomotionBlendGroup, new Vector2(localVelocity.x, localVelocity.z), 0f);
        }

        private void CacheComponents()
        {
            if (_cachedTransform == null)
                _cachedTransform = transform;

            if (_anim == null)
                _anim = GetComponentInChildren<AnimView>();
        }

    }
}
