using System.Collections.Generic;
using UnityEngine;

public class ScoutAction : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseArea(player, logged));
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = dataFile.startingCoin;
            mathResult += dataFile.actionAmount * 3 * MostScout(player).Item2;
        }
        recalculate = false;
    }

    void ChooseArea(Player player, int logged)
    {
        if (player.myType == PlayerType.Bot)
        {
            List<int> possibilities = new() { MostScout(player).Item1 };
            player.AIDecision(Next, possibilities);
        }
        else
        {
            player.ChooseTroopDisplay(new() { 0, 1, 2, 3 }, "Choose an Area.", Next);
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            player.ResourceRPC(Resource.Action, player.CalcTroopScout(convertedChoice).Item2 * dataFile.actionAmount, logged);
        }
    }

    (int, int) MostScout(Player player)
    {
        int bestArea = 0;
        int bestScout = 0;

        for (int i = 0; i<4; i++)
        {
            if (player.CalcTroopScout(i).Item2 > bestScout)
            {
                bestArea = i;
                bestScout = player.CalcTroopScout(i).Item2;
            }
        }
        return (bestArea, bestScout);
    }
}
