using System.Linq;
using UnityEngine;

public class CheckScout : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        AddScout(player, logged);
    }

    protected override void PostAddScout(Player player, int logged)
    {
        int convertedChoice = player.choice - 100;
        player.ResourceRPC(Resource.Coin, player.CalcTroopScout(convertedChoice).Item2, logged);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            int mostScout = Enumerable.Range(0, 4).Select(i => player.CalcTroopScout(i).Item2).Max();
            mathResult = dataFile.startingCoin + dataFile.scoutAmount + (mostScout+1);
        }
        recalculate = false;
    }
}
