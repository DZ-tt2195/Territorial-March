using UnityEngine;

public class Another : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        (int troop, int scout) = player.CalcTroopScout(1);
        player.ResourceRPC(Resource.Coin, troop, logged);
        player.MoveTroopRPC(1, 2, -1);
        Log.inst.AddTextRPC(player, $"{player.name} moves {troop} Troop to Area 3.", LogAdd.Remember, logged);
    }
}
