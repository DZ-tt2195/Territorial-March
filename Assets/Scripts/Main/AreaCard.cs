using UnityEngine;

public class AreaCard : Card
{
    public CardData dataFile { get; private set; }
    public int areaNumber { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    internal void AssignAreaNum(int number)
    {
        areaNumber = number;
    }

    internal override void AssignInfo(int fileNumber)
    {
        this.dataFile = CarryVariables.inst.areaCardFiles[fileNumber];
        GetInstructions(dataFile);
    }

    public override CardData GetFile()
    {
        return dataFile;
    }

    protected void ControlThis(Player player, CardData dataFile, int logged)
    {
        if (player.areasControlled[areaNumber])
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void ControlThisNot(Player player, CardData dataFile, int logged)
    {
        if (!player.areasControlled[areaNumber])
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }
}
