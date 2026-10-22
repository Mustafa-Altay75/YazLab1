using UnityEngine;

/// <summary>
/// Başlangıç durumu. İskelet diriltilene kadar hiçbir şey yapmaz.
/// Diriltildikten sonra da hemen yürümez: önce yerden kalkma animasyonunun
/// bitmesini bekler. Bunun için sabit bir süre saymak yerine animatörün
/// Skeletons_Idle durumuna düşmesi beklenir, çünkü animasyon süresi
/// değişirse sabit süre tutmaz.
/// </summary>
public class InactiveState : ISkeletonState
{
    public string Name => "Inactive";

    public void Enter(SkeletonContext context)
    {
        // Diriltilmeden önce ajan hiç hareket etmesin.
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.Interactable == null || !context.Interactable.IsResurrected)
        {
            return this;
        }

        // Kalkma animasyonu bitip boşta durma animasyonuna geçmeden yürümeye başlama.
        if (context.Animator != null)
        {
            AnimatorStateInfo info = context.Animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(SkeletonCompanion.IdleStateName))
            {
                return this;
            }
        }

        return context.MoveToKey;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
