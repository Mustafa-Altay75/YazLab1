using UnityEngine;

/// <summary>
/// Bir altını alır. Yeterince toplanmadıysa bir sonrakine döner.
///
/// Tek tek toplamak kasıtlı: iskelet merkez odada altından altına
/// yürüyor ve oyuncu bunu izliyor. Hepsini tek seferde yok etmek aynı
/// işi görürdü ama ajanın çalıştığını göstermezdi.
/// </summary>
public class CollectCoinState : ISkeletonState
{
    public string Name => "CollectCoin";

    private float enteredAt;

    public void Enter(SkeletonContext context)
    {
        enteredAt = Time.time;
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (Time.time - enteredAt < context.PickUpDuration * 0.5f) return this;

        context.TakeCoin();

        if (context.CoinsCollected >= context.CoinsNeeded) return context.ReturnToPaladin;

        return context.MoveToCoins;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
