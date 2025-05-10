using UnityEngine;

public class Hi : AreaCard
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
            AskLoseAction(player, logged);
    }

    protected override void PostPayment(Player player, bool success, int logged)
    {
        if (success)
        {
            int amount = (int)(player.CalcTroopScout(this.areaNumber).Item1 / dataFile.miscAmount);
            player.ResourceRPC(Resource.Coin, amount, logged);
        }
    }
}
