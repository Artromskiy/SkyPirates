using DVG.Core;
using DVG.MathsOld;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;
using System.Collections.Generic;



namespace DVG.SkyPirates.Server.Presenters
{
    public class SquadPm : Presenter, ITickable
    {
        private readonly List<UnitPm> _units = new();
        private readonly InputPm _input;

        private readonly IPathFactory<PackedCirclesModel> _circlesModelFactory;
        private PackedCirclesModel _packedCircles;

        private float3[] _targetPositions;

        public SquadPm(InputPm input, IPathFactory<PackedCirclesModel> circlesModelFactory)
        {
            _input = input;
            _circlesModelFactory = circlesModelFactory;
            UpdatePackedCircles(1);
        }

        public void AddUnit(UnitPm unit)
        {
            UpdatePackedCircles(_units.Count + 1);
            _units.Add(unit);
        }

        private void UpdatePackedCircles(int count)
        {
            _packedCircles = _circlesModelFactory.Create("Configs/PackedCircles/PackedCirclesModel" + count);
            _targetPositions = new float3[count];
        }

        public void Tick()
        {
            for (int i = 0; i < _targetPositions.Length; i++)
                _targetPositions[i] = _input.Position + angle.rotate(_packedCircles.points[i] * 0.5f, _input.Rotation).x_y;

            ReorderUnitsToNearest(_targetPositions);
            for (int i = 0; i < _units.Count; i++)
                _units[i].TargetPosition = _targetPositions[i];

            foreach (var item in _units)
                item.Tick();
        }

        private void ReorderUnitsToNearest(float3[] targets)
        {
            int count = Maths.Min(targets.Length, _units.Count);
            for (int i = 0; i < count; i++)
            {
                var posI = _units[i].Position;
                float firstDist = float2.SqrDistance(posI.xz, targets[i].xz);
                if (firstDist <= 1)
                    continue;

                int swapIndex = -1;
                float minSwap = float.MaxValue;
                for (int j = i + 1; j < count; j++)
                {
                    var posJ = _units[j].Position;
                    var swapSum = float2.SqrDistance(posI.xz, targets[j].xz) + float2.SqrDistance(posJ.xz, targets[i].xz);
                    if (swapSum < minSwap && swapSum < firstDist + float2.SqrDistance(posJ.xz, targets[j].xz))
                    {
                        minSwap = swapSum;
                        swapIndex = j;
                    }
                }
                if (swapIndex != -1)
                    (_units[swapIndex], _units[i]) = (_units[i], _units[swapIndex]);
            }
        }
    }
}
