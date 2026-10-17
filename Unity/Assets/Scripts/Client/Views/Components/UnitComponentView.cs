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
        protected override void OnVisualLoaded(GameObject visual)
        {
            ReplaceMaterial(visual, Context.Get<TeamId>().Value);
        }

        private static void ReplaceMaterial(GameObject visual, int teamId)
        {
            var meshRenderer = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var materials = meshRenderer.sharedMaterials;
            int replaceIndex = Array.FindIndex(materials, m => m.name.Contains(MaterialName));
            var replaceMaterial = materials[replaceIndex];
            replaceMaterial = TeamIdToColor.GetReplacementMaterial(replaceMaterial, teamId);
            materials[replaceIndex] = replaceMaterial;
            meshRenderer.sharedMaterials = materials;
        }
    }
}
