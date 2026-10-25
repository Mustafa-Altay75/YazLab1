using UnityEngine;

/// <summary>
/// Kılıcı alan iskelet muhafızlara döner.
///
/// Hedef sabit bir nokta değil, o an ayakta olan en yakın muhafız. Biri
/// düşerse iskelet kendiliğinden diğerine yönelir; ayrıca hedef seçme
/// mantığı burada değil GuardEncounter'da duruyor, çünkü kimin ayakta
/// olduğunu bilen yer orası.
///
/// Varış ölçütü kabul yarıçapı değil vuruş menzili: muhafızlar NavMesh'i
/// oyduğu için iskelet onların tam üstüne yürüyemez, kenarında durur.
/// Çarpışmayı da buradaki varış başlatıyor — o ana kadar iki taraf da
/// birbirine dokunamıyor.
/// </summary>
public class MoveToThreatState : ISkeletonState
{
    private const int MaxRepath = 3;

    public string Name => "MoveToThreat";

    private int repathTries;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;
        if (context.Agent != null) context.Agent.updateRotation = true;

        Health hedef = context.NearestThreat();
        if (hedef != null) context.SetDestination(hedef.transform.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        Health hedef = context.NearestThreat();
        if (hedef == null) return context.FightOver();

        if (context.WithinAttackRange(hedef))
        {
            context.Encounter.BeginFight();
            return context.AttackThreat;
        }

        if (context.PathFailed()) return context.Blocked;

        if (!context.HasArrived())
        {
            if (context.IsStuck())
            {
                repathTries++;

                if (context.LogTransitions)
                {
                    string olcum = context.Sensor != null ? context.Sensor.Ozet() : "sensör yok";
                    Debug.LogWarning("İskelet muhafızlara giderken takıldı (" + repathTries + "/"
                                     + MaxRepath + "). Sensör: " + olcum);
                }

                if (repathTries > MaxRepath) return context.Blocked;
                if (!context.Repath(hedef.transform.position)) return context.Blocked;
            }

            return this;
        }

        // Yolun sonuna geldi ama menzile girmediyse hedef değişmiş olabilir;
        // bir kez daha yol iste, sonra pes et.
        repathTries++;
        if (repathTries > MaxRepath) return context.Blocked;
        context.Repath(hedef.transform.position);
        return this;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
