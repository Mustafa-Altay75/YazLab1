using UnityEngine;

/// <summary>
/// Masanın üstündeki anahtara yürür.
/// Rota kodda tarif edilmez; ajana sadece hedef nokta verilir, yolu
/// NavMesh üzerinde A* algoritması kendisi hesaplar.
/// </summary>
public class MoveToKeyState : ISkeletonState
{
    /// <summary>Üst üste bu kadar yeniden yoldan sonra pes edilir.</summary>
    private const int MaxRepath = 3;

    public string Name => "MoveToKey";

    private int repathTries;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;

        if (context.KeyObject == null)
        {
            // Hedef hiç atanmamışsa yürünecek yer yok.
            return;
        }

        context.SetDestination(context.KeyObject.transform.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.KeyObject == null)
        {
            return context.Blocked;
        }

        // Hiç yol kurulamadıysa boşuna bekleme.
        if (context.PathFailed())
        {
            return context.Blocked;
        }

        if (!context.HasArrived())
        {
            // Yolda ama kıpırdamıyorsa bir yere sıkışmış olabilir.
            // Sensörün ölçtüğü veri hem konsola yazılıyor hem de kararın
            // dayanağı oluyor: önünde gerçekten bir engel mi var.
            if (context.IsStuck())
            {
                repathTries++;

                if (context.LogTransitions)
                {
                    string olcum = context.Sensor != null ? context.Sensor.Ozet() : "sensör yok";
                    Debug.LogWarning("İskelet anahtara giderken takıldı (" + repathTries + "/"
                                     + MaxRepath + "). Sensör: " + olcum);
                }

                if (repathTries > MaxRepath) return context.Blocked;
                if (!context.Repath(context.KeyObject.transform.position)) return context.Blocked;
            }

            return this;
        }

        // Yolun sonuna gelindi. Yol kısmi çıkmışsa burası hedefin yanı
        // olmayabilir; gerçekten anahtarın yanında mıyız diye ayrıca bak.
        if (!context.IsNear(context.KeyObject.transform.position))
        {
            return context.Blocked;
        }

        return context.PickUpKey;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
