using Delta.ECS.Unity;
using DG.Tweening;
using DVG.SkyPirates.Shared.Components.Runtime;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class DamageBlinkComponentView : MonoBehaviour, IEntityView
    {
        private EntityContext _context;
        [SerializeField]
        private Color _blinkColor = Color.white;

        private static readonly int BaseColorHash = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorHash = Shader.PropertyToID("_Color");
        private const float TweenDuration = 0.1f;

        private float _health;
        private Tween _tween;

        public void OnBind(EntityContext context)
        {
            _context = context;
            _health = Health;
        }

        private void Update()
        {
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            float health = Health;
            if (health >= _health)
            {
                _health = health;
                return;
            }

            _health = health;
            Blink();
        }

        private void Blink()
        {
            _tween?.Kill();

            List<MaterialFlash> flashes = CollectMaterialFlashes();
            if (flashes.Count == 0)
                return;

            float intensity = 0;
            _tween = DOTween.Sequence()
                .Append(DOTween.To(() => intensity, value => intensity = value, 1, TweenDuration))
                .Append(DOTween.To(() => intensity, value => intensity = value, 0, TweenDuration))
                .OnUpdate(() => ApplyFlash(flashes, intensity))
                .OnComplete(() => ApplyFlash(flashes, 0))
                .OnKill(() => ApplyFlash(flashes, 0));
        }

        private List<MaterialFlash> CollectMaterialFlashes()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            var flashes = new List<MaterialFlash>(renderers.Length);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] materials = renderer.sharedMaterials;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == null)
                        continue;

                    int colorProperty = GetColorProperty(material);
                    if (colorProperty == 0)
                        continue;

                    var propertyBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(propertyBlock, materialIndex);
                    Color originalColor = propertyBlock.HasColor(colorProperty)
                        ? propertyBlock.GetColor(colorProperty)
                        : material.GetColor(colorProperty);

                    flashes.Add(new MaterialFlash(renderer, materialIndex, colorProperty, originalColor, propertyBlock));
                }
            }

            return flashes;
        }

        private int GetColorProperty(Material material)
        {
            if (material.HasProperty(BaseColorHash))
                return BaseColorHash;

            return material.HasProperty(ColorHash) ? ColorHash : 0;
        }

        private void ApplyFlash(List<MaterialFlash> flashes, float intensity)
        {
            for (int i = 0; i < flashes.Count; i++)
            {
                MaterialFlash flash = flashes[i];
                if (flash.Renderer == null)
                    continue;

                flash.PropertyBlock.SetColor(
                    flash.ColorProperty,
                    Color.Lerp(flash.OriginalColor, _blinkColor, intensity));
                flash.Renderer.SetPropertyBlock(flash.PropertyBlock, flash.MaterialIndex);
            }
        }

        private float Health => (float)_context.Get<Health>().Value;

        private void OnDestroy() => _tween?.Kill();

        private readonly struct MaterialFlash
        {
            public readonly Renderer Renderer;
            public readonly int MaterialIndex;
            public readonly int ColorProperty;
            public readonly Color OriginalColor;
            public readonly MaterialPropertyBlock PropertyBlock;

            public MaterialFlash(
                Renderer renderer,
                int materialIndex,
                int colorProperty,
                Color originalColor,
                MaterialPropertyBlock propertyBlock)
            {
                Renderer = renderer;
                MaterialIndex = materialIndex;
                ColorProperty = colorProperty;
                OriginalColor = originalColor;
                PropertyBlock = propertyBlock;
            }
        }
    }
}
