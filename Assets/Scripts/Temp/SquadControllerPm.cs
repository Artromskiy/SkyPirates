using DVG.Maths;
using System;
using System.Collections.Generic;
using UnityEngine;
using static DVG.Maths.math;
using static DVG.Maths.vec2;
using static DVG.Maths.vec3;

[Obsolete]
public class SquadControllerPm
{
    /*
        public vec3 startPosition;
        public vec2 direction;
        public bool groupUnits;
        public bool useSkill;
        public float onUpdate;


    public const float AgressionRadius = 4;

    private const float LerpTime = 0.15f;
    private static float _colorLerp = 1f;
    private static readonly Color _enemiesColor = new(0f, 0.7f, 1f, 1f);
    private static readonly Color _simpleColor = new(0f, 0.7f, 1f, 0.5f);

    private CharacterController _ctrl;
    public bool Immortal;
    public vec3 ReactPosition;
    private angle _angle;

    public readonly List<UnitPm> Units = new();
    public readonly List<UnitPm> UnitsArranged = new();
    public readonly List<UnitPm> avaliableTargets = new();
    public float Radius;
    public float UnitsRadius;

    private bool _moveToEnd;
    private vec3 _savedMiddlePos;

    private readonly int WorldCircleColor = Shader.PropertyToID("_WorldCircleColor1");
    private readonly int WorldCircleCenter = Shader.PropertyToID("_WorldCircleCenter");
    private readonly int WorldCircleRadius = Shader.PropertyToID("_WorldCircleRadius");

    public SquadControllerPm()
    {

    }

    public void AddUnit(UnitPm unit)
    {
        unit.Health.OnDead += () => RemoveUnit(unit);
        Units.Add(unit);
        UnitsArranged.Add(unit);
        unit.ControllerEnabled = true;
        unit.LockUpdate = false;
        unit.Health.Immortal = Immortal;
        _moveToEnd = true;
    }

    public void RemoveUnit(UnitPm unit)
    {
        UnitsArranged.Remove(unit);
        Units.Remove(unit);
    }

    private void Update(float deltaTime)
    {
        vec2 commDirection = GetDirection(out var leveldSpeed);
        MoveController(commDirection, 7 * leveldSpeed, out var moveToUnits);

        SetAttackState();
        _moveToEnd |= groupUnits;
        bool attack = !groupUnits && avaliableTargets != null && TargetsInRange();
        if (!attack && !moveToUnits)
        {
            SimpleMoveUnits(deltaTime);
        }
        else
        {
            SimpleAttackUnits();
            _savedMiddlePos = ReactPosition + Vector3.ClampMagnitude(CalculateMiddlePos().ToY0() - ReactPosition, Radius - UnitsRadius);
            _moveToEnd = false;
        }
        //_onSquadUpdate?.Invoke(update);
    }

    private bool TargetsInRange()
    {
        foreach (var t in avaliableTargets)
        {
            if (MathHelper.DistanceLessXZ(t.Position, ReactPosition, Radius))
                return true;
        }
        return false;
    }

    private void UpdateRadius(float deltaTime)
    {
        //UnitsRadius = Positioner.GetCircleRadius(Units.Count) * 0.5f;
        Radius = AgressionRadius + UnitsRadius;
    }

    private void UpdateShaderData(float update)
    {
        var pos = ReactPosition;
        _colorLerp = moveto(_colorLerp, groupUnits ? LerpTime : 0, update);
        Shader.SetGlobalVector(WorldCircleCenter, new Vector4(pos.x, pos.z, 0, 0));
        Shader.SetGlobalFloat(WorldCircleRadius, Radius);
        Shader.SetGlobalColor(WorldCircleColor, Color.Lerp(_enemiesColor, _simpleColor, _colorLerp / LerpTime));
    }

    private vec2 GetDirection(out float leveldSpeed)
    {
        vec2 commDirection = direction == null ? vec2.zero : direction;
        commDirection = sqrlength(commDirection) > 1 ? normalize(commDirection) : commDirection;

        var mag = length(commDirection);
        leveldSpeed = mag < 0.3f ? 0 : 1;

        return commDirection.isZero ? commDirection : normalize(commDirection);
    }


    private void SimpleMoveUnits(float deltaTime)
    {
        var count = Units.Count;
        Span<vec3> positions = stackalloc vec3[Units.Count];
        GetPositions(positions, _moveToEnd ? ReactPosition : _savedMiddlePos.zeroY(), _angle);
        RaycastTargets(positions);
        NearestTarget(positions);
        for (int i = 0; i < count; i++)
        {
            var unit = Units[i];
            var unitDir = unit.Direction;
            vec2 nearPlayerPos = positions[i].noY();
            var direction = nearPlayerPos - unit.Position.noY();
            direction = sqrlength(direction) > 1 ? normalize(direction) : direction;
            direction = Vector2.MoveTowards(unitDir, direction, deltaTime * 10); //smoothing with max time 0.25 sec.
            unit.Direction = direction;
        }
    }

    private void SetAttackState()
    {
        int count = Units.Count;
        for (int i = 0; i < count; i++)
        {
            var unit = Units[i];
            unit.CanAttack = !groupUnits;
        }
    }

    private void SetImmortality(bool immortality)
    {
        foreach (var unit in Units)
        {
            unit.Health.Immortal = immortality;
        }
    }

    private void SimpleAttackUnits()
    {
        int count = Units.Count;
        for (int i = 0; i < count; i++)
        {
            var unit = Units[i];
            if (unit.State == UnitPm.UnitState.Move && unit.CurrentEnemy != null &&
                MathHelper.DistanceLessXZ(unit.Position, ReactPosition, Radius))
            {
                unit.Direction = vec2.zero;
                continue;
            }
            float minSqrDistance = float.MaxValue;
            UnitPm t = null;
            foreach (var item in avaliableTargets)
            {
                var sqrDistance = item.Position.DistanceSqrdXZ(unit.Position);
                if (sqrDistance < minSqrDistance)
                {
                    t = item;
                    minSqrDistance = sqrDistance;
                }
            }
            if (t == null)
            {
                unit.Direction = vec2.zero;
            }
            else
            {
                var direction = unit.GetAttackDirection(t.Position.xz);
                unit.Direction = normalize(direction) * 0.5f;
            }
        }
    }

    private void MoveController(vec2 direction, float speed, out bool moveToUnits)
    {
        var middlePos = CalculateMiddlePos();
        var distanceToMiddle = middlePos.DistanceSqrdXZ(ReactPosition);

        var inputDirection = direction * speed;
        _ctrl.SimpleMove((direction * speed).ToY0());
        direction = _ctrl.velocity.NoY();
        direction = length(direction) / speed > 0.1f ? normalize(direction) : vec2.zero;
        _ctrl.enabled = false;
        _ctrl.transform.position = ReactPosition;
        _ctrl.enabled = true;
        _ctrl.SimpleMove((direction * speed).ToY0());
        if (!inputDirection.isZero)
            _angle = new angle(inputDirection);

        ReactPosition = _ctrl.transform.position.ToY0();

        moveToUnits = middlePos.DistanceSqrdXZ(ReactPosition) < distanceToMiddle;

        if (!moveToUnits)
            return;

        _ctrl.SimpleMove((speed * 0.5f * direction).ToY0());
        direction = _ctrl.velocity.NoY();
        direction = length(direction) / speed > 0.1f ? normalize(direction) : vec2.zero;
        _ctrl.enabled = false;
        _ctrl.transform.position = ReactPosition;
        _ctrl.enabled = true;
        _ctrl.SimpleMove((direction * speed).ToY0());
        ReactPosition = _ctrl.transform.position.ToY0();
    }

    private vec2 CalculateMiddlePos()
    {
        if (Units.Count == 0)
            return ReactPosition.xz;
        vec2 min = new Vector2(float.MaxValue, float.MaxValue);
        vec2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < Units.Count; i++)
        {
            var vec = Units[i].Position.xz;
            min = vec2.min(min, vec);
            max = vec2.max(max, vec);
        }
        vec2 middle = (max + min) / 2f;
        return middle;
    }

    private void RaycastTargets(Span<vec3> targets)
    {
        var from = ReactPosition;
        var fromOffset = from + vec3.up;
        float radius = 0.3f;
        for (int i = 0; i < targets.Length; i++)
        {
            var direction = targets[i] - from;
            int layers = 0; // Layers.ObstacleMask | Layers.FakeObstacleMask
            if (Physics.Raycast(fromOffset, direction, out var hit, length(direction), layers))
                targets[i] = new Plane(hit.normal.ToY0(), hit.point.ToY0()).ClosestPointOnPlane(targets[i]).ToY0() + (hit.normal * radius);
            Debug.DrawLine(from, targets[i], Color.red);
        }
    }

    private void NearestTarget(Span<vec3> targets)
    {
        int count = targets.Length;
        for (int i = 0; i < count; i++)
        {
            var posI = Units[i].Position;
            float firstDist = posI.DistanceSqrdXZ(targets[i]);
            if (firstDist <= 1)
                continue;

            int swapIndex = -1;
            float minSwap = float.MaxValue;
            for (int j = i + 1; j < count; j++)
            {
                var posJ = Units[j].Position;
                var swapSum = posI.DistanceSqrdXZ(targets[j]) + posJ.DistanceSqrdXZ(targets[i]);
                if (swapSum < minSwap && swapSum < firstDist + posJ.DistanceSqrdXZ(targets[j]))
                {
                    minSwap = swapSum;
                    swapIndex = j;
                }
            }
            if (swapIndex != -1)
                (Units[swapIndex], Units[i]) = (Units[i], Units[swapIndex]);
        }
    }

    private void CreateCtrl()
    {
        var go = new GameObject("CTRL");
        go.transform.position = ReactPosition;
        //go.layer = Layers.ObstacleMoverNumber;
        _ctrl = go.AddComponent<CharacterController>();
        _ctrl.radius = 0.1f;
        _ctrl.center = _ctrl.height * 0.5f * Vector3.up;
        _ctrl.enableOverlapRecovery = false;
    }


    private void GetPositions(Span<vec3> positions, vec3 targetPosition, angle angle)
    {
        //Positioner.GetCirclePositions(positions, 0.5f);
        //Positioner.NormalizePointsBox(positions);
        //Positioner.RotateSpan(positions, targetPosition, angle);
    }
    */
}
