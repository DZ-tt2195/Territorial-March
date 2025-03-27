using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CardLayout : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup cg { get; private set; }
    Image artBox;

    TMP_Text description;
    TMP_Text cardName;
    TMP_Text coinBonus;
    CardData data;

    private void Awake()
    {
        cg = transform.Find("Canvas Group").GetComponent<CanvasGroup>();
        cardName = cg.transform.Find("Card Name").GetComponent<TMP_Text>();
        description = cg.transform.Find("Card Description").GetComponent<TMP_Text>();
        coinBonus = cg.transform.Find("Coin Bonus").GetComponent<TMP_Text>();
        artBox = cg.transform.Find("Art Box").GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            CarryVariables.inst.RightClickDisplay(this.data, cg.alpha);
    }

    public void FillInCards(CardData data)
    {
        this.data = data;
        try {artBox.sprite = Resources.Load<Sprite>($"Card Art/{data.cardName}");} catch { Debug.LogError($"no art for {data.cardName}"); }
        description.text = KeywordTooltip.instance.EditText(data.textBox);
        cardName.text = data.cardName;

        if (data is PlayerCardData converted)
        {
            coinBonus.gameObject.SetActive(true);
            coinBonus.text = $"{converted.coinBonus}";
        }
        else
        {
            coinBonus.gameObject.SetActive(false);
        }
    }
}
