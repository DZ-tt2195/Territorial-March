using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System;

public class AreaCard : Card
{

#region Setup

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

    public override CardData GetFile()
    {
        return dataFile;
    }


    #endregion

#region Instructions

    protected virtual void AreaInstructions(Player player, int logged)
    {
        Log.inst.RememberStep(this, StepType.UndoPoint, () => player.EndTurn());
        if (dataFile.useSheets)
        {
            stepCounter = -1;
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
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

    protected void AddScoutHere(Player player, CardData dataFile, int logged)
    {
        player.ChangeScoutRPC(this.areaNumber, dataFile.scoutAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void LoseScoutHere(Player player, CardData dataFile, int logged)
    {
        player.ChangeScoutRPC(this.areaNumber, -1 * dataFile.scoutAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void AskLoseScoutHere(Player player, CardData dataFile, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(this.areaNumber);
        if (scout < dataFile.scoutAmount)
            return;

        Action action = () => LoseScoutHere(player, dataFile, logged);
        if (dataFile.scoutAmount == 0)
            action();
        else
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Remove {dataFile.scoutAmount} Scout from Area {areaNumber+1}?", dataFile, logged));
    }

    protected void SetToTroopHere(Player player, CardData dataFile, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        SetAllStats(troop, dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void SetToScoutHere(Player player, CardData dataFile, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        SetAllStats(scout, dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    #endregion

}
