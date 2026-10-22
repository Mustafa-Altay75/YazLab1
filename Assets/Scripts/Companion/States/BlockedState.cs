using UnityEngine;

/// <summary>
/// Yol bulunamadığında veya hedef kaybolduğunda girilen durum.
/// Oyunu kilitlememek için bir süre bekleyip yeniden dener; nerede
/// kaldığına göre anahtara mı yoksa kapıya mı döneceğine karar verir.
/// </summary>
public class BlockedState : ISkeletonState
{
    public string Name => "Blocked";

    private float enteredAt;

    public void Enter(SkeletonContext context)
    {
        enteredAt = Time.time;
        context.Stop();

        if (context.LogTransitions)
        {
            Debug.LogWarning("İskelet hedefe ulaşamadı, yeniden denenecek.");
        }
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (Time.time - enteredAt < context.RetryDelay)
        {
            return this;
        }

        return context.HasKey ? context.MoveToDoor : context.MoveToKey;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
