using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Secure : PlayerCard
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
        if (canAdd.Count >= 1)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseArea(player, canAdd, logged));
    }

    void ChooseArea(Player player, List<int> canAdd, int logged)
    {
        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, player.ConvertToHundred(canAdd));
        else
            player.ChooseTroopDisplay(canAdd, $"Add {dataFile.scoutAmount} Scout to an Area with {this.name}.", Next);

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
                player.ChangeScoutRPC(convertedChoice, dataFile.scoutAmount, logged);
        }
    }

    protected override List<int> CanAdd(Player player)
    {
        List<int> canAdd = new();
        for (int i = 0; i<4; i++)
        {
            if (player.areasControlled[i])
            {
                (int troop, int scout) = player.CalcTroopScout(i);
                if (troop == dataFile.miscAmount || scout == dataFile.miscAmount)
                    canAdd.Add(i);
            }
        }
        return canAdd;
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
            mathResult = this.dataFile.startingCoin + ((CanAdd(player).Count >= 1) ? dataFile.scoutAmount * 2 : 0);
        recalculate = false;
    }
}
