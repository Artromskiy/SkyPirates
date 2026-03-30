using DVG.Core;
using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;
using System.Collections;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.Controls
{
    [Inject]
    public class HexMapSpawnController : MonoBehaviour, IView
    {
        [Inject]
        private readonly IEntityRegistry _entityRegistryService;
        [Inject]
        private readonly IEntityFactory _commandEntityFactory;
        [Inject]
        private readonly IHexMapFactory _hexMapFactory;

        private IEnumerator Start()
        {
            yield return new WaitWhile(() => _hexMapFactory is null);

            var entityId = _entityRegistryService.Reserve();
            var entity = _hexMapFactory.Create(entityId.Value);
        }
    }
}