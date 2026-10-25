using UnityEngine;

/// <summary>
/// Masanın üstündeki kılıca yürür.
///
/// MoveToKey ile aynı kalıp, ama bu yolculuk projenin en uzunu: emir
/// muhafızların önünde veriliyor, kılıç ise hücrenin yanındaki masada.
/// İskelet otuz birimden fazla yolu, arada kapı ve dar koridor varken,
/// kendisi planlıyor. Rota hiçbir yerde yazılı değil.
/// </summary>
public class MoveToSwordState : ISkeletonState
{
    private const int MaxRepath = 3;

    public string Name => "MoveToSword";

    private int repathTries;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;

        if (context.Agent != null) context.Agent.updateRotation = true;
        if (context.SwordObject == null) return;

        context.SetDestination(context.SwordObject.transform.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.SwordObject == null) return context.Blocked;
        if (context.PathFailed()) return context.Blocked;

        if (!context.HasArrived())
        {
            if (context.IsStuck())
            {
                repathTries++;

                if (context.LogTransitions)
                {
                    string olcum = context.Sensor != null ? context.Sensor.Ozet() : "sensör yok";
                    Debug.LogWarning("İskelet kılıca giderken takıldı (" + repathTries + "/"
                                     + MaxRepath + "). Sensör: " + olcum);
                }

                if (repathTries > MaxRepath) return context.Blocked;
                if (!context.Repath(context.SwordObject.transform.position)) return context.Blocked;
            }

            return this;
        }

        if (!context.IsNear(context.SwordObject.transform.position)) return context.Blocked;

        return context.PickUpSword;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
