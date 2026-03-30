using DG.Tweening;
using DVG.SkyPirates.Shared.Components.Runtime;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class DamageBlinkComponentView : ComponentView
    {
        [SerializeField]
        private Color _blinkColor = Color.white;
        private MeshRenderer[] _meshRenderers;
        private MeshRenderer[] MeshRenderers =>
            _meshRenderers == null || Array.Exists(_meshRenderers, mr => mr == null) ?
            _meshRenderers = GetComponentsInChildren<MeshRenderer>() : _meshRenderers;

        private MaterialPropertyBlock _propBlocks;
        private MaterialPropertyBlock PropBlocks => _propBlocks ??= new();

        private static readonly int EmissionColorHash = Shader.PropertyToID("_EmissionColor");
        private const float TweenDuration = 0.2f / 2;
        private float _health;
        private Tween _tween;

        public override void OnInject()
        {
            _health = Health;
        }

        public override void Tick()
        {
            if (Health >= _health)
            {
                _health = Health;
                return;
            }

            _health = Health;
            _tween?.Kill();
            Blink();
        }

        private void Blink()
        {
            var propBlock = new MaterialPropertyBlock();
            MeshRenderers[0].GetPropertyBlock(PropBlocks);

            _tween = DOTween.Sequence().
            Append(DOTween.To(
                () => propBlock.GetColor(EmissionColorHash),
                value => propBlock.SetColor(EmissionColorHash, value), _blinkColor, TweenDuration)).

            Append(DOTween.To(
                () => propBlock.GetColor(EmissionColorHash),
                value => propBlock.SetColor(EmissionColorHash, value), Color.black, TweenDuration));

            _tween.OnUpdate(() =>
            {
                for (int i = 0; i < MeshRenderers.Length; i++)
                    MeshRenderers[i].SetPropertyBlock(propBlock);
            });
        }

        private float Health => (float)ViewModel.Get<Health>().Value;
    }
}
