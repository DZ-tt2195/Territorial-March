using UnityEngine;

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
        for (int i = 0; i<4; i++)
        {
            (int troop, int scout) = player.CalcTroopScout(i);
            if (troop == dataFile.troopAmount)
                player.ChangeScoutRPC(i, GetFile().scoutAmount, logged);
        }
    }
}
