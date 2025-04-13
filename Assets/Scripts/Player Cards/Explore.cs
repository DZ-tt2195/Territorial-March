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

    public override int DoMath(Player player)
    {
        int answer = this.dataFile.startingCoin;
        foreach (int area in ToAddScout(player))
            answer += dataFile.scoutAmount * 2;

        return answer;
    }

    List<int> ToAddScout(Player player)
    {
        List<int> canAdd = new();
        for (int i = 0; i < 4; i++)
        {
            (int troop, int scout) = player.CalcTroopScout(i);
            if (troop == dataFile.troopAmount)
                canAdd.Add(i);
        }
        return canAdd;
    }
}
