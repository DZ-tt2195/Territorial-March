using System.Collections.Generic;
using UnityEngine;
using MyBox;
using TMPro;
using UnityEngine.UI;
using System;

public class CardGallery : MonoBehaviour
{

#region Setup

    [SerializeField] TMP_Text searchResults;
    [SerializeField] GridLayoutGroup storeCards;
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] TMP_Dropdown coinDropdown;
    [SerializeField] TMP_Dropdown typeDropdown;
    [SerializeField] Scrollbar cardScroll;
    List<Card> allCards = new();

    private void Start()
    {
        searchInput.onValueChanged.AddListener(ChangeSearch);
        coinDropdown.onValueChanged.AddListener(ChangeDropdown);
        typeDropdown.onValueChanged.AddListener(ChangeDropdown);

        for (int i = 0; i < CarryVariables.inst.playerCardFiles.Count; i++)
        {
            GameObject nextObject = Instantiate(CarryVariables.inst.playerCardPrefab.gameObject);
            PlayerCard card = nextObject.AddComponent<PlayerCard>();
            card.AssignInfo(i);
            allCards.Add(card);
        }
        for (int i = 0; i < CarryVariables.inst.areaCardFiles.Count; i++)
        {
            GameObject nextObject = Instantiate(CarryVariables.inst.areaCardPrefab.gameObject);
            AreaCard card = nextObject.AddComponent<AreaCard>();
            card.AssignInfo(i);
            allCards.Add(card);
        }

        SearchCards();
    }

    #endregion

#region Card Search

    bool CompareStrings(string searchBox, string comparison)
    {
        if (searchBox.IsNullOrEmpty())
            return true;
        return (comparison.IndexOf(searchBox, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    void ChangeSearch(string text)
    {
        SearchCards();
    }

    void ChangeDropdown(int n)
    {
        SearchCards();
    }

    void SearchCards()
    {
        int searchCoin;
        if (typeDropdown.options[typeDropdown.value].text == "Area")
        {
            searchCoin = -1;
            coinDropdown.gameObject.SetActive(false);
        }
        else
        {
            coinDropdown.gameObject.SetActive(true);
            try { searchCoin = int.Parse(coinDropdown.options[coinDropdown.value].text); }
            catch { searchCoin = -1; }
        }

        foreach (Card card in allCards)
        {
            bool stringMatch = (CompareStrings(searchInput.text, card.GetFile().textBox) || CompareStrings(searchInput.text, card.name));
            bool crownMatch = false;
            bool typeMatch = false;

            if (typeDropdown.options[typeDropdown.value].text == "Area")
            {
                crownMatch = true;
                typeMatch = card is AreaCard;
            }
            else if (typeDropdown.options[typeDropdown.value].text == "Card")
            {
                if ((card is PlayerCard))
                {
                    PlayerCardData data = (PlayerCardData)card.GetFile();
                    crownMatch = (searchCoin == -1) || data.startingCoin == searchCoin;
                    typeMatch = true;
                }
            }

            if (stringMatch && crownMatch && typeMatch)
            {
                card.transform.SetParent(storeCards.transform);
                card.transform.SetAsLastSibling();
            }
            else
            {
                card.transform.SetParent(null);
            }
        }

        storeCards.transform.localPosition = new Vector3(0, -1050, 0);
        if (typeDropdown.options[typeDropdown.value].text == "Area")
        {
            storeCards.GetComponent<RectTransform>().sizeDelta = new Vector3
                (2560, Math.Max(750, 275 * (Mathf.Ceil(storeCards.transform.childCount / 5f))));
            storeCards.cellSize = CarryVariables.inst.areaCardPrefab.GetComponent<RectTransform>().sizeDelta;
            storeCards.constraintCount = 5;
        }
        else
        {
            storeCards.GetComponent<RectTransform>().sizeDelta = new Vector3
                (2560, Math.Max(800, 400 * (Mathf.Ceil(storeCards.transform.childCount / 8f))));
            storeCards.cellSize = CarryVariables.inst.playerCardPrefab.GetComponent<RectTransform>().sizeDelta;
            storeCards.constraintCount = 8;
        }
        searchResults.text = $"Found {storeCards.transform.childCount} Cards";
    }

    #endregion

}
