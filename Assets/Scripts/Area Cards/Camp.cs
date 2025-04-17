using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Camp : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        player.DrawCardRPC(dataFile.cardAmount, logged);
        player.ResourceRPC(Resource.Action, dataFile.actionAmount, logged);
        Log.inst.RememberStep(this, StepType.Holding, () => Loop(player, logged));
    }

    protected override void PostPlay(Player player, PlayerCard cardToPlay, CardData dataFile, int logged)
    {
        base.PostPlay(player, cardToPlay, dataFile, logged);
        if (cardToPlay != null)
        {
            player.ResourceRPC(Resource.Action, -1, logged);
            Log.inst.RememberStep(this, StepType.Holding, () => Loop(player, logged));
        }
    }

    void Loop(Player player, int logged)
    {
        if (player.resourceDict[Resource.Action] >= 1 && player.cardsInHand.Count >= 1)
            PlayCard(player, GetFile(), logged);
    }

    protected override List<int> SimulatePlay(Player player)
    {
        player.resourceDict[Resource.Action]--;
        List<int> simulate = base.SimulatePlay(player);
        player.resourceDict[Resource.Action]++;
        return simulate;
    }
}
