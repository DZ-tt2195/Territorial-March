using UnityEngine;

public class PlayerCard : Card
{
    public PlayerCardData dataFile { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    internal override void AssignInfo(int fileNumber)
    {
        this.dataFile = CarryVariables.inst.playerCardFiles[fileNumber];
        GetInstructions(dataFile);
    }

    public override CardData GetFile()
    {
        return dataFile;
    }

    public virtual void ResolveCard(Player player, int logged)
    {
        player.ResourceRPC(Resource.Coin, this.dataFile.coinBonus, logged);
        if (dataFile.useSheets)
        {
            stepCounter = -1;
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
    }
}