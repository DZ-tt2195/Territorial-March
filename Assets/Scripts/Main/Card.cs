using UnityEngine;
using UnityEngine.UI;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Photon.Pun;

public class Card : PhotonCompatible
{

#region Setup

    public Button button { get; private set; }
    public Image border { get; private set; }
    public CardLayout layout { get; private set; }

    protected List<string> activationSteps = new();
    protected int stepCounter;
    protected int sideCounter;
    protected bool mayStopEarly;

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();

        border = this.transform.Find("Border").GetComponent<Image>();
        button = GetComponent<Button>();
        layout = GetComponent<CardLayout>();

        Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        this.transform.localScale = Vector3.Lerp(Vector3.one, canvas.transform.localScale, 0.5f);
    }

    public virtual Color MyColor()
    {
        return Color.white;
    }

    internal virtual void AssignInfo(int fileNumber)
    {
    }

    protected void GetInstructions(CardData dataFile)
    {
        this.name = dataFile.cardName;
        if (dataFile.useSheets)
        {
            activationSteps = SpliceString(dataFile.cardInstructions);
            foreach (string next in activationSteps)
            {
                if (FindMethod(next) == null)
                    Debug.LogError($"{this.name} - {next} is wrong");
            }
        }

        List<string> SpliceString(string text)
        {
            if (text.IsNullOrEmpty())
            {
                return new();
            }
            else
            {
                string divide = text.Replace(" ", "").Trim();
                return divide.Split('/').ToList();
            }
        }
    }

    public virtual CardData GetFile()
    {
        return null;
    }

    public virtual int DoMath(Player player)
    {
        return 0;
    }

    #endregion

