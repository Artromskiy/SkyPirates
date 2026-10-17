using Delta.ECS.Unity;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ids;
using System;
using UnityEngine;

namespace SlyPirates.Client.Views.Components
{
    public class UnitComponentView : VisualComponentView<UnitId>
    {
        private const string MaterialName = "VC_Unit_B";
        public override void OnBind(EntityContext context)
        {
            base.OnBind(context);
            ReplaceMaterial(context.Get<TeamId>().Value);
        }

        private void ReplaceMaterial(int teamId)
        {
            var _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(false);
            var materials = _meshRenderer.sharedMaterials;
            int replaceIndex = Array.FindIndex(materials, m => m.name.Contains(MaterialName));
            var replaceMaterial = materials[replaceIndex];
            replaceMaterial = TeamIdToColor.GetReplacementMaterial(replaceMaterial, teamId);
            materials[replaceIndex] = replaceMaterial;
            _meshRenderer.sharedMaterials = materials;
        }
    }
}
