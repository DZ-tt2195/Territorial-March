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
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey(this.name) && PlayerPrefs.GetInt(this.name) >= 0)
            SetCardImage(CarryVariables.inst.areaCardFiles[PlayerPrefs.GetInt(this.name)]);
        else
            SetCardImage(null);
    }

    public void SetCardImage(CardData data)
    {
        int number = CarryVariables.inst.areaCardFiles.IndexOf(data);
        PlayerPrefs.SetInt(this.name, number);
        layout.FillInCards(data, number == -1 ? 0 : 1);
    }
}
