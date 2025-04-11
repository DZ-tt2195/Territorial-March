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
        for (int i = 0; i<4; i++)
        {
            (int troop, int scout) = player.CalcTroopScout(i);
            if (troop >= dataFile.troopAmount)
                player.DrawCardRPC(1, logged);
        }
    }
}
