using System.Collections.Generic;
using UnityEngine;

public class Pawn : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        for (int i = 0; i < dataFile.miscAmount; i++)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseOption(player, logged));
    }

    void ChooseOption(Player player, int logged)
    {
        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, new() { 0, 1, 2 });
        else
            player.ChooseButton(new() { "+1 Card", "+1 Action", "+3 Coin" }, Vector3.zero, $"Choose one for {this.name}.", Next);

        void Next()
        {
            switch (player.choice)
            {
                case 0:
                    player.DrawCardRPC(dataFile.cardAmount, logged);
                    break;
                case 1:
                    player.ResourceRPC(Resource.Action, dataFile.actionAmount, logged);
                    break;
                case 2:
                    player.ResourceRPC(Resource.Coin, dataFile.coinAmount, logged);
                    break;
            }
        }
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
            mathResult = 6;
        recalculate = false;
    }
}
