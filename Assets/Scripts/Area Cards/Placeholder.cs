using UnityEngine;

public class Placeholder : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        int total = 0;
        for (int i = 0; i<4; i++)
        {
            if (player.CalcTroopScout(i).Item1 >= dataFile.troopAmount)
                total++;
        }
        player.ResourceRPC(Resource.Coin, total * dataFile.coinAmount, logged);
    }
}
