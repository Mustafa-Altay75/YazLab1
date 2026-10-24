/// <summary>
/// İskelet plakanın üstünde bekler.
///
/// Kapıyı bu durum açmıyor; plakanın kendi tetiği açıyor. Ayrım kasıtlı:
/// plaka üstünde ağırlık olup olmadığını bilen şey plakadır, iskeletin
/// durum makinesi değil. Böylece ileride plakaya başka bir şey de
/// basabilir ve burada hiçbir değişiklik gerekmez.
///
/// Bu durumdan kendiliğinden çıkış yok; iskelet oyuncu geri çağırana
/// kadar bekler, kapı da o sürece açık kalır.
/// </summary>
public class StandOnPlateState : ISkeletonState
{
    public string Name => "StandOnPlate";

    public void Enter(SkeletonContext context)
    {
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        // Oyuncu takip moduna alırsa plakadan iner; kapı da kapanır.
        if (context.FollowRequested)
        {
            context.PlateOrderGiven = false;
            return context.Following;
        }

        return this;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
