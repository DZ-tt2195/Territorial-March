using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pasture : AreaCard
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
            AddScoutHere(player, logged);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => RemoveScouts(player, logged));
        }
    }

    void RemoveScouts(Player player, int logged)
    {
        int scoutsHere = player.CalcTroopScout(areaNumber).Item2;
        if (player.myType == PlayerType.Bot)
        {
            List<int> possibleRemove = new();
            for (int i = 0; i <= scoutsHere; i++)
                possibleRemove.Add(i);
            player.AIDecision(Next, possibleRemove);
        }
        else
        {
            player.ChooseSlider(0, scoutsHere, "How many Scout to remove?", Next);
        }

        void Next()
        {
            if (player.choice == 0)
            {
                Log.inst.AddTextRPC(player, $"{player.name} removes 0 Scout from Area {areaNumber + 1}.", LogAdd.Remember, logged);
            }
            else
            {
                player.ChangeScoutRPC(areaNumber, -1 * player.choice, logged);
                player.ResourceRPC(Resource.Coin, GetFile().coinAmount * player.choice, logged);
            }
        }
    }
}
