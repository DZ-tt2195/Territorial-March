using Photon.Pun;
using UnityEngine;

public class Region : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => Loop(player, logged));
    }

    protected override void PostAdvance(Player player, bool success, CardData dataFile, int logged)
    {
        if (success)
        {
            player.ResourceRPC(Resource.Coin, -dataFile.coinAmount, logged);
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
            if (player.resourceDict[Resource.Coin] >= dataFile.coinAmount)
                AskAdvance(player, GetFile(), logged);
        }
    }
}
