using UnityEngine;

/// <summary>
/// Yol bulunamadığında veya hedef kaybolduğunda girilen durum.
/// Oyunu kilitlememek için bir süre bekleyip <b>yarıda kalan görevin
/// kendisine</b> döner.
/// </summary>
public class BlockedState : ISkeletonState
{
    public string Name => "Blocked";

    private float enteredAt;

    public void Enter(SkeletonContext context)
    {
        enteredAt = Time.time;
        context.Stop();

        if (context.LogTransitions)
        {
            Debug.LogWarning("İskelet hedefe ulaşamadı, yeniden denenecek.");
        }
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (Time.time - enteredAt < context.RetryDelay)
        {
            return this;
        }

        // Hangi görevden düşüldüyse oraya dön. Bayrağa bakmak yetmiyordu:
        // HasKey yalnızca anahtar görevini biliyor, oysa plaka, kılıç ve
        // muhafız görevleri de buraya düşebiliyor.
        if (context.KesilenGorev != null) return context.KesilenGorev;

        // Bilinmiyorsa eski davranış: anahtar yoksa anahtara, varsa kapıya.
        return context.HasKey ? context.MoveToDoor : context.MoveToKey;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
