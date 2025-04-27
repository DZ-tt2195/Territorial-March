using UnityEngine;

public class Bank : AreaCard
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
            if (player.resourceDict[Resource.Coin] >= GetFile().miscAmount)
            {
                int amount = (int)(player.resourceDict[Resource.Coin] / GetFile().miscAmount);
                player.ResourceRPC(Resource.Coin, amount, logged);
            }
        }
    }
}
