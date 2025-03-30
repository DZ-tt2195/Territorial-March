using Photon.Pun;
using UnityEngine;

public class Camp : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    protected override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        player.DrawCardRPC(dataFile.cardAmount, logged);
        player.ResourceRPC(Resource.Action, dataFile.actionAmount, logged);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => Loop(player, logged));
    }

    protected override void PostPlaying(Player player, PlayerCard cardToPlay, CardData dataFile, int logged)
    {
        if (cardToPlay != null)
        {
            player.ResourceRPC(Resource.Action, -1, -1);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => Loop(player, logged));
        }
    }

    void Loop(Player player, int logged)
    {
        Log.inst.undoToThis = null;
        if (player.resourceDict[Resource.Action] >= 1)
            PlayCard(player, GetFile(), logged);
        player.PopStack();
    }
}
