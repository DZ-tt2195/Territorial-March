using System.Collections.Generic;
using System.Linq;

public class NoControl : AreaCard
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
            List<int> canAdd = CanAdd(player);
            if (canAdd.Count >= 1)
                AskDiscardCard(player, logged);
        }
    }

    protected override void PostDiscard(Player player, bool success, int logged)
    {
        List<int> canAdd = CanAdd(player);
        if (success)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseArea(player, canAdd, logged));
    }

    void ChooseArea(Player player, List<int> canAdd, int logged)
    {
        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, player.ConvertToHundred(canAdd));
        else
            player.ChooseTroopDisplay(canAdd, $"Add {dataFile.scoutAmount} Scout to an Area you don't control.", Next);

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
                player.ChangeScoutRPC(convertedChoice, dataFile.scoutAmount, logged);
        }
    }

    protected override List<int> CanAdd(Player player)
    {
        return Enumerable.Range(0, 4).Where(i => player.areasControlled[i]).ToList();
    }
}
