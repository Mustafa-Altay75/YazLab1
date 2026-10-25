/// <summary>
/// İskelet altınlarla kapının önünde bekler.
///
/// Oyuncu [E] ile altınları alana kadar burada kalıyor. Aldıktan sonra
/// takip moduna DÖNMÜYOR, koridora çekiliyor: Paladin'in dibinde bekleyen
/// iskelet oyuncunun etkileşim menzilini kapıyor ve altınları Paladin'e
/// vermek zorlaşıyordu.
/// </summary>
public class WaitingWithCoinsState : ISkeletonState
{
    public string Name => "WaitingWithCoins";

    public void Enter(SkeletonContext context)
    {
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.CoinsReady) return this;

        // Altınlar alındı: oyuncunun önünden çekil.
        return context.Retreat;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
