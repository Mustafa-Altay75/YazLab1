/// <summary>
/// Yazı yazarken oyun tuşlarının tetiklenmemesi için tek bayrak.
///
/// Diyalog paneli açıkken klavye metin kutusuna ait oluyor. Oyuncunun
/// bileşenlerini (hareket, etkileşim) kapatmak yeterli değil: iskeletin
/// takip tuşunu okuyan SkeletonCompanion ve saldırıyı okuyan PlayerAttack
/// klavyeyi doğrudan dinliyor. Panelde "faydasız" yazarken iskeletin
/// takibe geçmesi ya da boşluk tuşunun büyü atması bu yüzden mümkündü.
///
/// Mustafa'nın PlayerController ve PlayerInteraction dosyalarına
/// dokunmamak için onlar bileşen olarak kapatılıyor; kendi
/// scriptlerimiz ise bu bayrağa bakıyor.
/// </summary>
public static class InputLock
{
    /// <summary>Klavye bir arayüz öğesine aitse true.</summary>
    public static bool Captured { get; private set; }

    public static void Capture() { Captured = true; }

    public static void Release() { Captured = false; }
}
