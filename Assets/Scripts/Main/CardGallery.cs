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
    [SerializeField] RectTransform storeCards;
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] TMP_Dropdown costDropdown;
    [SerializeField] TMP_Dropdown typeDropdown;
    [SerializeField] Scrollbar cardScroll;
    List<Card> allCards = new();

    private void Start()
    {
        searchInput.onValueChanged.AddListener(ChangeSearch);
        costDropdown.onValueChanged.AddListener(ChangeDropdown);
        typeDropdown.onValueChanged.AddListener(ChangeDropdown);

        foreach (string cardName in CarryVariables.inst.cardScripts)
        {
            /*
            GameObject nextObject = Instantiate(CarryVariables.inst.cardPrefab.gameObject);
            allCards.Add(CarryVariables.inst.AddCardComponent(nextObject, cardName));
            */
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
        int searchCost;
        try { searchCost = int.Parse(costDropdown.options[costDropdown.value].text); }
        catch { searchCost = -1; }

        foreach (Card card in allCards)
        {
            /*
            bool stringMatch = (CompareStrings(searchInput.text, card.extraText) || CompareStrings(searchInput.text, card.name));
            bool costMatch = ((searchCost == -1) || card.coinCost == searchCost);
            bool typeMatch = typeDropdown.options[typeDropdown.value].text switch
            {
                "Any" => true,
                _ => false
            };
            if (stringMatch && costMatch && typeMatch)
            {
                card.transform.SetParent(storeCards);
                card.transform.SetAsLastSibling();
            }
            else
            {
                card.transform.SetParent(null);
            }
            */
        }

        storeCards.transform.localPosition = new Vector3(0, -1050, 0);
        storeCards.sizeDelta = new Vector3(2560, Math.Max(800, 400 * (Mathf.Ceil(storeCards.childCount / 8f))));
        searchResults.text = $"Found {storeCards.childCount} Cards";
    }

    #endregion

}
