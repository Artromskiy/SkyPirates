namespace DVG.SkyPirates.Tooling.Views
{
    /*

    [SelectionBase]
    public class HexMapToolView : View<IHexMapVM>
    {
        [SerializeField]
        [Dropdown(nameof(Ids))]
        private string _currentTile = TileId.Constants.Land2;
        [SerializeField]
        [Range(0, 30)]
        private int _currentHeight;
        [SerializeField]
        private GameObject _ghostTile;

        private readonly string[] Ids = TileId.Constants.AllIds.
            ToArray().Select(s => s.Value).ToArray();


        private readonly TileVMFactory _tilemapFactory = new();
        private readonly TileViewFactory _tileViewFactory = new();

        private Dictionary<int3, TileId> Map
        {
            get
            {
                if (ViewModel.Map.Data == null)
                    ViewModel.Map = new() { Data = new() };
                return ViewModel.Map.Data;
            }
        }
        private readonly Dictionary<int3, GameObject> _gameObjects = new();

        public override void OnInject() { }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
                _currentHeight++;
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore))
                _currentHeight--;

            MoveGhost();
            if (!Input.GetMouseButton(0) &&
                !Input.GetMouseButton(1))
            {
                return;
            }

            if (Input.GetKey(KeyCode.F) &&
                Input.GetMouseButtonDown(0))
            {
                TryFill();
                return;
            }

            if (Input.GetKey(KeyCode.F) &&
                Input.GetMouseButtonDown(1))
            {
                TryUnfill();
                return;
            }

            var axial = GetAxial();

            Remove(axial);

            if (Input.GetMouseButton(1))
                return;

            Set(axial, new(_currentTile));
        }


        private void MoveGhost()
        {
            var axial = GetAxial();
            var pos = Hex.AxialToWorld(axial);
            if (_ghostTile != null)
                _ghostTile.transform.position = (float3)pos;
        }

        private int3 GetAxial()
        {
            var world = (fix2)GetWorldXZ().xz;
            var axial = Hex.WorldToAxial(world);
            return new int3(axial.x, _currentHeight, axial.y);
        }

        private float3 GetWorldXZ()
        {
            var pos = Input.mousePosition;
            var ray = Camera.main.ScreenPointToRay(pos);
            new Plane(Vector3.down, _currentHeight / 2f).Raycast(ray, out var enter);
            return ray.origin + ray.direction * enter;
        }

        private void Remove(int3 axial)
        {
            Map.Remove(axial);
            if (_gameObjects.Remove(axial, out var go))
                DestroyImmediate(go);
        }

        private void Set(int3 axial, TileId tileId)
        {
            Map[axial] = tileId;

            var vm = _tilemapFactory.Create((ViewModel.Map, axial));
            var view = _tileViewFactory.Create(vm);
            var go = (view as MonoBehaviour).gameObject;
            go.transform.SetParent(transform, true);
            _gameObjects[axial] = go;
        }

        private void TryFill()
        {
            var axial = GetAxial();
            var filled = ViewModel.Map.Data.Keys.Where(s => s.y == axial.y);
            HashSet<int3> _toFill = new(filled);
            Queue<int3> _searching = new();
            _searching.Enqueue(axial);
            int limiter = 1024;
            while (_searching.TryDequeue(out var current) && limiter >= 0)
            {
                foreach (var item in Hex.AxialNear)
                {
                    var near = current + item.x_y;

                    if (!_toFill.Add(near))
                        continue;

                    limiter--;
                    _searching.Enqueue(near);
                }
            }
            _toFill.RemoveWhere(ViewModel.Map.Data.ContainsKey);
            foreach (var item in _toFill)
            {
                Set(item, new(_currentTile));
            }
        }

        private void TryUnfill()
        {
            var axial = GetAxial();
            HashSet<int3> _toUnfill = new();
            Queue<int3> _searching = new();
            _searching.Enqueue(axial);
            int limiter = 1024;
            while (_searching.TryDequeue(out var current) && limiter >= 0)
            {
                foreach (var item in Hex.AxialNear)
                {
                    var near = current + item.x_y;

                    if (!ViewModel.Map.Data.ContainsKey(near) ||
                        !_toUnfill.Add(near))
                        continue;

                    limiter--;
                    _searching.Enqueue(near);
                }
            }
            foreach (var item in _toUnfill)
            {
                Remove(item);
            }
        }
}
    */
}