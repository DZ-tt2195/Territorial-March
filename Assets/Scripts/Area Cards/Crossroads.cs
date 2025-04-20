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
        player.ResourceRPC(Resource.Coin, dataFile.coinAmount *
            Mathf.Min(player.CalcTroopScout(1).Item1, player.CalcTroopScout(2).Item2), logged);
    }
}
