using UnityEngine;

public class Castle : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        if (!player.BoolFromAbilities(true, nameof(IgnoreArea), IgnoreArea.CheckParameters(), logged))
        {
            int total = 0;
            for (int i = 0; i < 4; i++)
            {
                if (player.CalcTroopScout(i).Item1 >= dataFile.troopAmount)
                    total++;
            }
            player.DrawCardRPC(total * dataFile.cardAmount, logged);
        }
    }
}
