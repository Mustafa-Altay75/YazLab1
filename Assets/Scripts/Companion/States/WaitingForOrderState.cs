/// <summary>
/// İskelet diriltildi, ayağa kalktı ve emir bekliyor.
///
/// Diriliş tek başına harekete geçirmiyor: oyuncunun ayrıca "anahtarı getir"
/// demesi gerekiyor. Şartname otonom kahramana açıkça emir verilmesini
/// istediği için bu adım araya kondu.
/// </summary>
public class WaitingForOrderState : ISkeletonState
{
    public string Name => "WaitingForOrder";

    public void Enter(SkeletonContext context)
    {
        // Emir gelene kadar yerinde dursun.
        context.Stop();
    }

    public ISkeletonState Tick(SkeletonContext context)
    {
        return context.OrderGiven ? context.MoveToKey : this;
    }

    public void Exit(SkeletonContext context)
    {
    }
}
