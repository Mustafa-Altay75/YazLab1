using UnityEngine;

/// <summary>
/// İskelet muhafıza vurur.
///
/// Oyuncuyla aynı hasarı veriyor; çarpışmanın kısa sürmesi isteniyor,
/// uzatmak değil. Hedef ölünce ya da menzilden çıkınca durum kendi
/// kararını veriyor: ayakta muhafız kaldıysa ona yürünür, kalmadıysa
/// iskeletin bu görevi biter ve oyuncunun yanına döner.
/// </summary>
public class AttackThreatState : ISkeletonState
{
    public string Name => "AttackThreat";

    private float sonrakiVurus;

    public void Enter(SkeletonContext context)
    {
        context.Stop();
        sonrakiVurus = Time.time + context.AttackCooldown * 0.5f;
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        Health hedef = context.NearestThreat();
        if (hedef == null) return context.FightOver();

        if (!context.WithinAttackRange(hedef)) return context.MoveToThreat;

        context.FaceTarget(hedef.transform.position);

        if (Time.time < sonrakiVurus) return this;
        sonrakiVurus = Time.time + context.AttackCooldown;

        context.TriggerAttackAnimation();
        hedef.TakeDamage(context.AttackDamage);

        return this;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
