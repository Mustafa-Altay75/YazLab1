using UnityEngine;

/// <summary>
/// Kılıcı alır: masadaki kopya kaybolur, elindeki görünür.
///
/// İki ayrı nesne olmasının sebebi, el kılıcının kemiğe (handslot.r)
/// bağlı durması ve animasyonla birlikte hareket etmesi gerekmesi.
/// Masadakini taşımak yerine birini gizleyip diğerini göstermek hem daha
/// basit hem de ölçek/dönüş sorunu çıkarmıyor.
/// </summary>
public class PickUpSwordState : ISkeletonState
{
    public string Name => "PickUpSword";

    private float enteredAt;

    public void Enter(SkeletonContext context)
    {
        enteredAt = Time.time;
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (Time.time - enteredAt < context.PickUpDuration) return this;

        if (context.SwordObject != null) context.SwordObject.SetActive(false);
        if (context.SwordInHand != null) context.SwordInHand.SetActive(true);

        context.Armed = true;
        return context.MoveToThreat;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
