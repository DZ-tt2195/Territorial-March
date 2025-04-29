using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using System.Linq;

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

    protected (bool, int) PlayCard(Player player, int logged)
    {
        List<int> sortedCards = SimulatePlay(player);
        if (logged >= 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePlay(player, sortedCards, logged));
        return (true, sortedCards.Count == 0 ? 0 : sortedCards.Max() - 6);
    }

    void ChoosePlay(Player player, List<int> sortedCards, int logged)
    {
        List<string> actions = new() { $"Don't Play" };
        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, sortedCards);
        }
        else
        {
            player.ChooseButton(actions, Vector3.zero, "What to play?", Next);
            player.ChooseCardOnScreen(player.cardsInHand, "What to play?", null);
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice < player.cardsInHand.Count && convertedChoice >= 0)
            {
                PlayerCard toPlay = (PlayerCard)player.cardsInHand[convertedChoice];
                Log.inst.AddTextRPC(player, $"{player.name} spends 1 Action to play {toPlay.name}.", LogAdd.Remember, logged);
                player.DiscardPlayerCard(toPlay, -1);
                player.ResourceRPC(Resource.Action, -1, -1);

                Log.inst.RememberStep(this, StepType.Holding, () => Loop(player, logged));
                toPlay.ResolveCard(player, logged + 1);
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't play a card.", LogAdd.Personal, logged);
            }
            NextStepRPC(player, logged);
        }
    }

    void Loop(Player player, int logged)
    {
        if (player.resourceDict[Resource.Action] >= 1 && player.cardsInHand.Count >= 1)
            PlayCard(player, logged);
    }

    List<int> SimulatePlay(Player player)
    {
        player.resourceDict[Resource.Action]--;
        List<int> sortedCards = new() { -1 };
        for (int i = 0; i < player.cardsInHand.Count; i++)
            player.cardsInHand[i].recalculate = true;

        for (int i = 0; i < player.cardsInHand.Count; i++)
        {
            Card card = player.cardsInHand[i];
            card.DoMath(player);
            if (card.mathResult >= 6)
                sortedCards.Add(i + 100);
        }

        player.resourceDict[Resource.Action]++;
        return sortedCards;
    }
}