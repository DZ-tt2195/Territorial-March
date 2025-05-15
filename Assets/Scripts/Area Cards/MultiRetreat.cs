using UnityEngine;

public class MultiRetreat : AreaCard
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
            if (player.CalcTroopScout(1).Item1 != player.CalcTroopScout(2).Item1)
                RetreatTroop(player, logged);
        }
    }
}
