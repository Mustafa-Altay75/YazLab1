using UnityEngine;

/// <summary>
/// Anahtarı alır. Alma anının gözle görülebilmesi için kısa bir süre
/// bekler, sonra anahtarı sahneden kaldırır.
/// </summary>
public class PickUpKeyState : ISkeletonState
{
    public string Name => "PickUpKey";

    private float enteredAt;

    public void Enter(SkeletonContext context)
    {
        enteredAt = Time.time;
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (Time.time - enteredAt < context.PickUpDuration)
        {
            return this;
        }

        if (context.KeyObject != null)
        {
            context.KeyObject.SetActive(false);
        }

        context.HasKey = true;
        return context.MoveToDoor;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
