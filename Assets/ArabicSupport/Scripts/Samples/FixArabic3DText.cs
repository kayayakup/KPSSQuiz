using UnityEngine;
using TMPro;
using ArabicSupport;

public class FixArabic3DText : MonoBehaviour {

    public bool showTashkeel = true;
    public bool useHinduNumbers = true;

    // Use this for initialization
    void Start () {
        TextMesh textMesh = gameObject.GetComponent<TextMesh>();
        TMP_Text tmpText = gameObject.GetComponent<TMP_Text>();

        if (textMesh == null && tmpText == null)
        {
            Debug.LogWarning($"[FixArabic3DText] No TextMesh or TMP_Text component found on {gameObject.name}.");
            return;
        }

        string originalText = textMesh != null ? textMesh.text : tmpText.text;
        string fixedText = ArabicFixer.Fix(originalText, showTashkeel, useHinduNumbers);

        if (textMesh != null)
            textMesh.text = fixedText;
        else
            tmpText.text = fixedText;
    }

}
