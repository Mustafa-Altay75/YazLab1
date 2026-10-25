using UnityEngine;

/// <summary>
/// Merkez odadaki altınlara yürür.
///
/// İskeletin üçüncü ve son görevi. Önceki ikisi gibi rota tarif edilmiyor;
/// hedef veriliyor, yolu ajan buluyor. Buradaki fark başlangıç noktasının
/// oyunun en uzak köşesi olması: emir Paladin'in kapısının önünde
/// veriliyor, altınlar ise merkez odada. Aynı ajanın üçüncü kez, bambaşka
/// bir yerden yola çıkıp hedefe varması, raporun "farklı başlangıç
/// koşullarında tekrarlanabilir" bölümünün kanıtı.
/// </summary>
public class MoveToCoinsState : ISkeletonState
{
    private const int MaxRepath = 3;

    public string Name => "MoveToCoins";

    private int repathTries;
    private GameObject hedef;

    public void Enter(SkeletonContext context)
    {
        repathTries = 0;
        if (context.Agent != null) context.Agent.updateRotation = true;

        hedef = context.NextCoin();
        if (hedef != null) context.SetDestination(hedef.transform.position);
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        // Hedef alınmış ya da yok olmuşsa yenisini seç.
        if (hedef == null || !hedef.activeInHierarchy)
        {
            hedef = context.NextCoin();
            if (hedef == null) return context.ReturnToPaladin;

            repathTries = 0;
            if (!context.SetDestination(hedef.transform.position)) return context.Blocked;
            return this;
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
                    Debug.LogWarning("İskelet altınlara giderken takıldı (" + repathTries + "/"
                                     + MaxRepath + "). Sensör: " + olcum);
                }

                if (repathTries > MaxRepath) return context.Blocked;
                if (!context.Repath(hedef.transform.position)) return context.Blocked;
            }

            return this;
        }

        if (!context.IsNear(hedef.transform.position)) return context.Blocked;

        return context.CollectCoin;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
