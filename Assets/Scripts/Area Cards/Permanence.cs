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
        if (player.CalcTroopScout(this.areaNumber).Item2 > player.cardsInHand.Count)
            DrawCard(player, logged);
    }
}
