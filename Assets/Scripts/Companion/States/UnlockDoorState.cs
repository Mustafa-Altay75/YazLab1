using UnityEngine;

/// <summary>
/// Kapıyı açar. Oyuncu bu kapıyı E ile açamaz; DoorInteractable yalnızca
/// PlayerController taşımayan bir etkileşimciye izin verir. Kapının
/// açılmasının tek yolu buradan OpenDoor() çağrılmasıdır.
/// </summary>
public class UnlockDoorState : ISkeletonState
{
    public string Name => "UnlockDoor";

    public void Enter(SkeletonContext context)
    {
        context.Stop();

        if (context.Door != null)
        {
            context.Door.OpenDoor();
        }
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        return context.Completed;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
