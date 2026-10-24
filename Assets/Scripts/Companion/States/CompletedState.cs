/// <summary>
/// Anahtar görevi bitti. İskelet kapının yanında bekler.
///
/// Artık son durak değil: ikinci görev eklendikten sonra buradan takip
/// moduna veya doğrudan plakaya geçilebiliyor. Oyuncu emir vermezse
/// iskelet olduğu yerde kalır.
/// </summary>
public class CompletedState : ISkeletonState
{
    public string Name => "Completed";

    public void Enter(SkeletonContext context)
    {
        context.Stop();

        // Anahtar görevi bitti; takip modu bundan sonra açılabilir.
        context.KeyErrandDone = true;

        // Açtığı kapıya dönük dursun; sırtı dönük beklemesin.
        context.FaceDoor();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        if (context.PlateOrderGiven) return context.MoveToPlate;
        if (context.FollowRequested) return context.Following;

        return this;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
