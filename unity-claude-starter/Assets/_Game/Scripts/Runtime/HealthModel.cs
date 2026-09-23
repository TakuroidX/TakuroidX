using System;

namespace Game
{
    /// <summary>
    /// Pure C# health logic, independent of Unity and testable in EditMode.
    /// </summary>
    public sealed class HealthModel
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        /// <summary>(current, max)</summary>
        public event Action<int, int> Changed;
        public event Action Died;

        public HealthModel(int max)
        {
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max), "max must be positive");
            Max = max;
            Current = max;
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsDead || amount == 0) return;

            Current = Math.Max(0, Current - amount);
            Changed?.Invoke(Current, Max);
            if (IsDead) Died?.Invoke();
        }

        public void Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsDead || amount == 0) return;

            Current = Math.Min(Max, Current + amount);
            Changed?.Invoke(Current, Max);
        }
    }
}
