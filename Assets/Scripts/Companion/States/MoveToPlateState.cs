using UnityEngine;

/// <summary>
/// Basınç plakasına yürür.
///
/// MoveToKey ile aynı kalıp: rota tarif edilmez, sadece hedef verilir ve
/// yolu NavMeshAgent hesaplar. Aradaki fark, bu görevin başlangıç
/// noktasının sabit olmaması: emir oyuncunun o an nerede durduğuna göre
/// her seferinde başka bir yerden verilebiliyor. Projenin "farklı
/// başlangıç koşullarında tekrarlanabilir" isterini karşılayan yer burası.
/// </summary>
public class MoveToPlateState : ISkeletonState
{
    /// <summary>Üst üste bu kadar yeniden yoldan sonra pes edilir.</summary>
    private const int MaxRepath = 3;

    public string Name => "MoveToPlate";

    private int repathTries;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;

        if (context.Agent != null) context.Agent.updateRotation = true;
        if (context.PlatePoint == null) return;

        context.SetDestination(context.PlatePoint.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.PlatePoint == null) return context.Blocked;

        if (context.PathFailed()) return context.Blocked;

        if (!context.HasArrived())
        {
            if (context.IsStuck())
            {
                repathTries++;

                if (context.LogTransitions)
                {
                    string olcum = context.Sensor != null ? context.Sensor.Ozet() : "sensör yok";
                    Debug.LogWarning("İskelet plakaya giderken takıldı (" + repathTries + "/"
                                     + MaxRepath + "). Sensör: " + olcum);
                }

                if (repathTries > MaxRepath) return context.Blocked;
                if (!context.Repath(context.PlatePoint.position)) return context.Blocked;
            }

            return this;
        }

        // Yol kısmi çıkmış olabilir; yolun sonu gerçekten plakanın yanı mı.
        if (!context.IsNear(context.PlatePoint.position)) return context.Blocked;

        return context.StandOnPlate;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
