using System.Collections.Generic;
using UnityEngine;

public class Occupy : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        List<int> canAdd = CanAdd(player);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseArea(player, canAdd, logged));
    }

    void ChooseArea(Player player, List<int> canAdd, int logged)
    {
        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, player.ConvertToHundred(canAdd, false));
        else
            player.ChooseTroopDisplay(canAdd, $"Choose an Area to control.", Next);

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
                player.UpdateAreaControl(convertedChoice, true, logged);
        }
    }

    protected override List<int> CanAdd(Player player)
    {
        List<int> notControlThis = new();
        for (int i = 0; i < 4; i++)
        {
            if (!player.areasControlled[i])
                notControlThis.Add(i);
        }
        return notControlThis;
    }
}
