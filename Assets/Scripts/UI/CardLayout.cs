using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CardLayout : MonoBehaviour, IPointerClickHandler
{
    CanvasGroup cg;
    Image artBox;

    TMP_Text description;
    TMP_Text cardName;
    TMP_Text startingCoin;
    CardData data;

    private void Awake()
    {
        cg = transform.Find("Canvas Group").GetComponent<CanvasGroup>();
        cardName = cg.transform.Find("Card Name").GetComponent<TMP_Text>();
        description = cg.transform.Find("Card Description").GetComponent<TMP_Text>();

        try { startingCoin = cg.transform.Find("Starting Coin").GetComponent<TMP_Text>(); } catch { }
        try { artBox = cg.transform.Find("Art Box").GetComponent<Image>(); } catch { }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && this.data != null)
            CarryVariables.inst.RightClickDisplay(this.data, cg.alpha, this.data is PlayerCardData);
    }

    public float GetAlpha()
    {
        return cg.alpha;
    }

    public void FillInCards(CardData data, float alpha)
    {
        this.data = data;
        cg.alpha = alpha;
        if (data == null)
            return;

        try {artBox.sprite = Resources.Load<Sprite>($"Card Art/{data.cardName}");} catch { }
        description.text = KeywordTooltip.instance.EditText(data.textBox);
        cardName.text = data.cardName;

        if (data is PlayerCardData converted)
            startingCoin.text = $"{converted.startingCoin}";
    }
}
