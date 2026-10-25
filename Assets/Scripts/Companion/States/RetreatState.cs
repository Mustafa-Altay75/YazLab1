using UnityEngine;

/// <summary>
/// İskelet, işi bitince belirlenen noktaya çekilir ve orada bekler.
///
/// Altınları teslim ettikten sonra takip moduna dönmesi sorun çıkarıyordu:
/// Paladin'in dibinde duran iskelet, oyuncunun etkileşim menziline önce
/// kendisi giriyor ve altınları Paladin'e vermek zorlaşıyordu.
/// PlayerInteraction en yakın IInteractable'ı seçiyor, iskelet de bir
/// tane taşıyor.
///
/// Takip bayrağı burada kapalı tutuluyor; iskelet çağrılana kadar
/// çekildiği yerde kalıyor.
/// </summary>
public class RetreatState : ISkeletonState
{
    private const int MaxRepath = 3;

    public string Name => "Retreat";

    private int repathTries;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;
        context.FollowRequested = false;

        if (context.Agent != null) context.Agent.updateRotation = true;
        if (context.RetreatPoint == null) return;

        context.SetDestination(context.RetreatPoint.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        // Yeni bir emir gelirse çekilmeyi bırakır.
        if (context.CoinOrderGiven) return context.MoveToCoins;
        if (context.SwordOrderGiven) return context.MoveToSword;
        if (context.PlateOrderGiven) return context.MoveToPlate;
        if (context.FollowRequested) return context.Following;

        if (context.RetreatPoint == null) return context.Completed;
        if (context.PathFailed()) return context.Completed;

        if (!context.HasArrived())
        {
            if (context.IsStuck())
            {
                repathTries++;
                if (repathTries > MaxRepath) return context.Completed;
                if (!context.Repath(context.RetreatPoint.position)) return context.Completed;
            }

            return this;
        }

        return context.Completed;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
