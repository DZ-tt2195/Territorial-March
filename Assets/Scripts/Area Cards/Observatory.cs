using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class Observatory : AreaCard
{
    Transform areaDeck;
    AreaCard topArea;
    bool hasShuffled = false;
    Button topButton;

    protected override void Awake()
    {
        base.Awake();
        areaDeck = GameObject.Find("Area Deck").transform;
        this.bottomType = this.GetType();

        topButton = Instantiate(CarryVariables.inst.textButton);
        topButton.transform.SetParent(this.transform);
        topButton.transform.localPosition = new(115, 30);
        topButton.transform.localScale = new(0.5f, 0.5f);
    }

    public override void AreaTurnEffect()
    {
        if (!hasShuffled)
        {
            hasShuffled = true;
            areaDeck.Shuffle();
        }
        DoFunction(() => ChangeTopCard(areaDeck.GetChild(0).GetComponent<PhotonView>().ViewID));
    }

    [PunRPC]
    void ChangeTopCard(int ID)
    {
        topArea = PhotonView.Find(ID).GetComponent<AreaCard>();
        topArea.AssignAreaNum(this.areaNumber);

        topButton.GetComponentInChildren<TMP_Text>().text = topArea.name;
        topButton.onClick.RemoveAllListeners();
        topButton.onClick.AddListener(() => CarryVariables.inst.RightClickDisplay(topArea.dataFile, 1, false));
    }

    public override void AreaInstructions(Player player, int logged)
    {
        if (Manager.inst.AmMaster() && player.myType != PlayerType.Bot)
        {
            Log.inst.AddTextRPC(null, $"{this.name} chooses {topArea.name}.", LogAdd.Public);
            topArea.transform.SetAsLastSibling();
        }
        if (!player.BoolFromAbilities(true, nameof(IgnoreArea), IgnoreArea.CheckParameters(), logged))
            topArea.AreaInstructions(player, logged);
        else
            Log.inst.RememberStep(player, StepType.UndoPoint, () => player.EndTurn());
    }
}
