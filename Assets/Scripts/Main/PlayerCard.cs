using UnityEngine;

public class PlayerCard : Card
{
    public CardData dataFile { get; private set; }

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

    public virtual void ActivateThis(Player player, int logged)
    {
        if (dataFile.useSheets)
        {
            stepCounter = -1;
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
        }
    }
}