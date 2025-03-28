using UnityEngine;
using UnityEngine.UI;
using MyBox;
using System.Collections.Generic;

public class CardSelect : MonoBehaviour
{
    RectTransform rectTrans;
    CardLayout layout;
    Button randomButton;
    Button chooseButton;

    private void Awake()
    {
        rectTrans = GetComponent<RectTransform>();
        layout = GetComponent<CardLayout>();

        randomButton = transform.Find("Random").GetComponent<Button>();
        randomButton.onClick.AddListener(() => SetCardImage(null));

        chooseButton = transform.Find("Choose").GetComponent<Button>();
        chooseButton.onClick.AddListener(() => ForceAreas.instance.ChooseFromImages(this));

        if (PlayerPrefs.HasKey(this.name))
            SetCardImage(CarryVariables.inst.areaCardFiles[PlayerPrefs.GetInt(this.name)]);
        else
            SetCardImage(null);
    }

    public void SetCardImage(CardData data)
    {
        PlayerPrefs.SetInt(this.name, CarryVariables.inst.areaCardFiles.IndexOf(data));
        layout.FillInCards(data, data == null ? 0 : 1);
    }
}
