using UnityEngine;

/// <summary>
/// İskelet oyuncuyu takip eder.
///
/// Bu durumun varlık sebebi tek başına bir kolaylık değil: anahtar görevi
/// hep aynı karodan başladığı için ajanın kendi yolunu bulduğunu
/// gösteremiyor. Takip modu sayesinde oyuncu iskeleti istediği yere
/// götürebiliyor ve emri oradan verebiliyor; yani başlangıç noktası her
/// seferinde farklı oluyor.
/// </summary>
public class FollowingState : ISkeletonState
{
    /// <summary>Hedef bu kadar kaymadan yeniden yol istenmez.</summary>
    private const float HedefEsigi = 1.0f;

    private float sonrakiYenileme;
    private Vector3 sonHedef;
    private bool hedefVar;

    public string Name => "Following";

    public void Enter(SkeletonContext context)
    {
        sonrakiYenileme = 0f;
        hedefVar = false;
        context.ResetStuck();

        // Takip ederken tekrar dönebilmeli; görev bitince FaceDoor bunu
        // kapatmış oluyor.
        if (context.Agent != null) context.Agent.updateRotation = true;
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.CoinOrderGiven) return context.MoveToCoins;
        if (context.SwordOrderGiven) return context.MoveToSword;
        if (context.PlateOrderGiven) return context.MoveToPlate;
        if (!context.FollowRequested) return context.Completed;

        if (context.Player == null) return this;

        // Oyuncu yeterince yakınsa dur; yoksa sürekli dürtükler.
        float uzaklik = Vector3.Distance(context.Agent.transform.position, context.Player.position);
        if (uzaklik <= context.FollowDistance)
        {
            if (context.Agent != null && context.Agent.isOnNavMesh && context.Agent.hasPath)
            {
                context.Agent.ResetPath();
            }
            return this;
        }

        // Hedefi her karede değil, aralıklı yenile.
        if (Time.time >= sonrakiYenileme)
        {
            sonrakiYenileme = Time.time + context.FollowRepathInterval;

            // Oyuncunun üstüne değil, biraz gerisine git; yoksa iterek
            // yürüyor.
            Vector3 geri = context.Player.position
                           - context.Player.forward * (context.FollowDistance * 0.8f);

            // Hedef kayda değer biçimde değişmediyse yeniden yol isteme.
            // Her çağrı yolu sıfırlayıp ajanı bir an duraklatıyor; sık
            // çağrılınca ajan hızının üçte birinde yürüyordu.
            if (!hedefVar || Vector3.Distance(geri, sonHedef) > HedefEsigi)
            {
                if (context.SetDestination(geri))
                {
                    sonHedef = geri;
                    hedefVar = true;
                }
            }
        }

        return this;
    }

    public void Exit(SkeletonContext context)
    {
        context.Stop();
    }
}
