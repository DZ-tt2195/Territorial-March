using System.Collections.Generic;
using UnityEngine;

public class LoseAdvance : AreaCard
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
            AskLoseScout(player, logged);
    }

    protected override (int, List<int>) CanLose(Player player)
    {
        int total = 0;
        List<int> canLose = new();
        for (int i = 0; i < 3; i++)
        {
            int scout = player.CalcTroopScout(i).Item2;
            if (scout > 0)
            {
                total += scout;
                canLose.Add(i);
            }
        }
        return (total, canLose);
    }

    protected override void PostLoseScout(Player player, bool success, int logged)
    {
        if (success)
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice < 3)
                ChooseAdvanceTwo(player, convertedChoice, false, 1, logged);
        }
    }
}
