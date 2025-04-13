using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

public class PlayerCard : Card
{
    public PlayerCardData dataFile { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    internal override void AssignInfo(int fileNumber)
    {
        this.dataFile = CarryVariables.inst.playerCardFiles[fileNumber];
        GetInstructions(dataFile);
        layout.FillInCards(dataFile, 1);
    }

    public override CardData GetFile()
    {
        return dataFile;
    }

    public virtual void ResolveCard(Player player, int logged)
    {
        player.ResourceRPC(Resource.Coin, this.dataFile.startingCoin, logged);
        if (dataFile.useSheets)
        {
            stepCounter = -1;
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = this.dataFile.startingCoin;
            player.resourceDict[Resource.Coin] += dataFile.startingCoin;
            int index = player.cardsInHand.IndexOf(this);
            player.cardsInHand.RemoveAt(index);

            foreach (string next in activationSteps)
            {
                MethodInfo method = FindMethod(next);
                if (method.ReturnType == typeof((bool, int)))
                {
                    (bool success, int effect) = (ValueTuple<bool, int>)method.Invoke(this, new object[3] { player, GetFile(), -1 });
                    if (success)
                        mathResult += effect;
                    else
                        break;
                }
            }
            player.resourceDict[Resource.Coin] -= dataFile.startingCoin;
            player.cardsInHand.Insert(index, this);
        }
        recalculate = false;
    }
}