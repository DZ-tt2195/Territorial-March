using System.Collections.Generic;
using UnityEngine;

public class Island : AreaCard
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
            if (player.CalcTroopScout(3).Item1 >= dataFile.miscAmount)
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceTwo(player, 0, false, 1, logged));
        }
    }
}
