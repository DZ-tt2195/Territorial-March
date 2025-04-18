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
        layout.FillInCards(dataFile, 1);
    }

    [PunRPC]
    internal void ResolveArea(int playerPosition, int logged)
    {
        Player player = Manager.inst.playersInOrder[playerPosition];
        player.StartTurn(() => AreaInstructions(player, logged), this.areaNumber);
    }

    public override CardData GetFile()
    {
        return dataFile;
    }

    #endregion

#region Instructions

    public virtual void AreaInstructions(Player player, int logged)
    {
        Log.inst.RememberStep(player, StepType.UndoPoint, () => player.EndTurn());
        if (dataFile.useSheets)
        {
            stepCounter = -1;
            NextStepRPC(player, logged);
        }
    }

    protected (bool, int) ControlThis(Player player, int logged)
    {
        bool answer = player.areasControlled[areaNumber];
        if (answer && logged >= 0)
            NextStepRPC(player, logged);
        return (answer, 0);
    }

    protected (bool, int) ControlNot(Player player, int logged)
    {
        bool answer = !player.areasControlled[areaNumber];
        if (answer && logged >= 0)
            NextStepRPC(player, logged);
        return (answer, 0);
    }

    protected (bool, int) AddScoutHere(Player player, int logged)
    {
        if (logged >= 0)
        {
            player.ChangeScoutRPC(this.areaNumber, dataFile.scoutAmount, logged);
            NextStepRPC(player, logged);
        }
        return (true, dataFile.scoutAmount * 2);
    }

    protected (bool, int) LoseScoutHere(Player player, int logged)
    {
        if (logged >= 0)
        {
            player.ChangeScoutRPC(this.areaNumber, -1 * dataFile.scoutAmount, logged);
            NextStepRPC(player, logged);
        }
        return (true, dataFile.scoutAmount * -2);
    }

    protected (bool, int) AskLoseScoutHere(Player player, int logged)
    {
        mayStopEarly = true;
        (int troop, int scout) = player.CalcTroopScout(this.areaNumber);
        bool answer = scout >= dataFile.scoutAmount;

        if (logged >= 0 && answer)
        {
            Action action = () => LoseScoutHere(player, logged);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Remove {dataFile.scoutAmount} Scout from Area {areaNumber + 1}?", logged));
        }
        return (answer, dataFile.scoutAmount * -2);
    }

    protected (bool, int) SetToTroopHere(Player player, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        return SetAllStats(player, dataFile, troop, logged);
    }

    protected (bool, int) SetToScoutHere(Player player, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        return SetAllStats(player, dataFile, scout, logged);
    }

    protected (bool, int) TroopHereOrMore(Player player, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        return ResolveBoolean(player, troop >= GetFile().miscAmount, logged);
    }

    protected (bool, int) TroopHereOrLess(Player player, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        return ResolveBoolean(player, troop <= GetFile().miscAmount, logged);
    }

    protected (bool, int) ScoutHereOrMore(Player player, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        return ResolveBoolean(player, scout >= GetFile().miscAmount, logged);
    }

    protected (bool, int) ScoutHereOrLess(Player player, int logged)
    {
        (int troop, int scout) = player.CalcTroopScout(areaNumber);
        return ResolveBoolean(player, scout <= GetFile().miscAmount, logged);
    }

    #endregion

}
