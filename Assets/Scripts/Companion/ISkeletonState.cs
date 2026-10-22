/// <summary>
/// İskeletin durum makinesindeki tek bir durumu temsil eder.
/// Her durum ayrı bir sınıftır; böylece karar mantığı tek bir dev switch
/// bloğuna değil, kendi sorumluluğu olan parçalara dağılır.
/// </summary>
public interface ISkeletonState
{
    /// <summary>Konsolda ve Inspector'da görünen okunabilir ad.</summary>
    string Name { get; }

    /// <summary>Bu duruma girildiğinde bir kez çalışır.</summary>
    void Enter(SkeletonContext context);

    /// <summary>
    /// Her karede çalışır. Durum değişmeyecekse kendini, değişecekse
    /// bir sonraki durumu döndürür. Böylece geçiş kararı durumun kendisine ait olur.
    /// </summary>
    ISkeletonState Tick(SkeletonContext context);

    /// <summary>Bu durumdan çıkılırken bir kez çalışır.</summary>
    void Exit(SkeletonContext context);
}
