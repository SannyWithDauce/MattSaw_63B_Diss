public interface IDamageable
{
    void TakeDamage(float amount);
    float CurrentHealth { get; }
    bool IsAlive { get; }
}