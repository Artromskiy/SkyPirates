using DVG.Core;
using DVG.MathsOld;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;
using System.Collections.Generic;

using VContainer.Unity;

namespace DVG.SkyPirates.Server.Presenters
{
    public class SquadPm : Presenter, ITickable
    {
        private readonly List<UnitPm> _units = new();
        private readonly InputPm _input;

        private readonly IPathFactory<PackedCirclesModel> _circlesModelFactory;
        private PackedCirclesModel _packedCircles;

        private vec3[] _targetPositions;

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
            _targetPositions = new vec3[count];
        }

        public void Tick()
        {
            for (int i = 0; i < _targetPositions.Length; i++)
                _targetPositions[i] = _input.Position + angle.rotate(_packedCircles.points[i] * 0.5f, _input.Rotation).x0y;

            ReorderUnitsToNearest(_targetPositions);
            for (int i = 0; i < _units.Count; i++)
                _units[i].TargetPosition = _targetPositions[i];

            foreach (var item in _units)
                item.Tick();
        }

        private void ReorderUnitsToNearest(vec3[] targets)
        {
            int count = math.min(targets.Length, _units.Count);
            for (int i = 0; i < count; i++)
            {
                var posI = _units[i].Position;
                float firstDist = posI.DistanceSqrdXZ(targets[i]);
                if (firstDist <= 1)
                    continue;

                int swapIndex = -1;
                float minSwap = float.MaxValue;
                for (int j = i + 1; j < count; j++)
                {
                    var posJ = _units[j].Position;
                    var swapSum = posI.DistanceSqrdXZ(targets[j]) + posJ.DistanceSqrdXZ(targets[i]);
                    if (swapSum < minSwap && swapSum < firstDist + posJ.DistanceSqrdXZ(targets[j]))
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
