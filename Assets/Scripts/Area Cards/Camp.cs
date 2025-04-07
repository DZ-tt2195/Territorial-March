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
        Log.inst.RememberStep(this, StepType.UndoPoint, () => Loop(player, logged));
    }

    protected override void PostPlaying(Player player, PlayerCard cardToPlay, CardData dataFile, int logged)
    {
        if (cardToPlay != null)
        {
            player.ResourceRPC(Resource.Action, -1, logged);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => Loop(player, logged));
        }
    }

    void Loop(Player player, int logged)
    {
        Log.inst.undoToThis = null;
        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, new() { -1 });
        }
        else
        {
            player.inReaction.Add(Next);
            player.PopStack();
        }

        void Next()
        {
            if (player.resourceDict[Resource.Action] >= 1)
                PlayCard(player, GetFile(), logged);
        }
    }
}
