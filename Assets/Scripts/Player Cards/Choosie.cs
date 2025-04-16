using UnityEngine;

public class Choosie : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseOption(player, logged));
    }

    void ChooseOption(Player player, int logged)
    {
        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, new() { 0, 1, 2 });
        else
            player.ChooseButton(new() { "+4 Coin", "+2 Scout", "+1 Troop" }, Vector3.zero, $"Choose one for {this.name}.", Next);

        void Next()
        {
            switch (player.choice)
            {
                case 0:
                    player.ResourceRPC(Resource.Coin, dataFile.coinAmount, logged);
                    break;
                case 1:
                    AddScout(player, GetFile(), logged);
                    break;
                case 2:
                    AdvanceTroop(player, GetFile(), logged);
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
