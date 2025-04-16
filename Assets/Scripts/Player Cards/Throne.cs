using System.Collections.Generic;
using UnityEngine;

public class Throne : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        AskLoseAction(player, GetFile(), logged);
    }

    protected override void PostPayment(Player player, bool success, CardData dataFile, int logged)
    {
        if (success)
            PlayCard(player, dataFile, logged);
    }

    protected override void PostPlaying(Player player, PlayerCard cardToPlay, CardData dataFile, int logged)
    {
        if (cardToPlay != null)
        {
            for (int i = 0; i < dataFile.miscAmount - 1; i++)
                Log.inst.RememberStep(this, StepType.Holding, () => ReplayCard(player, cardToPlay, logged));
        }
        base.PostPlaying(player, cardToPlay, dataFile, logged);
    }

    void ReplayCard(Player player, PlayerCard cardToPlay, int logged)
    {
        Log.inst.AddTextRPC(player, $"{player.name} plays {cardToPlay.name} again.", LogAdd.Remember, logged);
        cardToPlay.ResolveCard(player, logged + 1);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            if (player.resourceDict[Resource.Action] >= dataFile.actionAmount)
                mathResult = -dataFile.actionAmount + (2 * dataFile.miscAmount - 1) * 3;
            else
                mathResult = 0;
        }
        recalculate = false;
    }
}
