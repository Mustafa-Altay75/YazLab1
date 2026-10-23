using UnityEngine;

/// <summary>
/// Anahtarı aldıktan sonra hücre kapısına yürür.
/// </summary>
public class MoveToDoorState : ISkeletonState
{
    /// <summary>Üst üste bu kadar yeniden yoldan sonra pes edilir.</summary>
    private const int MaxRepath = 3;

    public string Name => "MoveToDoor";

    private int repathTries;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;

        if (context.Door == null)
        {
            return;
        }

        context.SetDestination(context.Door.transform.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.Door == null)
        {
            return context.Blocked;
        }

        if (context.PathFailed())
        {
            return context.Blocked;
        }

        if (!context.HasArrived())
        {
            if (context.IsStuck())
            {
                repathTries++;

                if (context.LogTransitions)
                {
                    string olcum = context.Sensor != null ? context.Sensor.Ozet() : "sensör yok";
                    Debug.LogWarning("İskelet kapıya giderken takıldı (" + repathTries + "/"
                                     + MaxRepath + "). Sensör: " + olcum);
                }

                if (repathTries > MaxRepath) return context.Blocked;
                if (!context.Repath(context.Door.transform.position)) return context.Blocked;
            }

            return this;
        }

        if (!context.IsNear(context.Door.transform.position))
        {
            return context.Blocked;
        }

        return context.UnlockDoor;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
