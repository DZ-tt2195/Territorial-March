using System.Collections.Generic;
using UnityEngine;
using MyBox;
using TMPro;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Reflection;
using UnityEngine.Networking;

[Serializable]
public class CardData
{
    public string cardName;
    public string textBox;
    public string cardInstructions;
    public bool useSheets;
    public int cardAmount;
    public int coinAmount;
    public int actionAmount;
    public int scoutAmount;
    public int troopAmount;
    public int miscAmount;
    public string artCredit;
}

[Serializable]
public class PlayerCardData : CardData
{
    public int coinBonus;
}

public class CarryVariables : MonoBehaviour
{

#region Setup

    public static CarryVariables inst;

    [Foldout("Prefabs", true)]
    public Player playerPrefab;
    public CardLayout playerCardPrefab;
    public CardLayout areaCardPrefab;
    public Popup textPopup;
    public Popup cardPopup;
    public SliderChoice sliderPopup;
    public TroopDisplay troopDisplayPrefab;
    public Button playerButtonPrefab;

    [Foldout("Right click", true)]
    [SerializeField] Transform rightClickBackground;
    [SerializeField] CardLayout rightClickCard;
    [SerializeField] CardLayout rightClickLandscape;
    [SerializeField] TMP_Text rightClickText;
    [SerializeField] TMP_Text artistText;

    [Foldout("Card data", true)]
    public List<PlayerCardData> playerCardFiles { get; private set; }
    public List<CardData> areaCardFiles { get; private set; }
    string sheetURL = "1s-H-hVKvhJ0QTbhY4WxC1iS3xV4rTrs2iAJTl_Fy1yY";
    string apiKey = "AIzaSyCl_GqHd1-WROqf7i2YddE3zH6vSv3sNTA";
    string baseUrl = "https://sheets.googleapis.com/v4/spreadsheets/";

    [Foldout("Misc", true)]
    [SerializeField] Transform permanentCanvas;
    public bool playWithBot;

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
            Application.targetFrameRate = 60;
            StartCoroutine(GetScripts());
            DontDestroyOnLoad(this.gameObject);

            if (!Application.isEditor)
                playWithBot = true;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public string PrintIntList(List<int> listOfInts)
    {
        string answer = "";
        foreach (int next in listOfInts)
            answer += $"{next}, ";
        return answer;
    }

    #endregion

#region Right click

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            rightClickBackground.gameObject.SetActive(false);
    }

    public void RightClickDisplay(CardData data, float alpha, bool isCard)
    {
        rightClickBackground.gameObject.SetActive(true);

        rightClickCard.gameObject.SetActive(isCard);
        rightClickCard.FillInCards(data, alpha);

        rightClickLandscape.gameObject.SetActive(!isCard);
        rightClickLandscape.FillInCards(data, alpha);

        if (alpha == 0)
        {
            artistText.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            artistText.transform.parent.gameObject.SetActive(!data.artCredit.IsNullOrEmpty());
            artistText.text = data.artCredit;
        }
    }

    #endregion

#region Download

    IEnumerator Download(string range)
    {
        if (Application.isEditor)
        {
            string url = $"{baseUrl}{sheetURL}/values/{range}?key={apiKey}";
            using UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Download failed: {www.error}");
            }
            else
            {
                string filePath = $"Assets/Resources/{range}.txt";
                File.WriteAllText($"{filePath}", www.downloadHandler.text);

                string[] allLines = File.ReadAllLines($"{filePath}");
                List<string> modifiedLines = allLines.ToList();
                modifiedLines.RemoveRange(1, 3);
                File.WriteAllLines($"{filePath}", modifiedLines.ToArray());
                Debug.Log($"downloaded {range}");
            }
        }
    }

    IEnumerator GetScripts()
    {
        CoroutineGroup group = new(this);
        group.StartCoroutine(Download("Player Cards"));
        group.StartCoroutine(Download("Area Cards"));
        while (group.AnyProcessing)
            yield return null;

        playerCardFiles = GetDataFiles<PlayerCardData>(ReadFile("Player Cards"));
        areaCardFiles = GetDataFiles<CardData>(ReadFile("Area Cards"));

        string[][] ReadFile(string range)
        {
            TextAsset data = Resources.Load($"{range}") as TextAsset;

            string editData = data.text;
            editData = editData.Replace("],", "").Replace("{", "").Replace("}", "");

            string[] numLines = editData.Split("[");
            string[][] list = new string[numLines.Length][];

            for (int i = 0; i < numLines.Length; i++)
            {
                list[i] = numLines[i].Split("\",");
            }
            return list;
        }
    }

    List<T> GetDataFiles<T>(string[][] data) where T : new()
    {
        Dictionary<string, int> columnIndex = new();
        List<T> toReturn = new();

        for (int i = 0; i < data[1].Length; i++)
        {
            string nextLine = data[1][i].Trim().Replace("\"", "");
            if (!columnIndex.ContainsKey(nextLine))
                columnIndex.Add(nextLine, i);
        }

        for (int i = 2; i < data.Length; i++)
        {
            for (int j = 0; j < data[i].Length; j++)
                data[i][j] = data[i][j].Trim().Replace("\"", "").Replace("\\", "").Replace("]", "");

            if (data[i][0].IsNullOrEmpty())
                continue;

            T nextData = new();
            toReturn.Add(nextData);

            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (columnIndex.TryGetValue(field.Name, out int index))
                {
                    try
                    {
                        string sheetValue = data[i][index];
                        if (field.FieldType == typeof(int))
                            field.SetValue(nextData, StringToInt(sheetValue));
                        else if (field.FieldType == typeof(bool))
                            field.SetValue(nextData, StringToBool(sheetValue));
                        else if (field.FieldType == typeof(string))
                            field.SetValue(nextData, sheetValue);
                    }
                    catch
                    {
                        Debug.LogError($"{field.Name}, {index}");
                    }
                }
            }
        }

        return toReturn;
    }

    int StringToInt(string line)
    {
        line = line.Trim();
        try
        {
            return (line.Equals("")) ? -1 : int.Parse(line);
        }
        catch (FormatException)
        {
            return -1;
        }
    }

    bool StringToBool(string line)
    {
        line = line.Trim();
        return line == "TRUE";
    }

    #endregion

}
