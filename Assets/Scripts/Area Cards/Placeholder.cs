using UnityEngine;

public class Placeholder : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    protected override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        int totalMoney = 0;
        for (int i = 0; i<4; i++)
        {
            (int troop, int scout) = player.CalcTroopScout(i);
            if (troop >= 3)
                totalMoney += 2;
        }
        player.ResourceRPC(Resource.Coin, totalMoney, logged);
    }
}
