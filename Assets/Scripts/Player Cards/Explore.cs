using UnityEngine;
using System.Collections.Generic;

public class Explore : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        foreach (int area in ToAddScout(player))
            player.ChangeScoutRPC(area, GetFile().scoutAmount, logged);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = this.dataFile.startingCoin;
            mathResult += dataFile.scoutAmount * 2 * ToAddScout(player).Count;
        }
        recalculate = false;
    }

    List<int> ToAddScout(Player player)
    {
        List<int> canAdd = new();
        for (int i = 0; i < 4; i++)
        {
            if (player.CalcTroopScout(i).Item1 == dataFile.miscAmount)
                canAdd.Add(i);
        }
        return canAdd;
    }
}
