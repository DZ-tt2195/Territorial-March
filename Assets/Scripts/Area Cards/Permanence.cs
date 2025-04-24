using UnityEngine;

public class Permanence : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        (int troop, int scout) = player.CalcTroopScout(this.areaNumber);
        if (scout > troop)
            AdvanceTroop(player, logged);
    }
}
