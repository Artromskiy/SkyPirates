using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Health Class
    /// </summary>
    /// 
    [Obsolete]
    public interface IBaseHealth
    {
        AttackerData? ExtendedData { get; set; }
        event Action<float> OnDamage;
        event Action<float> OnHeal;
        event Action<float> OnPercentChanged;
        event Action<int> OnShields;
        event Action OnImmortalityHit;
        event Action OnDead;

        bool Immortal { get; set; }
        float MaxPoints { get; }
        int Shields { get; set; }
        float Points { get; set; }
        float Percentage { get; set; }
        void Respawn();
        void Respawn(float maxPoints);
        void ClearEvents();
    }
    [Obsolete]
    public interface IHealth<out T> : IBaseHealth
    {
        event Action<T, float> OnDamageDetailed;
        event Action<T, float> OnHealDetailed;
        event Action<T> OnDeadDetailed;
        event Action<T, int> OnShieldsDetailed;
    }
    [Obsolete]
    public struct AttackerData
    {
        public bool player;
        public Vector3 sourceDir;
    }
    [Obsolete]
    public class BaseHealth : IBaseHealth
    {
        private float _points;
        private float _maxPoints;
        private int _shields;
        public event Action<float> OnDamage;
        public event Action<float> OnHeal;
        public event Action<float> OnPercentChanged;
        public event Action<int> OnShields;
        public event Action OnImmortalityHit;
        public event Action OnDead;

        public AttackerData? ExtendedData { get; set; }
        public bool Immortal { get; set; }
        public float MaxPoints => _maxPoints;
        public int Shields
        {
            get => _shields;
            set
            {
                if (value != _shields)
                {
                    RiseOnShields(value);
                    _shields = value;
                }
            }
        }
        public virtual float Points
        {
            get => _points;
            set
            {
                SetHealth(value);
                ExtendedData = null;
            }
        }

        public virtual float Percentage
        {
            get => Points / _maxPoints;
            set
            {
                value = Mathf.Clamp01(value);
                SetHealth(_maxPoints * value);
            }
        }

        private void SetHealth(float value)
        {
            value = Mathf.Clamp(value, 0, _maxPoints);
            if (value < _points && Shields > 0)
            {
                value = _points;
                Shields--;
            }
            if (Immortal && value == 0)
            {
                value = _points;
                OnImmortalityHit?.Invoke();
            }

            if (value != _points)
                OnPercentChanged?.Invoke(value / _maxPoints);
            if (value < _points)
                RiseOnDamage(value);
            if (value > _points)
                RiseOnHeal(value);
            if (value == 0 && _points != 0)
                RiseOnDead();

            _points = value;
        }

        public BaseHealth(float points, float maxPoints)
        {
            Immortal = false;
            _maxPoints = maxPoints;
            _points = points;
        }


        public void Respawn()
        {
            _points = _maxPoints;
            Shields = 0;
            OnPercentChanged?.Invoke(1);
        }

        public void Respawn(float maxPoints)
        {
            _maxPoints = maxPoints;
            Respawn();
        }

        public virtual void ClearEvents()
        {
            OnDamage = null;
            OnHeal = null;
            OnDead = null;
            OnPercentChanged = null;
        }

        protected virtual void RiseOnDamage(float value)
        {
            OnDamage?.Invoke(value);
        }

        protected virtual void RiseOnHeal(float value)
        {
            OnHeal?.Invoke(value);
        }

        protected virtual void RiseOnDead()
        {
            OnDead?.Invoke();
        }

        protected virtual void RiseOnShields(int shields)
        {
            OnShields?.Invoke(shields);
        }
    }
    [Obsolete]
    public class Health<T> : BaseHealth, IHealth<T>
    {
        private readonly T _parent;
        public event Action<T, float> OnDamageDetailed;
        public event Action<T, float> OnHealDetailed;
        public event Action<T> OnDeadDetailed;
        public event Action<T, int> OnShieldsDetailed;

        public override void ClearEvents()
        {
            base.ClearEvents();

            OnDeadDetailed = null;
            OnDamageDetailed = null;
            OnHealDetailed = null;
            OnShieldsDetailed = null;
        }

        public Health(T parent, float points, float maxPoints) : base(points, maxPoints)
        {
            _parent = parent;
        }

        protected override void RiseOnDead()
        {
            base.RiseOnDead();
            OnDeadDetailed?.Invoke(_parent);
        }
        protected override void RiseOnDamage(float value)
        {
            base.RiseOnDamage(value);
            OnDamageDetailed?.Invoke(_parent, value);
        }

        protected override void RiseOnShields(int shields)
        {
            base.RiseOnShields(shields);
            OnShieldsDetailed?.Invoke(_parent, shields);
        }
        protected override void RiseOnHeal(float value)
        {
            base.RiseOnHeal(value);
            OnHealDetailed?.Invoke(_parent, value);
        }
    }
}
