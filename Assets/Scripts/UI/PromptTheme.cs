using UnityEngine;

/// <summary>
/// Etkileşim yazısının hangi işe ait olduğu.
///
/// Oyunda dokuz etkileşim var ve hepsi aynı levhayı kullanıyordu; yaptığın
/// işin türü hiçbir yerden okunmuyordu. Renk ve etiket bunu söylüyor.
/// </summary>
public enum PromptThemeKind
{
    /// <summary>Not, sandık, kapı, anahtar: dünyadaki sıradan nesneler.</summary>
    World,

    /// <summary>İskeleti diriltmek ve ona emir vermek.</summary>
    Necromancy,

    /// <summary>Hırsızlar: düşman.</summary>
    Enemy,

    /// <summary>Paladin'le konuşma.</summary>
    Paladin
}

/// <summary>Bir temanın renkleri ve etiketi.</summary>
[System.Serializable]
public class PromptTheme
{
    public PromptThemeKind kind = PromptThemeKind.World;

    [Tooltip("Yazının üstünde görünen kategori. Boş bırakılırsa etiket gizlenir.")]
    public string label = "";

    [Tooltip("Levhanın dış kenarlığı.")]
    public Color border = new Color(0.61f, 0.53f, 0.31f, 1f);

    [Tooltip("Levhanın gövdesi.")]
    public Color body = new Color(0.23f, 0.23f, 0.21f, 0.96f);

    [Tooltip("Etiket ve tuş kutusu yazısının rengi.")]
    public Color accent = new Color(0.78f, 0.70f, 0.47f, 1f);
}
