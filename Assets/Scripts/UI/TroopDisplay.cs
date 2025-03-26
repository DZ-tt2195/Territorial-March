using UnityEngine;
using UnityEngine.UI;
using MyBox;
using Photon.Pun;
using TMPro;

public class TroopDisplay : PhotonCompatible
{
    [SerializeField] private int playerPosition;
    public int PlayerPosition => playerPosition;
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
        this.transform.localScale = Vector3.Lerp(Vector3.one, Manager.inst.canvas.transform.localScale, 0.5f);
    }

    private void FixedUpdate()
    {
        try { this.border.SetAlpha(Manager.inst.opacity); } catch { }
    }

    public void UpdateText(string text, Color color)
    {
        this.button.image.color = color;
        myText.text = text;
    }
}
