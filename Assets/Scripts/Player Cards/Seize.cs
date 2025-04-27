using System.Collections.Generic;
using Photon.Pun;

public class Seize : PlayerCard
{
    int chosenArea;

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseArea(player, logged));
    }

    void ChooseArea(Player player, int logged)
    {
        List<int> canControl = new();
        for (int i = 0; i<4; i++)
        {
            if (!player.areasControlled[i])
                canControl.Add(i);
        }

        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, player.ConvertToHundred(canControl));
        else
            player.ChooseTroopDisplay(canControl, "Choose an Area to control.", Next);

        void Next()
        {
            int convertedChoice = player.choice - 100;
            AddAbilityRPC(player);
            Log.inst.RememberStep(this, StepType.Revert, () => ControlThisArea(false, convertedChoice));
            player.UpdateAreaControl(convertedChoice, true, logged);
        }
    }

    protected override TriggeredAbility SetupAbility(Player player)
    {
        ControlArea controlAbility = new(this, WhenDelete.UntilCamp, Control, AroundThis);
        return controlAbility;

        bool AroundThis(string condition, object[] parameters)
        {
            int area = (int)parameters[0];
            return area == chosenArea;
        }
        bool Control(int logged, object[] parameters)
        {
            return true;
        }
    }

    [PunRPC]
    void ControlThisArea(bool undo, int number)
    {
        chosenArea = number;
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
            mathResult = dataFile.startingCoin + 2;
        recalculate = false;
    }
}
