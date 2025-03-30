using UnityEngine;
using UnityEngine.UI;
using MyBox;
using Photon.Pun;
using TMPro;

public class TroopDisplay : PhotonCompatible
{
    public int playerPositon { get; private set; }
    public int areaPosition { get; private set; }

    TMP_Text myText;
    public Button button { get; private set; }
    public Image border { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();

        border = this.transform.Find("Border").GetComponent<Image>();
        button = GetComponent<Button>();
        myText = this.transform.Find("Text").GetComponent<TMP_Text>();
    }

    private void FixedUpdate()
    {
        try { this.border.SetAlpha(Manager.inst.opacity); } catch { }
    }

    public void AssignInfo(int playerPosition, int areaPosition)
    {
        this.playerPositon = playerPositon;
        this.areaPosition = areaPosition;
    }

    public void UpdateText(string text, Color color)
    {
        this.gameObject.SetActive(true);
        myText.text = KeywordTooltip.instance.EditText(text);
        this.button.image.color = color;
    }
}
