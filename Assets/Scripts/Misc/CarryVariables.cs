using System.Collections.Generic;
using UnityEngine;
using MyBox;
using TMPro;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using UnityEngine.UI;

public class CarryVariables : MonoBehaviour
{

#region Setup

    public static CarryVariables inst;

    [Foldout("Prefabs", true)]
    public Player playerPrefab;
    public CardLayout cardPrefab;
    public Popup textPopup;
    public Popup cardPopup;
    public SliderChoice sliderPopup;
    public TroopDisplay troopDisplayPrefab;
    public Button playerButtonPrefab;

    [Foldout("Right click", true)]
    [SerializeField] Transform rightClickBackground;
    [SerializeField] CardLayout rightClickCard;
    [SerializeField] TMP_Text rightClickText;
    [SerializeField] TMP_Text artistText;

    [Foldout("Misc", true)]
    [SerializeField] Transform permanentCanvas;
    public Sprite faceDownSprite;
    [ReadOnly] public List<string> cardScripts = new();

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
            Application.targetFrameRate = 60;
            GetScripts();
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    #endregion

#region Right click

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            rightClickBackground.gameObject.SetActive(false);
    }

    public void RightClickDisplay(Card card, float alpha)
    {
        rightClickBackground.gameObject.SetActive(true);
        rightClickCard.FillInCards(card);

        if (alpha == 0)
        {
            rightClickCard.cg.alpha = 0;
            artistText.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            rightClickCard.cg.alpha = 1;
            artistText.transform.parent.gameObject.SetActive(!card.artistText.IsNullOrEmpty());
            artistText.text = card.artistText;
        }
    }

    #endregion

#region Find Cards

    void GetScripts()
    {
        if (Application.isEditor)
        {
            string filePath = $"Assets/Resources/AvailableScripts.txt";
            List<string[]> allStrings = new() { ScriptsInRange("Cards") };
            File.WriteAllText(filePath, Format(allStrings));
        }

        string[] ScriptsInRange(string range)
        {
            string[] list = Directory.GetFiles($"Assets/Scripts/{range}", "*.cs", SearchOption.TopDirectoryOnly);
            string[] answer = new string[list.Length];
            for (int i = 0; i < list.Length; i++)
                answer[i] = Path.GetFileNameWithoutExtension(list[i]);

            return answer;
        }

        string Format(List<string[]> allStrings)
        {
            string content = "{\n";
            for (int i = 0; i < allStrings.Count; i++)
            {
                content += "  [\n";
                for (int j = 0; j < allStrings[i].Length; j++)
                {
                    content += $"    \"{allStrings[i][j]}\"";
                    if (j < allStrings[i].Length - 1)
                        content += ",";
                    content += "\n";
                }
                content += "  ]";
                if (i < allStrings.Count - 1)
                    content += ",";
                content += "\n";
            }
            content += "}\n";
            return content;
        }

        var data = ReadFile("AvailableScripts");
        for (int i = 0; i < data[1].Length; i++)
            data[1][i].Trim().Replace("\"", "");

        string[] nextArray = new string[data[1].Length];

        for (int j = 0; j < data[1].Length; j++)
        {
            string nextObject = data[1][j].Replace("\"", "").Replace("\\", "").Replace("]", "").Trim();
            nextArray[j] = nextObject;
        }

        cardScripts = nextArray.ToList();
    }

    string[][] ReadFile(string range)
    {
        TextAsset data = Resources.Load($"{range}") as TextAsset;
        string editData = data.text;
        editData = editData.Replace("],", "").Replace("{", "").Replace("}", "");

        string[] numLines = editData.Split("[");
        string[][] list = new string[numLines.Length][];

        for (int i = 0; i < numLines.Length; i++)
            list[i] = numLines[i].Split("\",");
        return list;
    }

    public Card AddCardComponent(GameObject obj, string cardName)
    {
        Type type = Type.GetType(cardName);
        obj.AddComponent(type);
        obj.name = Regex.Replace(cardName, "(?<=[a-z])(?=[A-Z])", " ");

        Card card = obj.GetComponent<Card>();
        card.layout.FillInCards(card);
        return card;
    }

    #endregion

}
