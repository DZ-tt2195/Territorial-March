using UnityEngine;

public class Crossroads : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        (int area1Troop, int area1Scout) = player.CalcTroopScout(1);
        (int area2Troop, int area2Scout) = player.CalcTroopScout(2);
        player.ResourceRPC(Resource.Coin, dataFile.coinAmount * Mathf.Min(area1Troop, area2Troop), logged);
    }
}
