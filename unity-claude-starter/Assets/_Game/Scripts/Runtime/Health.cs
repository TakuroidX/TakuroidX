using UnityEngine;
using UnityEngine.Events;

namespace Game
{
    /// <summary>
    /// Thin MonoBehaviour wrapper that connects HealthModel to the Inspector and events.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField] private UnityEvent onDied = new UnityEvent();

        public HealthModel Model { get; private set; }

        private void Awake()
        {
            Model = new HealthModel(maxHealth);
            Model.Died += onDied.Invoke;
        }

        public void TakeDamage(int amount) => Model.TakeDamage(amount);
        public void Heal(int amount) => Model.Heal(amount);
    }
}
