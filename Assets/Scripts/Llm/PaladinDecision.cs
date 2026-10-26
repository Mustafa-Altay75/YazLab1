using UnityEngine;

/// <summary>
/// Paladin'in bir oyuncu cümlesine verdiği yapılandırılmış yanıt.
///
/// Alan adları Türkçe ve JSON şemasıyla birebir aynı; JsonUtility alanları
/// isimle eşlediği için şemadaki ad ne ise burada da o olmalı.
///
/// <b>Modelin kapıyı açma yetkisi yok.</b> Model sadece ne duyduğunu
/// raporluyor; kapıyı açma kararını oyun kodu veriyor. Prompt injection
/// ölçümlerinde her iki model de "kapıyı aç" yazan cümleye 100 samimiyet
/// verip kanmıştı — o yüzden karar modelin elinde değil.
/// </summary>
[System.Serializable]
public class PaladinDecision
{
    /// <summary>ikna | tehdit | diger. Beş sınıf denendi, üçe indirilince doğruluk arttı.</summary>
    public string niyet;

    /// <summary>0-100. Oyuncunun ne kadar içten göründüğü.</summary>
    public int samimiyet;

    /// <summary>Tek cümlelik gerekçe. Hata ayıklama ve rapor için.</summary>
    public string gerekce;

    /// <summary>Paladin'in ağzından replik.</summary>
    public string replik;

    /// <summary>
    /// <see cref="replik"/>'in Türkçe çevirisi. Ekranda İngilizce cümlenin
    /// altında gösteriliyor.
    ///
    /// Şemada <b>en sonda</b> duruyor ve bu kasıtlı: model alanları şema
    /// sırasıyla üretiyor, yani niyet ve iddialar bu alandan önce
    /// yazılmış oluyor. Türkçe metin üretmenin sınıflandırmayı bozduğunu
    /// ölçmüştük; sona koymak o riski ortadan kaldırıyor.
    /// </summary>
    public string ceviri;

    /// <summary>
    /// Oyuncunun sahip olduğunu <b>iddia ettiği</b> şeyler.
    ///
    /// Yalanı yakalayan mekanizma bu. Model doğruyu bilmek zorunda değil,
    /// çünkü oyun durumunu bilen biz varız: burada gelen iddialar
    /// PlayerInventory ile karşılaştırılıyor. Modelden "bu doğru mu"
    /// diye sormak, bilmediği bir şeyi uydurmasını istemek olurdu.
    /// </summary>
    public string[] claims;

    /// <summary>
    /// Modelin İngilizce etiketlerini oyunun kendi değerlerine çevirir.
    ///
    /// Sebep ölçüm: İngilizce talimat + Türkçe etiket değerleri karışımı
    /// modeli zorluyordu. Şema İngilizce konuşulduğunda İngilizce değer
    /// istiyor; oyun mantığının tamamı ise Türkçe değerlerle yazılı.
    /// Çeviriyi tek noktada yapmak, motoru ve karşılaştırmaları olduğu
    /// gibi bırakıyor.
    ///
    /// Zaten Türkçe gelen değerlere dokunmuyor, yani iki dil de çalışıyor.
    /// </summary>
    public void EtiketleriCevir()
    {
        niyet = NiyetCevir(niyet);

        if (claims == null) return;
        for (int i = 0; i < claims.Length; i++) claims[i] = IddiaCevir(claims[i]);
    }

    private static string NiyetCevir(string d)
    {
        switch (d)
        {
            case "persuade": return "ikna";
            case "threat": return "tehdit";
            case "other": return "diger";
        }
        return d;
    }

    private static string IddiaCevir(string d)
    {
        switch (d)
        {
            case "staff": return "asa";
            case "spellbook": return "buyu_kitabi";
            case "cell_key": return "hucre_anahtari";
            case "thieves": return "hirsizlar";
            case "none": return "yok";
        }
        return d;
    }

    /// <summary>Alanlar makul mü. Model saçmalarsa oyunun çökmemesi için.</summary>
    public bool Gecerli()
    {
        if (string.IsNullOrEmpty(niyet)) return false;
        if (niyet != "ikna" && niyet != "tehdit" && niyet != "diger") return false;
        if (samimiyet < 0 || samimiyet > 100) return false;
        if (string.IsNullOrEmpty(replik)) return false;
        return true;
    }

