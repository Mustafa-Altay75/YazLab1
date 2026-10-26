using System.Collections.Generic;

/// <summary>
/// Hangi yazının hangi temaya ait olduğunu tutan küçük kayıt defteri.
///
/// Levhayı Mustafa'nın <c>PlayerInteraction</c>'ı sürüyor ve o an hangi
/// nesneyle etkileşimde olduğumuzu dışarı vermiyor. Onun dosyasına
/// dokunmamak için iki kötü yol vardı: aynı taramayı ikinci kez yapmak
/// (iki sonuç ayrışabilir), ya da temayı öğrenmek için her kare
/// <c>GetInteractPrompt</c> çağırmak — ki o metotların yan etkisi var,
/// <c>GuardEncounter</c> "görüldü", <c>PlateGate</c> "kapı görüldü"
/// işaretliyor; uzaktan çağırmak oyunu bozardı.
///
/// Bunun yerine: <c>PlayerInteraction</c> zaten menzildeki her nesnenin
/// yazısını her kare alıyor. Bizim etkileşimlerimiz o sırada ne
/// döndürdüklerini buraya bildiriyor, levha da ekranda yazan metni
/// burada arayıp temasını buluyor. Fazladan çağrı yok, yan etki yok.
///
/// Eşleşmeyen yazılar <see cref="PromptThemeKind.World"/> sayılıyor —
/// Mustafa'nın not, sandık ve kapı yazıları için doğru varsayılan.
/// </summary>
public static class PromptThemeRegistry
{
    private static readonly Dictionary<string, PromptThemeKind> kayit =
        new Dictionary<string, PromptThemeKind>();

    /// <summary>Bir etkileşim, döndürdüğü yazıyı ve temasını bildirir.</summary>
    public static void Report(string prompt, PromptThemeKind kind)
    {
        if (string.IsNullOrEmpty(prompt)) return;
        kayit[prompt] = kind;
    }

    /// <summary>Ekranda yazan metnin teması. Bilinmiyorsa World.</summary>
    public static PromptThemeKind Resolve(string prompt)
    {
        if (string.IsNullOrEmpty(prompt)) return PromptThemeKind.World;

        PromptThemeKind kind;
        return kayit.TryGetValue(prompt, out kind) ? kind : PromptThemeKind.World;
    }

    /// <summary>
    /// Sahne yeniden yüklenince defter boşalsın; yoksa eski oyunun
    /// yazıları bellekte kalıyor.
    /// </summary>
    public static void Clear()
    {
        kayit.Clear();
    }
}
