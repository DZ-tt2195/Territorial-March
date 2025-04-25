using UnityEngine;

public class SpreadScout : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        if (Success(player))
            AdvanceTroop(player, logged);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
            mathResult = dataFile.startingCoin + (Success(player) ? dataFile.troopAmount * 4 : 0);
        recalculate = false;
    }

    bool Success(Player player)
    {
        for (int i = 0; i<4; i++)
        {
            AreaCard area = Manager.inst.listOfAreas[i];
            if (area is Camp or Road)
            {
                if (player.CalcTroopScout(i).Item2 < dataFile.miscAmount)
                    return false;
            }
        }
        return true;
    }
}
