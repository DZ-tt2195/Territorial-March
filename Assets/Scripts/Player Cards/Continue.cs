using System;
using System.Collections.Generic;
using UnityEngine;

public class Continue : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        if (CalculateCardsDrawn(player) >= dataFile.miscAmount)
        {
            player.DrawCardRPC(dataFile.cardAmount, logged);
            player.ResourceRPC(Resource.Action, dataFile.actionAmount, logged);
        }
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = dataFile.startingCoin;
            if (CalculateCardsDrawn(player) >= dataFile.miscAmount)
                mathResult += dataFile.cardAmount * 3 + dataFile.actionAmount * 3;
        }
        recalculate = false;
    }

    int CalculateCardsDrawn(Player player)
    {
        return Log.inst.SearchHistory("DrawFromDeck").Count;
    }
}
