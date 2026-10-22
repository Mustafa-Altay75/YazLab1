using UnityEngine;

/// <summary>
/// Anahtarı aldıktan sonra hücre kapısına yürür.
/// </summary>
public class MoveToDoorState : ISkeletonState
{
    public string Name => "MoveToDoor";

    public void Enter(SkeletonContext context)
    {
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
