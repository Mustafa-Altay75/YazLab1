using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.1f, 1f); // Altýn sarýsý
    [SerializeField][Range(0f, 3f)] private float emissionIntensity = 1.2f;

    private Renderer[] renderers;
    private Color[][] originalColors;
    private bool isHighlighted = false;

    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProp = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        // Objenin ve alt parçalarýnýn tüm Renderer bileþenlerini al
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            originalColors[i] = new Color[mats.Length];

            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty(BaseColorProp))
                    originalColors[i][j] = mats[j].GetColor(BaseColorProp);
                else if (mats[j].HasProperty(ColorProp))
                    originalColors[i][j] = mats[j].GetColor(ColorProp);
            }
        }
    }

    public void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight || renderers == null) return;
        isHighlighted = highlight;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material[] mats = renderers[i].materials;

            for (int j = 0; j < mats.Length; j++)
            {
                if (highlight)
                {
                    // Rengi sarýya çek ve parlama (Emission) aç
                    if (mats[j].HasProperty(BaseColorProp))
                        mats[j].SetColor(BaseColorProp, glowColor);
                    if (mats[j].HasProperty(ColorProp))
                        mats[j].SetColor(ColorProp, glowColor);

                    mats[j].EnableKeyword("_EMISSION");
                    mats[j].SetColor(EmissionColorProp, glowColor * emissionIntensity);
                }
                else
                {
                    // Orijinal rengine döndür ve parlamayý kapat
                    if (mats[j].HasProperty(BaseColorProp))
                        mats[j].SetColor(BaseColorProp, originalColors[i][j]);
                    if (mats[j].HasProperty(ColorProp))
                        mats[j].SetColor(ColorProp, originalColors[i][j]);

                    mats[j].SetColor(EmissionColorProp, Color.black);
                    mats[j].DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private void OnDisable()
    {
        // Obje alýndýðýnda veya kapandýðýnda parlamayý sýfýrla
        SetHighlight(false);
    }
}