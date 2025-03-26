using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MyBox;
using System.Linq;

public class Popup : MonoBehaviour
{
    [SerializeField] TMP_Text textbox;
    RectTransform textWidth;
    RectTransform imageWidth;

    [SerializeField] Button textButton;
    [SerializeField] Button cardButton;
    public List<Button> buttonsInCollector { get; private set; }
    Player decidingPlayer;

    void Awake()
    {
        textWidth = textbox.GetComponent<RectTransform>();
        imageWidth = this.transform.GetComponent<RectTransform>();
        buttonsInCollector = new();
    }

    internal void StatsSetup(Player player, string header, Vector2 position)
    {
        decidingPlayer = player;
        this.textbox.text = (header);
        this.transform.SetParent(Manager.inst.canvas.transform);
        this.transform.localPosition = position;
        this.transform.localScale = new Vector3(1, 1, 1);
    }

    internal void AddTextButton(string text)
    {
        Button nextButton = Instantiate(textButton, this.transform.GetChild(1));
        nextButton.transform.GetChild(0).GetComponent<TMP_Text>().text = (text);

        nextButton.interactable = true;
        int buttonNumber = buttonsInCollector.Count;
        nextButton.onClick.AddListener(() => decidingPlayer.DecisionMade(buttonNumber));
        buttonsInCollector.Add(nextButton);

        for (int i = 0; i < buttonsInCollector.Count; i++)
        {
            Transform nextTransform = buttonsInCollector[i].transform;
            nextTransform.transform.localPosition = new Vector2((buttonsInCollector.Count - 1) * -150 + (300 * i), 0);
        }
        Resize();
    }

    internal void AddCardButton(Card card, float alpha)
    {
        Button nextButton = Instantiate(cardButton, this.transform.GetChild(1));
        CardLayout layout = nextButton.GetComponent<CardLayout>();
        layout.FillInCards(card);
        layout.cg.alpha = alpha;

        nextButton.interactable = true;
        int buttonNumber = buttonsInCollector.Count+100;
        nextButton.onClick.AddListener(() => decidingPlayer.DecisionMade(buttonNumber));
        buttonsInCollector.Add(nextButton);

        for (int i = 0; i < buttonsInCollector.Count; i++)
        {
            Transform nextTransform = buttonsInCollector[i].transform;
            nextTransform.transform.localPosition = new Vector2((buttonsInCollector.Count - 1) * -150 + (300 * i), 0);
        }
        Resize();
    }

    void Resize()
    {
        imageWidth.sizeDelta = new Vector2(Mathf.Max(buttonsInCollector.Count, 2) * 350, imageWidth.sizeDelta.y);
        textWidth.sizeDelta = new Vector2(Mathf.Max(buttonsInCollector.Count, 2) * 350, textWidth.sizeDelta.y);
    }

    internal void DisableButtons()
    {
        foreach (Button button in buttonsInCollector)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }
    }

    internal void RemoveButtons()
    {
        foreach (Button button in buttonsInCollector)
        {
            Destroy(button.gameObject);
        }
        buttonsInCollector.Clear();
        Resize();
    }
}