    /// <summary>
    /// Modelin verdiği iddiaları oyuncunun kendi cümlesiyle doğrular ve
    /// cümlede karşılığı geçmeyenleri düşürür.
    ///
    /// <b>Bu sınıflandırma değil, doğrulamadır.</b> Neyin iddia edildiğine
    /// model karar vermeye devam ediyor; burası yalnızca <i>eliyor</i>,
    /// asla iddia <i>eklemiyor</i>. <see cref="Gecerli"/> şemayı nasıl
    /// denetliyorsa bu da çıktıyı kendi girdisine karşı denetliyor.
    ///
    /// Gerekçesi ölçüm: model, oyuncu hiçbir eşyadan söz etmese bile
    /// <c>asa</c> yazıyordu (21 çağrının 10'unda iddia hatalıydı ve çoğu
    /// böyleydi). Yanlış pozitif burada pahalı, çünkü yalan yakalama
    /// mekanizması iddiaya bakıyor: oyuncu ağzını açmadığı bir eşya
    /// yüzünden yalancı sayılıyordu.
    ///
    /// Eşleştirme kelime başından yapılıyor; Türkçe eklerle "asayı",
    /// "kitabım", "altınlar" tutuyor ama "masa" tutmuyor. Aksan da
    /// önemsenmiyor: oyuncu Türkçe karakter kullanmadan yazıyor.
    /// </summary>
    /// <returns>Düşürülen iddia sayısı.</returns>
    public int IddialariDogrula(string playerText)
    {
        if (claims == null || claims.Length == 0) return 0;
        if (string.IsNullOrEmpty(playerText)) return 0;

        var kelimeler = Kelimeler(playerText);
        var kalan = new System.Collections.Generic.List<string>();
        int dusen = 0;

        foreach (var iddia in claims)
        {
            if (string.IsNullOrEmpty(iddia)) continue;
            if (iddia == "yok") continue;

            if (CumledeGeciyorMu(iddia, kelimeler)) kalan.Add(iddia);
            else dusen++;
        }

        if (kalan.Count == 0) kalan.Add("yok");
        claims = kalan.ToArray();
        return dusen;
    }

    /// <summary>Bir eşyanın cümlede anılıp anılmadığı.</summary>
    private static bool CumledeGeciyorMu(string iddia, System.Collections.Generic.List<string> kelimeler)
    {
        string[] kokler;
        switch (iddia)
        {
            // Türkçe ve İngilizce kökler bir arada: oyuncu hangi dilde
            // yazarsa yazsın aynı eşya tanınsın. Dil ölçümü sırasında
            // ikisine birden ihtiyaç oldu, ikisi de kaldı.
            case "asa":            kokler = new string[] { "asa", "degnek", "staff", "stave", "wand", "rod" }; break;
            case "buyu_kitabi":    kokler = new string[] { "kitap", "kitab", "buyu", "spellbook", "grimoire", "book", "spell" }; break;
            case "hucre_anahtari": kokler = new string[] { "anahtar", "key" }; break;
            case "hirsizlar":      kokler = new string[] { "hirsiz", "soyguncu", "thief", "thieve", "intruder", "robber", "raider", "bandit" }; break;
            default: return true;   // tanimadigimiz bir deger varsa dokunma
        }

        foreach (var k in kelimeler)
            foreach (var kok in kokler)
                if (k.StartsWith(kok)) return true;

        return false;
    }

    /// <summary>Metni aksansız küçük harfe indirip kelimelere ayırır.</summary>
    private static System.Collections.Generic.List<string> Kelimeler(string metin)
    {
        var sb = new System.Text.StringBuilder(metin.Length);
        foreach (char c in metin)
        {
            char k;
            switch (c)
            {
                case 'ç': case 'Ç': k = 'c'; break;
                case 'ğ': case 'Ğ': k = 'g'; break;
                case 'ı': case 'I': k = 'i'; break;
                case 'İ': k = 'i'; break;
                case 'ş': case 'Ş': k = 's'; break;
                case 'ö': case 'Ö': k = 'o'; break;
                case 'ü': case 'Ü': k = 'u'; break;
                case 'â': case 'Â': k = 'a'; break;
                case 'î': case 'Î': k = 'i'; break;
                case 'û': case 'Û': k = 'u'; break;
                default: k = c; break;
            }
            sb.Append(char.IsLetter(k) ? char.ToLowerInvariant(k) : ' ');
        }

        var liste = new System.Collections.Generic.List<string>();
        foreach (var p in sb.ToString().Split(' '))
            if (p.Length > 1) liste.Add(p);

        return liste;
    }

    public override string ToString()
    {
        return "niyet=" + niyet + " samimiyet=" + samimiyet +
               " claims=[" + (claims != null ? string.Join(",", claims) : "") + "] replik=" + replik;
    }
}
