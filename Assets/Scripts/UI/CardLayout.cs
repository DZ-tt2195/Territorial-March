using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class CardLayout : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup cg { get; private set; }
    Image background;
    Image artBox;

    TMP_Text description;
    TMP_Text cardName;
    Card myCard;

    private void Awake()
    {
        cg = transform.Find("Canvas Group").GetComponent<CanvasGroup>();
        background = cg.transform.Find("Background").GetComponent<Image>();
        cardName = cg.transform.Find("Card Name").GetComponent<TMP_Text>();
        description = cg.transform.Find("Card Description").GetComponent<TMP_Text>();
        artBox = cg.transform.Find("Art Box").GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            CarryVariables.inst.RightClickDisplay(this.myCard, cg.alpha);
        }
    }

    public void FillInCards(Card card)
    {
        myCard = card;
        try {artBox.sprite = Resources.Load<Sprite>($"Card Art/{card.name}");} catch { Debug.LogError($"no art for {card.name}"); }
        background.color = card.MyColor();
        description.text = KeywordTooltip.instance.EditText(card.extraText);
        cardName.text = card.name;
    }
}
