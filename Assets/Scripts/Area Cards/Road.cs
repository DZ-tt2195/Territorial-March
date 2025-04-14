using Photon.Pun;
using UnityEngine;

public class Road : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        Log.inst.RememberStep(this, StepType.Holding, () => Loop(player, logged));
    }

    protected override void PostAdvance(Player player, bool success, CardData dataFile, int logged)
    {
        if (success)
        {
            player.ResourceRPC(Resource.Coin, -1 * dataFile.coinAmount, logged);
            Log.inst.RememberStep(this, StepType.Holding, () => Loop(player, logged));
        }
    }

    void Loop(Player player, int logged)
    {
        if (player.resourceDict[Resource.Coin] >= dataFile.coinAmount)
            AskAdvanceTroop(player, GetFile(), logged);
    }
}
