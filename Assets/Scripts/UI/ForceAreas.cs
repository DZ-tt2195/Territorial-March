using UnityEngine;
using UnityEngine.UI;
using MyBox;
using System.Linq;
using System.Collections.Generic;
using Photon.Pun;

public class ForceAreas : MonoBehaviour
{
    public static ForceAreas instance;
    int step = 0;

    List<CardSelect> cardList = new();
    [SerializeField] Button confirmButton;
    CardSelect mostRecentClick;
    List<Button> blankImages = new();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Advance();
        confirmButton.onClick.AddListener(Advance);
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            this.gameObject.SetActive(false);
    }

    internal void ChooseFromImages(CardSelect clicked)
    {
        mostRecentClick = clicked;
    }

    void SendName(CardData data)
    {
        mostRecentClick.SetCardImage(data);
        mostRecentClick = null;
        foreach (Button button in blankImages)
            button.gameObject.SetActive(false);
    }

    void Advance()
    {
        cardList.Clear();

        if (step == 0)
        {
            blankImages.Clear();
            Transform blankCards = transform.GetChild(0).Find("Blanks");

            for (int i = 0; i < blankCards.childCount; i++)
            {
                Button nextButton = blankCards.GetChild(i).gameObject.GetComponent<Button>();
                try
                {
                    CardData data = CarryVariables.inst.areaCardFiles[i + 2];
                    blankImages.Add(nextButton);

                    nextButton.GetComponent<CardLayout>().FillInCards(data, 1);
                    nextButton.onClick.RemoveAllListeners();
                    nextButton.onClick.AddListener(() => SendName(data));
                }
                catch
                {

                }
                nextButton.gameObject.SetActive(false);
            }
        }
        else
        {
            this.gameObject.SetActive(false);
        }
        step++;
    }
}