#region Animations

    public IEnumerator MoveCard(Vector3 newPos, float waitTime, Vector3 newScale)
    {
        float elapsedTime = 0;
        Vector2 originalPos = this.transform.localPosition;
        Vector2 originalScale = this.transform.localScale;

        while (elapsedTime < waitTime)
        {
            this.transform.localPosition = Vector3.Lerp(originalPos, newPos, elapsedTime / waitTime);
            this.transform.localScale = Vector3.Lerp(originalScale, newScale, elapsedTime / waitTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.transform.localPosition = newPos;
    }

    public IEnumerator RevealCard(float totalTime)
    {
        if (this.layout.GetAlpha() == 1f)
            yield break;

        transform.localEulerAngles = new Vector3(0, 0, 0);
        float elapsedTime = 0f;

        Vector3 originalRot = this.transform.localEulerAngles;
        Vector3 newRot = new(0, 90, 0);

        while (elapsedTime < totalTime)
        {
            this.transform.localEulerAngles = Vector3.Lerp(originalRot, newRot, elapsedTime / totalTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.layout.FillInCards(GetFile(), 1);
        elapsedTime = 0f;

        while (elapsedTime < totalTime)
        {
            this.transform.localEulerAngles = Vector3.Lerp(newRot, originalRot, elapsedTime / totalTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.transform.localEulerAngles = originalRot;
    }

    private void FixedUpdate()
    {
        try { this.border.SetAlpha(Manager.inst.opacity); } catch { }
    }

    #endregion

#region Ministeps

    #region Misc

    [PunRPC]
    protected void DoNextStep(bool undo, Player player, CardData dataFile, int logged)
    {
        if (dataFile.useSheets && logged >= 0)
        {
            if (undo)
            {
                stepCounter--;
            }
            else
            {
                stepCounter++;
                if (stepCounter < activationSteps.Count)
                    StringParameters(activationSteps[stepCounter], new object[3] { player, dataFile, logged });
            }
        }
    }

    [PunRPC]
    protected void ChangeSideCount(bool undo, int change)
    {
        if (undo)
            sideCounter -= change;
        else
            sideCounter += change;
    }

    [PunRPC]
    protected void SetSideCount(bool undo, int newNumber)
    {
        ChangeSideCount(undo, newNumber - sideCounter);
    }

    #endregion

    #region +/- Resources

    protected (bool, int) DrawCard(Player player, CardData dataFile, int logged)
    {
        if (logged >= 0)
        {
            player.DrawCardRPC(dataFile.cardAmount, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
        return (true, dataFile.cardAmount * 3);
    }

    protected (bool, int) AddCoin(Player player, CardData dataFile, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Coin, dataFile.coinAmount, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
        return (true, dataFile.coinAmount);
    }

    protected (bool, int) LoseCoin(Player player, CardData dataFile, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Coin, -1 * dataFile.coinAmount, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
        return (true, -1 * dataFile.coinAmount);
    }

    protected (bool, int) AddAction(Player player, CardData dataFile, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Action, dataFile.actionAmount, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
        return (true, dataFile.actionAmount * 3);
    }

    protected (bool, int) LoseAction(Player player, CardData dataFile, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Action, -1 * dataFile.actionAmount, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
        return (true, dataFile.actionAmount * -3);
    }

    #endregion

    #region Setters

    protected (bool, int) SetAllStats(Player player, CardData dataFile, int number, int logged)
    {
        float multiplier = (dataFile.miscAmount >= 0) ? dataFile.miscAmount : -1f / dataFile.miscAmount;
        int calculated = (int)Mathf.Floor(number * multiplier);
        dataFile.cardAmount = calculated;
        dataFile.coinAmount = calculated;
        dataFile.scoutAmount = calculated;
        dataFile.actionAmount = calculated;
        dataFile.troopAmount = calculated;

        if (logged >= 0)
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        return (true, calculated);
    }

    protected (bool, int) SetToHand(Player player, CardData dataFile, int logged)
    {
        return SetAllStats(player, dataFile, player.cardsInHand.Count, logged);
    }

    protected (bool, int) SetToCoin(Player player, CardData dataFile, int logged)
    {
        return SetAllStats(player, dataFile, player.resourceDict[Resource.Coin], logged);
    }

    protected (bool, int) SetToAction(Player player, CardData dataFile, int logged)
    {
        return SetAllStats(player, dataFile, player.resourceDict[Resource.Action], logged);
    }

    protected (bool, int) SetToControl(Player player, CardData dataFile, int logged)
    {
        int areasControlled = player.areasControlled.Count(control => control);
        return SetAllStats(player, dataFile, areasControlled, logged);
    }

    protected (bool, int) SetToNotControl(Player player, CardData dataFile, int logged)
    {
        int areasNotControlled = player.areasControlled.Count(control => !control);
        return SetAllStats(player, dataFile, areasNotControlled, logged);
    }

    #endregion

    #region Booleans

    protected (bool, int) ResolveBoolean(Player player, CardData dataFile, bool answer, int logged)
    {
        if (logged >= 0 && answer)
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        return (answer, 0);
    }

    protected (bool, int) HandOrMore(Player player, CardData dataFile, int logged)
    {
        return ResolveBoolean(player, dataFile, player.cardsInHand.Count >= dataFile.miscAmount, logged);
    }

    protected (bool, int) HandOrLess(Player player, CardData dataFile, int logged)
    {
        return ResolveBoolean(player, dataFile, player.cardsInHand.Count <= dataFile.miscAmount, logged);
    }

    protected (bool, int) CoinOrMore(Player player, CardData dataFile, int logged)
    {
        return ResolveBoolean(player, dataFile, player.resourceDict[Resource.Coin] >= dataFile.miscAmount, logged);
    }

    protected (bool, int) CoinOrLess(Player player, CardData dataFile, int logged)
    {
        return ResolveBoolean(player, dataFile, player.resourceDict[Resource.Coin] <= dataFile.miscAmount, logged);
    }

    protected (bool, int) ActionOrMore(Player player, CardData dataFile, int logged)
    {
        return ResolveBoolean(player, dataFile, player.resourceDict[Resource.Action] >= dataFile.miscAmount, logged);
    }

    protected (bool, int) ActionOrLess(Player player, CardData dataFile, int logged)
    {
        return ResolveBoolean(player, dataFile, player.resourceDict[Resource.Action] <= dataFile.miscAmount, logged);
    }

    #endregion

    #region Play

    virtual protected List<int> SimulatePlay(Player player)
    {
        List<int> sortedCards = new() { -1 };
        for (int i = 0; i < player.cardsInHand.Count; i++)
        {
            Card card = player.cardsInHand[i];
            player.cardsInHand.RemoveAt(i);
            PlayerCardData data = (PlayerCardData)card.GetFile();
            player.resourceDict[Resource.Coin] += data.startingCoin;

            if (card.DoMath(player) >= 6)
                sortedCards.Add(i + 100);

            player.cardsInHand.Insert(i, card);
            player.resourceDict[Resource.Coin] -= data.startingCoin;
        }

        return sortedCards;
    }

    protected (bool, int) PlayCard(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count == 0)
        {
            if (logged >= 0)
            {
                Log.inst.AddTextRPC(player, $"{player.name} can't play anything.", LogAdd.Personal, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
            return (true, 0);
        }
        else
        {
            List<int> sortedCards = SimulatePlay(player);
            if (logged >= 0)
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePlay(player, dataFile, sortedCards, logged));
            return (true, sortedCards.Max() - 6);
        }
    }

    void ChoosePlay(Player player, CardData dataFile, List<int> sortedCards, int logged)
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
                Log.inst.AddTextRPC(player, $"{player.name} plays {toPlay.name}.", LogAdd.Remember, logged);

                PostPlaying(player, toPlay, dataFile, logged);
                player.DiscardPlayerCard(toPlay, -1);
                toPlay.ResolveCard(player, logged + 1);
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't play a card.", LogAdd.Personal, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
        }
    }

    protected virtual void PostPlaying(Player player, PlayerCard cardToPlay, CardData dataFile, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    #endregion

    #region Discard

    List<(int, int)> SortToDiscard(Player player)
    {
        List<Card> possibleCards = new();
        possibleCards.AddRange(player.cardsInHand);
        possibleCards.Remove(this);
        return possibleCards.Select((card, index) => (card.DoMath(player), index + 100)).
            OrderByDescending(tuple => tuple.Item1).ToList();
    }

    protected (bool, int) DiscardCard(Player player, CardData dataFile, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        List<(int, int)> sortedCards = SortToDiscard(player);
        int maxDiscard = Mathf.Min(dataFile.cardAmount, sortedCards.Count);

        if (logged >= 0)
        {
            if (player.cardsInHand.Count <= dataFile.cardAmount)
                DiscardAll(player, dataFile, logged);
            else
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, sortedCards, false, logged));
        }

        int valueLost = 0;
        for (int i = 0; i < maxDiscard; i++)
            valueLost += (-1 * sortedCards[i].Item1) + 3;
        return (true, valueLost);
    }

    protected (bool, int) AskDiscardCard(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        List<(int, int)> sortedCards = SortToDiscard(player);
        bool answer = sortedCards.Count >= dataFile.cardAmount;

        if (answer && logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, sortedCards, true, logged));
        }

        int valueLost = 0;
        if (answer)
        {
            for (int i = 0; i < dataFile.cardAmount; i++)
                valueLost += (-1 * sortedCards[i].Item1) + 3;
        }
        return (answer, valueLost);
    }

    void DiscardAll(Player player, CardData dataFile, int logged)
    {
        int toDiscard = player.cardsInHand.Count;
        for (int i = 0; i < toDiscard; i++)
            player.DiscardPlayerCard(player.cardsInHand[0], logged);
        PostDiscarding(player, true, dataFile, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    void ChooseDiscard(Player player, CardData dataFile, List<(int, int)> sorted, bool optional, int logged)
    {
        string parathentical = (dataFile.cardAmount == 1) ? "" : $" ({sideCounter + 1}/{dataFile.cardAmount})";
        List<string> actions = new();
        if (optional) actions.Add("Don't Discard");

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, new() { sorted[0].Item2 });
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Discard a card to {this.name}{parathentical}.", Next);
                player.ChooseCardOnScreen(player.cardsInHand, "", null);
            }
            else
            {
                player.ChooseCardOnScreen(player.cardsInHand, $"Discard a card to {this.name}{parathentical}.", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice < player.cardsInHand.Count && convertedChoice >= 0)
            {
                Card toDiscard = player.cardsInHand[convertedChoice];
                player.DiscardPlayerCard(toDiscard, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => ChangeSideCount(false, 1));
                PostDiscarding(player, true, dataFile, logged);

                List<(int, int)> sortedCards = SortToDiscard(player);
                if (sideCounter == dataFile.cardAmount)
                    Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
                else
                    Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, sorted, false, logged));
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't discard to {this.name}.", LogAdd.Personal, logged);
                PostDiscarding(player, false, dataFile, logged);

                if (!mayStopEarly)
                    Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
        }
    }

    protected virtual void PostDiscarding(Player player, bool success, CardData dataFile, int logged)
    {
    }

    #endregion

    #region Advance

    protected virtual (int, List<int>) CanAdvance(Player player)
    {
        int total = 0;
        List<int> canAdvance = new();
        for (int i = 0; i < 3; i++)
        {
            (int troop, int scout) = player.CalcTroopScout(i);
            if (troop > 0)
            {
                total += troop;
                canAdvance.Add(i);
            }
        }
        return (total, canAdvance);
    }

    protected (bool, int) AdvanceTroop(Player player, CardData dataFile, int logged)
    {
        (int total, List<int> canAdvance) = CanAdvance(player);
        int maxAdvance = Mathf.Min(dataFile.troopAmount, total);

        if (logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, dataFile, canAdvance, false, logged));
        }
        return (true, maxAdvance);
    }

    protected (bool, int) AskAdvance(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canAdvance) = CanAdvance(player);
        bool answer = total >= dataFile.troopAmount;

        if (answer && logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, dataFile, canAdvance, true, logged));
        }
        return (answer, 4 * dataFile.troopAmount);
    }

    void ChooseAdvanceOne(Player player, CardData dataFile, List<int> canAdvance, bool optional, int logged)
    {
        string parathentical = (dataFile.troopAmount == 1) ? "" : $" ({sideCounter + 1}/{dataFile.troopAmount})";
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Advance");
            canAdvance.Insert(0, -1);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(canAdvance, optional));
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Advance a Troop with {this.name}{parathentical}.", Next);
                player.ChooseTroopDisplay(canAdvance, "", null);
            }
            else
            {
                player.ChooseTroopDisplay(canAdvance, $"Advance a Troop with {this.name}{parathentical}.", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
            {
                Log.inst.AddTextRPC(player, $"{player.name} chooses Troop in Area {convertedChoice + 1}.", LogAdd.Personal, logged);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceTwo(player, dataFile, convertedChoice, logged));
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't advance any Troop with {this.name}.", LogAdd.Personal, logged);
                PostAdvance(player, false, dataFile, logged);

                if (!mayStopEarly)
                    Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
        }
    }

    void ChooseAdvanceTwo(Player player, CardData dataFile, int chosenArea, int logged)
    {
        List<int> newPositions = new();
        if (chosenArea == 0)
        {
            newPositions.Add(1);
            newPositions.Add(2);
        }
        else
        {
            newPositions.Add(3);
        }

        if (player.myType == PlayerType.Bot)
            player.AIDecision(Resolve, player.ConvertToHundred(newPositions, false));
        else
            player.ChooseTroopDisplay(newPositions, $"Advance troop from Area {chosenArea + 1} to where?", Resolve);

        void Resolve()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
                player.MoveTroopRPC(chosenArea, convertedChoice, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => ChangeSideCount(false, 1));

            if (sideCounter == dataFile.troopAmount)
            {
                PostAdvance(player, true, dataFile, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
            else
            {
                (int total, List<int> canAdvance) = CanAdvance(player);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, dataFile, canAdvance, false, logged));
            }
        }
    }

    protected virtual void PostAdvance(Player player, bool success, CardData dataFile, int logged)
    {
    }

    #endregion

    #region Retreat

    protected virtual (int, List<int>) CanRetreat(Player player)
    {
        int total = 0;
        List<int> canRetreat = new();
        for (int i = 1; i < 4; i++)
        {
            (int troop, int scout) = player.CalcTroopScout(i);
            if (troop > 0)
            {
                total += troop;
                canRetreat.Add(i);
            }
        }
        return (total, canRetreat);
    }

    protected (bool, int) RetreatTroop(Player player, CardData dataFile, int logged)
    {
        (int total, List<int> canRetreat) = CanRetreat(player);
        int maxRetreat = Mathf.Min(dataFile.troopAmount, total);

        if (logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, dataFile, canRetreat, false, logged));
        }
        return (true, maxRetreat);
    }

    protected (bool, int) AskRetreat(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canRetreat) = CanRetreat(player);
        bool answer = total >= dataFile.troopAmount;

        if (answer && logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, dataFile, canRetreat, true, logged));
        }
        return (answer, -4 * dataFile.troopAmount);
    }

    void ChooseRetreatOne(Player player, CardData dataFile, List<int> canRetreat, bool optional, int logged)
    {
        string parathentical = (dataFile.troopAmount == 1) ? "" : $" ({sideCounter + 1}/{dataFile.troopAmount})";
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Retreat");
            canRetreat.Insert(0, -1);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(canRetreat, optional));
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Retreat a troop with {this.name}{parathentical}.", Next);
                player.ChooseTroopDisplay(canRetreat, "", null);
            }
            else
            {
                player.ChooseTroopDisplay(canRetreat, $"Retreat a troop with {this.name}{parathentical}.", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
            {
                Log.inst.AddTextRPC(player, $"{player.name} chooses Troop in Area {convertedChoice + 1}.", LogAdd.Personal, logged);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatTwo(player, dataFile, convertedChoice, logged));
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't retreat any Troop with {this.name}.", LogAdd.Personal, logged);
                PostRetreat(player, false, dataFile, logged);

                if (!mayStopEarly)
                    Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
        }
    }

    void ChooseRetreatTwo(Player player, CardData dataFile, int chosenTroop, int logged)
    {
        List<int> newPositions = new();
        if (chosenTroop == 3)
        {
            newPositions.Add(1);
            newPositions.Add(2);
        }
        else
        {
            newPositions.Add(0);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Resolve, newPositions);
        }
        else
        {
            player.ChooseTroopDisplay(newPositions, "Where to retreat this troop?", Resolve);
        }

        void Resolve()
        {
            player.MoveTroopRPC(chosenTroop, player.choice, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => ChangeSideCount(false, 1));

            if (sideCounter == dataFile.troopAmount)
            {
                PostRetreat(player, true, dataFile, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
            else
            {
                (int total, List<int> canRetreat) = CanRetreat(player);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, dataFile, canRetreat, false, logged));
            }
        }
    }

    protected virtual void PostRetreat(Player player, bool success, CardData dataFile, int logged)
    {
    }

    #endregion

    #region +Scout

    protected virtual List<int> CanAdd(Player player)
    {
        return new() { 0, 1, 2, 3 };
    }

    protected (bool, int) AddScout(Player player, CardData dataFile, int logged)
    {
        List<int> canAdd = CanAdd(player);
        if (logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAddScout(player, dataFile, canAdd, logged));
        }
        return (true, 2 * dataFile.scoutAmount);
    }

    void ChooseAddScout(Player player, CardData dataFile, List<int> canAdd, int logged)
    {
        string parathentical = (dataFile.scoutAmount == 1) ? "" : $" ({sideCounter + 1}/{dataFile.scoutAmount})";

        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, player.ConvertToHundred(canAdd, false));
        else
            player.ChooseTroopDisplay(canAdd, "Add a Scout to an Area.", Next);

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
                player.ChangeScoutRPC(convertedChoice, 1, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => ChangeSideCount(false, 1));

            if (sideCounter == dataFile.scoutAmount)
            {
                PostAddScout(player, dataFile, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
            else
            {
                List<int> canAdd = CanAdd(player);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAddScout(player, dataFile, canAdd, logged));
            }
        }
    }

    protected virtual void PostAddScout(Player player, CardData dataFile, int logged)
    {
    }

    #endregion

    #region -Scout

    protected virtual (int, List<int>) CanLose(Player player)
    {
        int total = 0;
        List<int> canLose = new();
        for (int i = 0; i < 4; i++)
        {
            (int troop, int scout) = player.CalcTroopScout(i);
            if (scout > 0)
            {
                total += scout;
                canLose.Add(i);
            }
        }
        return (total, canLose);
    }

    protected (bool, int) LoseScout(Player player, CardData dataFile, int logged)
    {
        (int total, List<int> canLose) = CanLose(player);
        if (logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, dataFile, canLose, false, logged));
        }
        return (true, -2 * Mathf.Min(total, dataFile.scoutAmount));
    }

    protected (bool, int) AskLoseScout(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canLose) = CanLose(player);
        bool answer = total >= dataFile.scoutAmount;

        if (answer && logged >= 0)
        {
            Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, dataFile, canLose, true, logged));
        }
        return (answer, -2 * dataFile.scoutAmount);
    }

    void ChooseLoseScout(Player player, CardData dataFile, List<int> canLose, bool optional, int logged)
    {
        string parathentical = (dataFile.scoutAmount == 1) ? "" : $" ({sideCounter + 1}/{dataFile.scoutAmount})";
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Lose");
            canLose.Insert(0, -1);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(canLose, optional));
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Lose a Scout with {this.name}{parathentical}.", Next);
                player.ChooseTroopDisplay(canLose, "", null);
            }
            else
            {
                if (canLose.Count <= 1)
                    Log.inst.undoToThis = null;
                player.ChooseTroopDisplay(canLose, $"Lose a Scout with {this.name}{parathentical}.", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
            {
                player.ChangeScoutRPC(convertedChoice, -1, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => ChangeSideCount(false, 1));

                if (sideCounter == dataFile.scoutAmount)
                {
                    PostLoseScout(player, true, dataFile, logged);
                    Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
                }
                else
                {
                    (int total, List<int> canLose) = CanLose(player);
                    Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, dataFile, canLose, false, logged));
                }
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't lose any Scout with {this.name}.", LogAdd.Personal, logged);
                PostLoseScout(player, false, dataFile, logged);

                if (!mayStopEarly)
                    Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
        }
    }

    protected virtual void PostLoseScout(Player player, bool success, CardData dataFile, int logged)
    {
    }

    #endregion

    #region Ask Pay

    protected (bool, int) AskLoseCoin(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        bool answer = player.resourceDict[Resource.Coin] >= dataFile.coinAmount;
        if (logged >= 0 && answer)
        {
            Action action = () => LoseCoin(player, dataFile, logged);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Pay {dataFile.coinAmount} Coin to {this.name}?", dataFile, logged));
        }
        return (answer, dataFile.coinAmount * -1);
    }

    protected (bool, int) AskLoseAction(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        bool answer = player.resourceDict[Resource.Action] >= dataFile.actionAmount;
        if (logged >= 0 && answer)
        {
            Action action = () => LoseAction(player, dataFile, logged);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Pay {dataFile.actionAmount} Action to {this.name}?", dataFile, logged));
        }
        return (answer, dataFile.actionAmount * -3);
    }

    protected void ChoosePay(Player player, Action ifDone, string text, CardData dataFile, int logged)
    {
        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, new List<int> { 0, 1 });
        else
            player.ChooseButton(new() { "Yes", "No" }, new(0, 250), text, Next);

        void Next()
        {
            if (player.choice == 0)
                ifDone();
            else
                Log.inst.AddTextRPC(player, $"{player.name} doesn't use {this.name}.", LogAdd.Personal, logged);
        }
    }

    #endregion

    #endregion

}