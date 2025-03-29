using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class AreaCard : Card
{
    public CardData dataFile { get; private set; }
    public int areaNumber { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    internal void AssignAreaNum(int number)
    {
        areaNumber = number;
    }

    internal override void AssignInfo(int fileNumber)
    {
        this.dataFile = CarryVariables.inst.areaCardFiles[fileNumber];
        GetInstructions(dataFile);
    }

    [PunRPC]
    internal void ResolveArea(int playerPosition, int logged)
    {
        Player player = Manager.inst.playersInOrder[playerPosition];
        player.StartTurn(() => AreaInstructions(player, logged));
    }

    protected virtual void AreaInstructions(Player player, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => player.EndTurn());
        if (dataFile.useSheets)
        {
            stepCounter = -1;
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
    }

    public override CardData GetFile()
    {
        return dataFile;
    }

    protected void ControlThis(Player player, CardData dataFile, int logged)
    {
        if (player.areasControlled[areaNumber])
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void ControlThisNot(Player player, CardData dataFile, int logged)
    {
        if (!player.areasControlled[areaNumber])
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }
}
