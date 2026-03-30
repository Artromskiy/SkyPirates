using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ids;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public class UnitComponentView : VisualComponentView<UnitId>
    {
        private const string MaterialName = "VC_Unit_B";
        public override void OnInject()
        {
            base.OnInject();
            ReplaceMaterial(ViewModel.Get<TeamId>().Value);
        }

        private void ReplaceMaterial(int teamId)
        {
            var _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(false);
            var materials = _meshRenderer.sharedMaterials;
            var replaceIndex = Array.FindIndex(materials, m => m.name.Contains(MaterialName));
            var replaceMaterial = materials[replaceIndex];
            replaceMaterial = TeamIdToColor.GetReplacementMaterial(replaceMaterial, teamId);
            materials[replaceIndex] = replaceMaterial;
            _meshRenderer.sharedMaterials = materials;
        }
    }
}