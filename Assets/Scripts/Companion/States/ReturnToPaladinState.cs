using UnityEngine;

/// <summary>
/// Altınları alan iskelet Paladin'in kapısının önüne döner.
///
/// Burada beklemesinin sebebi, oyuncunun altınları iskeletin elinden
/// alması gerekmesi: teslim noktası oyuncunun kendisi değil kapının önü,
/// çünkü oyuncu o sırada konuşmanın içinde olabilir.
/// </summary>
public class ReturnToPaladinState : ISkeletonState
{
    private const int MaxRepath = 3;

    public string Name => "ReturnToPaladin";

    private int repathTries;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;
        if (context.Agent != null) context.Agent.updateRotation = true;
        if (context.CoinDropPoint == null) return;

        context.SetDestination(context.CoinDropPoint.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.CoinDropPoint == null) return context.Blocked;
        if (context.PathFailed()) return context.Blocked;

        if (!context.HasArrived())
        {
            if (context.IsStuck())
            {
                repathTries++;

                if (context.LogTransitions)
                {
                    string olcum = context.Sensor != null ? context.Sensor.Ozet() : "sensör yok";
                    Debug.LogWarning("İskelet altınları getirirken takıldı (" + repathTries + "/"
                                     + MaxRepath + "). Sensör: " + olcum);
                }

                if (repathTries > MaxRepath) return context.Blocked;
                if (!context.Repath(context.CoinDropPoint.position)) return context.Blocked;
            }

            return this;
        }

        if (!context.IsNear(context.CoinDropPoint.position)) return context.Blocked;

        // Görev burada bitiyor; altınları oyuncu kendi eliyle alacak.
        context.CoinsReady = true;
        return context.WaitingWithCoins;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
