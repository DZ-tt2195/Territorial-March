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
    protected bool mayStopEarly;

    public bool recalculate;
    public int mathResult { get; protected set; }

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

    public virtual void DoMath(Player player)
    {
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

    protected void NextStepRPC(Player player, int logged)
    {
        if (GetFile().useSheets && logged >= 0)
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player.playerPosition, logged));
    }

    [PunRPC]
    void DoNextStep(bool undo, int playerPosition, int logged)
    {
        if (undo)
        {
            stepCounter--;
        }
        else
        {
            stepCounter++;
            if (stepCounter < activationSteps.Count)
                StringParameters(activationSteps[stepCounter], new object[2]
                { Manager.inst.playersInOrder[playerPosition], logged });
        }
    }

    #endregion

    #region +/- Resources

    protected (bool, int) DrawCard(Player player, int logged)
    {
        if (logged >= 0)
        {
            player.DrawCardRPC(GetFile().cardAmount, logged);
            NextStepRPC(player, logged);
        }
        return (true, GetFile().cardAmount * 3);
    }

    protected (bool, int) AddCoin(Player player, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Coin, GetFile().coinAmount, logged);
            NextStepRPC(player, logged);
        }
        return (true, GetFile().coinAmount);
    }

    protected (bool, int) LoseCoin(Player player, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Coin, -1 * GetFile().coinAmount, logged);
            NextStepRPC(player, logged);
        }
        return (true, -1 * GetFile().coinAmount);
    }

    protected (bool, int) AddAction(Player player, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Action, GetFile().actionAmount, logged);
            NextStepRPC(player, logged);
        }
        return (true, GetFile().actionAmount * 3);
    }

    protected (bool, int) LoseAction(Player player, int logged)
    {
        if (logged >= 0)
        {
            player.ResourceRPC(Resource.Action, -1 * GetFile().actionAmount, logged);
            NextStepRPC(player, logged);
        }
        return (true, GetFile().actionAmount * -3);
    }

    #endregion

    #region Setters

    protected (bool, int) SetAllStats(Player player, CardData dataFile, int number, int logged)
    {
        float multiplier = (dataFile.miscAmount >= 0) ? dataFile.miscAmount : -1f / dataFile.miscAmount;
        int calculated = (number > 0) ? (int)Mathf.Floor(number * multiplier) : 0;

        dataFile.cardAmount = calculated;
        dataFile.coinAmount = calculated;
        dataFile.scoutAmount = calculated;
        dataFile.actionAmount = calculated;
        dataFile.troopAmount = calculated;

        if (logged >= 0)
        {
            NextStepRPC(player, logged);
            return (true, 0);
        }
        else
        {
            return (true, 0);
        }
    }

    protected (bool, int) SetToHand(Player player, int logged)
    {
        return SetAllStats(player, GetFile(), player.cardsInHand.Count, logged);
    }

    protected (bool, int) SetToCoin(Player player, int logged)
    {
        return SetAllStats(player, GetFile(), player.resourceDict[Resource.Coin], logged);
    }

    protected (bool, int) SetToAction(Player player, int logged)
    {
        return SetAllStats(player, GetFile(), player.resourceDict[Resource.Action], logged);
    }

    protected (bool, int) SetToControl(Player player, int logged)
    {
        int areasControlled = player.areasControlled.Count(control => control);
        return SetAllStats(player, GetFile(), areasControlled, logged);
    }

    protected (bool, int) SetToNotControl(Player player, int logged)
    {
        int areasNotControlled = player.areasControlled.Count(control => !control);
        return SetAllStats(player, GetFile(), areasNotControlled, logged);
    }

    #endregion

    #region Booleans

    protected (bool, int) ResolveBoolean(Player player, bool answer, int logged)
    {
        if (logged >= 0 && answer)
            NextStepRPC(player, logged);
        return (answer, 0);
    }

    protected (bool, int) HandOrMore(Player player, int logged)
    {
        return ResolveBoolean(player, player.cardsInHand.Count >= GetFile().miscAmount, logged);
    }

    protected (bool, int) HandOrLess(Player player, int logged)
    {
        return ResolveBoolean(player, player.cardsInHand.Count <= GetFile().miscAmount, logged);
    }

    protected (bool, int) CoinOrMore(Player player, int logged)
    {
        return ResolveBoolean(player, player.resourceDict[Resource.Coin] >= GetFile().miscAmount, logged);
    }

    protected (bool, int) CoinOrLess(Player player, int logged)
    {
        return ResolveBoolean(player, player.resourceDict[Resource.Coin] <= GetFile().miscAmount, logged);
    }

    protected (bool, int) ActionOrMore(Player player, int logged)
    {
        return ResolveBoolean(player, player.resourceDict[Resource.Action] >= GetFile().miscAmount, logged);
    }

    protected (bool, int) ControlOrMore(Player player, int logged)
    {
        int areasControlled = player.areasControlled.Count(control => control);
        return ResolveBoolean(player, areasControlled >= GetFile().miscAmount, logged);
    }

    protected (bool, int) ControlOrLess(Player player, int logged)
    {
        int areasControlled = player.areasControlled.Count(control => control);
        return ResolveBoolean(player, areasControlled <= GetFile().miscAmount, logged);
    }

    #endregion

    #region Discard

    List<(int, int)> SortToDiscard(Player player, int logged)
    {
        List<Card> possibleCards = new();
        possibleCards.AddRange(player.cardsInHand);
        possibleCards.Remove(this);

        if (logged >= 0)
        {
            for (int i = 0; i < player.cardsInHand.Count; i++)
                player.cardsInHand[i].recalculate = true;

            for (int i = 0; i < player.cardsInHand.Count; i++)
                player.cardsInHand[i].DoMath(player);
            
            return possibleCards.Select((card, index) => (card.mathResult, index + 100)).
                OrderByDescending(tuple => tuple.mathResult).ToList();
        }
        else
        {
            List<(int, int)> newList = new();
            foreach (Card card in possibleCards)
                newList.Add((0, 0));
            return newList;
        }
    }

    protected (bool, int) DiscardCard(Player player, int logged)
    {
        List<(int, int)> sortedCards = SortToDiscard(player, logged);
        if (logged >= 0)
        {
            if (player.cardsInHand.Count <= GetFile().cardAmount)
                DiscardCardAll(player, logged);
            else if (GetFile().cardAmount > 0)
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, sortedCards, false, 1, logged));
        }
        return (true, -3 * Mathf.Min(GetFile().cardAmount, sortedCards.Count));
    }

    protected (bool, int) AskDiscardCard(Player player, int logged)
    {
        mayStopEarly = true;
        List<(int, int)> sortedCards = SortToDiscard(player, logged);
        bool answer = sortedCards.Count >= GetFile().cardAmount;

        if (answer && logged >= 0 && GetFile().cardAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, sortedCards, true, 1, logged));
        return (answer, -3 * GetFile().cardAmount);
    }

    protected (bool, int) DiscardCardAll(Player player, int logged)
    {
        int toDiscard = player.cardsInHand.Count;
        for (int i = 0; i < toDiscard; i++)
            player.DiscardPlayerCard(player.cardsInHand[0], logged);

        if (logged >= 0)
        {
            PostDiscard(player, true, logged);
            NextStepRPC(player, logged);
        }
        return (true, -3 * toDiscard);
    }

    void ChooseDiscard(Player player, List<(int, int)> sorted, bool optional, int counter, int logged)
    {
        string parathentical = (GetFile().cardAmount == 1) ? "" : $" ({counter}/{GetFile().cardAmount})";
        List<string> actions = new();
        if (optional) actions.Add("Don't Discard");

        if (player.myType == PlayerType.Bot)
        {
            List<int> possibilities = new();
            if (optional) possibilities.Add(-1);
            possibilities.Add(sorted[0].Item2);
            player.AIDecision(Next, possibilities);
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Discard a Card to {this.name}{parathentical}.", Next);
                player.ChooseCardOnScreen(player.cardsInHand, "", null);
            }
            else
            {
                player.ChooseCardOnScreen(player.cardsInHand, $"Discard a Card to {this.name}{parathentical}.", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice < player.cardsInHand.Count && convertedChoice >= 0)
            {
                Card toDiscard = player.cardsInHand[convertedChoice];
                player.DiscardPlayerCard(toDiscard, logged);
                PostDiscard(player, true, logged);

                List<(int, int)> sortedCards = SortToDiscard(player, logged);
                int newCounter = counter + 1;
                if (newCounter > GetFile().cardAmount)
                    NextStepRPC(player, logged);
                else
                    Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, sorted, false, newCounter, logged));
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't discard to {this.name}.", LogAdd.Personal, logged);
                PostDiscard(player, false, logged);

                if (!mayStopEarly)
                    NextStepRPC(player, logged);
            }
        }
    }

    protected virtual void PostDiscard(Player player, bool success, int logged)
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
            int troop = player.CalcTroopScout(i).Item1;
            if (troop > 0)
            {
                total += troop;
                canAdvance.Add(i);
            }
        }
        return (total, canAdvance);
    }

    protected (bool, int) AdvanceTroop(Player player, int logged)
    {
        (int total, List<int> canAdvance) = CanAdvance(player);
        if (logged >= 0 && GetFile().troopAmount > 0 && total > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, canAdvance, false, 1, logged));
        return (true, 4 * Mathf.Min(GetFile().troopAmount, total));
    }

    protected (bool, int) AskAdvanceTroop(Player player, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canAdvance) = CanAdvance(player);
        bool answer = total >= GetFile().troopAmount;

        if (answer && logged >= 0 && GetFile().troopAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, canAdvance, true, 1, logged));
        return (answer, 4 * GetFile().troopAmount);
    }

    protected (bool, int) AdvanceTroopOne(Player player, int logged)
    {
        int troopInArea = player.CalcTroopScout(0).Item1;
        if (logged >= 0 && troopInArea > 0 && GetFile().troopAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceTwo(player, 0, false, 1, logged));
        return (true, 4 * Mathf.Min(GetFile().troopAmount, troopInArea));
    }

    protected (bool, int) AdvanceTroopTwo(Player player, int logged)
    {
        int troopInArea = player.CalcTroopScout(1).Item1;
        if (logged >= 0 && troopInArea > 0 && GetFile().troopAmount > 0)
            player.MoveTroopRPC(1, 3, logged);
        return (true, 4 * Mathf.Min(GetFile().troopAmount, troopInArea));
    }

    protected (bool, int) AdvanceTroopThree(Player player, int logged)
    {
        int troopInArea = player.CalcTroopScout(2).Item1;
        if (logged >= 0 && troopInArea > 0 && GetFile().troopAmount > 0)
            player.MoveTroopRPC(2, 3, logged);
        return (true, 4 * Mathf.Min(GetFile().troopAmount, troopInArea));
    }

    protected void ChooseAdvanceOne(Player player, List<int> canAdvance, bool optional, int counter, int logged)
    {
        string parathentical = (GetFile().troopAmount == 1) ? "" : $" ({counter}/{GetFile().troopAmount})";
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Advance");
            canAdvance.Insert(0, -1);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(canAdvance));
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
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceTwo(player, convertedChoice, false, counter, logged));
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't advance any Troop with {this.name}.", LogAdd.Personal, logged);
                PostAdvance(player, false, logged);

                if (!mayStopEarly)
                    NextStepRPC(player, logged);
            }
        }
    }

    protected void ChooseAdvanceTwo(Player player, int chosenTroop, bool optional, int counter, int logged)
    {
        List<int> newPositions = new();
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Retreat");
            newPositions.Insert(0, -1);
        }
        if (chosenTroop == 0)
        {
            newPositions.Add(1);
            newPositions.Add(2);
        }
        else
        {
            newPositions.Add(3);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Resolve, player.ConvertToHundred(newPositions));
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Where to advance Troop from Area {chosenTroop + 1}?", Resolve);
                player.ChooseTroopDisplay(newPositions, "", null);
            }
            else
            {
                player.ChooseTroopDisplay(newPositions, $"Where to advance Troop from Area {chosenTroop + 1}?", Resolve);
            }
        }

        void Resolve()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
            {
                player.MoveTroopRPC(chosenTroop, convertedChoice, logged);

                int newCounter = counter + 1;
                if (newCounter > GetFile().troopAmount)
                {
                    PostAdvance(player, true, logged);
                    NextStepRPC(player, logged);
                }
                else
                {
                    (int total, List<int> canAdvance) = CanAdvance(player);
                    if (total > 0)
                        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, canAdvance, false, newCounter, logged));
                }
            }
        }
    }

    protected virtual void PostAdvance(Player player, bool success, int logged)
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
            int troop = player.CalcTroopScout(i).Item1;
            if (troop > 0)
            {
                total += troop;
                canRetreat.Add(i);
            }
        }
        return (total, canRetreat);
    }

    protected (bool, int) RetreatTroop(Player player, int logged)
    {
        (int total, List<int> canRetreat) = CanRetreat(player);
        int maxRetreat = Mathf.Min(GetFile().troopAmount, total);

        if (logged >= 0 && GetFile().troopAmount > 0 && total > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, canRetreat, false, 1, logged));
        return (true, -4 * Mathf.Min(GetFile().troopAmount, total));
    }

    protected (bool, int) AskRetreatTroop(Player player, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canRetreat) = CanRetreat(player);
        bool answer = total >= GetFile().troopAmount;

        if (answer && logged >= 0 && GetFile().troopAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, canRetreat, true, 1, logged));
        return (answer, -4 * GetFile().troopAmount);
    }

    protected (bool, int) RetreatTroopTwo(Player player, int logged)
    {
        int troopInArea = player.CalcTroopScout(1).Item1;
        if (logged >= 0 && troopInArea > 0 && GetFile().troopAmount > 0)
            player.MoveTroopRPC(1, 0, logged);
        return (true, -4 * Mathf.Min(GetFile().troopAmount, troopInArea));
    }

    protected (bool, int) RetreatTroopThree(Player player, int logged)
    {
        int troopInArea = player.CalcTroopScout(2).Item1;
        if (logged >= 0 && troopInArea > 0 && GetFile().troopAmount > 0)
            player.MoveTroopRPC(2, 0, logged);
        return (true, -4 * Mathf.Min(GetFile().troopAmount, troopInArea));
    }

    protected (bool, int) RetreatTroopFour(Player player, int logged)
    {
        int troopInArea = player.CalcTroopScout(3).Item1;
        if (logged >= 0 && troopInArea > 0 && GetFile().troopAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatTwo(player, 3, false, 1, logged));
        return (true, -4 * Mathf.Min(GetFile().troopAmount, troopInArea));
    }

    protected (bool, int) AskRetreatTroopFour(Player player, int logged)
    {
        mayStopEarly = true;
        int troopInArea = player.CalcTroopScout(3).Item1;
        if (logged >= 0 && troopInArea > 0 && GetFile().troopAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatTwo(player, 3, true, 1, logged));
        return (true, -4 * Mathf.Min(GetFile().troopAmount, troopInArea));
    }

    protected void ChooseRetreatOne(Player player, List<int> canRetreat, bool optional, int counter, int logged)
    {
        string parathentical = (GetFile().troopAmount == 1) ? "" : $" ({counter}/{GetFile().troopAmount})";
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Retreat");
            canRetreat.Insert(0, -1);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(canRetreat));
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Retreat a Troop with {this.name}{parathentical}.", Next);
                player.ChooseTroopDisplay(canRetreat, "", null);
            }
            else
            {
                player.ChooseTroopDisplay(canRetreat, $"Retreat a Troop with {this.name}{parathentical}.", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
            {
                Log.inst.AddTextRPC(player, $"{player.name} chooses Troop in Area {convertedChoice + 1}.", LogAdd.Personal, logged);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatTwo(player, convertedChoice, false, counter, logged));
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't retreat any Troop with {this.name}.", LogAdd.Personal, logged);
                PostRetreat(player, false, logged);

                if (!mayStopEarly)
                    NextStepRPC(player, logged);
            }
        }
    }

    protected void ChooseRetreatTwo(Player player, int chosenTroop, bool optional, int counter, int logged)
    {
        List<int> newPositions = new();
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Retreat");
            newPositions.Insert(0, -1);
        }
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
            player.AIDecision(Next, newPositions);
        }
        else
        {
            if (optional)
            {
                player.ChooseButton(actions, new(0, 250), $"Where to retreat Troop from Area {chosenTroop+1}?", Next);
                player.ChooseTroopDisplay(newPositions, "", null);
            }
            else
            {
                player.ChooseTroopDisplay(newPositions, $"Where to retreat Troop from Area {chosenTroop + 1}?", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
            {
                player.MoveTroopRPC(chosenTroop, convertedChoice, logged);

                int newCounter = counter + 1;
                if (newCounter > GetFile().troopAmount)
                {
                    PostRetreat(player, true, logged);
                    NextStepRPC(player, logged);
                }
                else
                {
                    (int total, List<int> canRetreat) = CanRetreat(player);
                    if (total > 0)
                        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, canRetreat, false, newCounter, logged));
                }
            }
        }
    }

    protected virtual void PostRetreat(Player player, bool success, int logged)
    {
    }

    #endregion

    #region +Scout

    protected virtual List<int> CanAdd(Player player)
    {
        return new() { 0, 1, 2, 3 };
    }

    protected (bool, int) AddScout(Player player, int logged)
    {
        List<int> canAdd = CanAdd(player);
        if (logged >= 0 && GetFile().scoutAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAddScout(player, canAdd, 1, logged));
        return (true, 2 * GetFile().scoutAmount);
    }

    void ChooseAddScout(Player player, List<int> canAdd, int counter, int logged)
    {
        string parathentical = (GetFile().scoutAmount == 1) ? "" : $" ({counter}/{GetFile().scoutAmount})";

        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, player.ConvertToHundred(canAdd));
        else
            player.ChooseTroopDisplay(canAdd, "Add a Scout to an Area.", Next);

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
                player.ChangeScoutRPC(convertedChoice, 1, logged);

            int newCounter = counter + 1;
            if (newCounter > GetFile().scoutAmount)
            {
                PostAddScout(player, logged);
                NextStepRPC(player, logged);
            }
            else
            {
                List<int> canAdd = CanAdd(player);
                Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAddScout(player, canAdd, newCounter, logged));
            }
        }
    }

    protected virtual void PostAddScout(Player player, int logged)
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
            int scout = player.CalcTroopScout(i).Item2;
            if (scout > 0)
            {
                total += scout;
                canLose.Add(i);
            }
        }
        return (total, canLose);
    }    

    protected (bool, int) LoseScout(Player player, int logged)
    {
        (int total, List<int> canLose) = CanLose(player);
        if (logged >= 0 && GetFile().scoutAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, canLose, false, 1, logged));
        return (true, -2 * Mathf.Min(GetFile().scoutAmount, total));
    }

    protected (bool, int) AskLoseScout(Player player, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canLose) = CanLose(player);
        bool answer = total >= GetFile().scoutAmount;

        if (answer && logged >= 0 && GetFile().scoutAmount > 0)
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, canLose, true, 1, logged));
        return (answer, -2 * GetFile().scoutAmount);
    }

    void ChooseLoseScout(Player player, List<int> canLose, bool optional, int counter, int logged)
    {
        string parathentical = (GetFile().scoutAmount == 1) ? "" : $" ({counter}/{GetFile().scoutAmount})";
        List<string> actions = new();
        if (optional)
        {
            actions.Add("Don't Lose");
            canLose.Insert(0, -1);
        }

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(canLose));
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
                int newCounter = counter + 1;

                if (newCounter > GetFile().scoutAmount)
                {
                    PostLoseScout(player, true, logged);
                    NextStepRPC(player, logged);
                }
                else
                {
                    (int total, List<int> canLose) = CanLose(player);
                    Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, canLose, false, newCounter, logged));
                }
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't lose any Scout with {this.name}.", LogAdd.Personal, logged);
                PostLoseScout(player, false, logged);

                if (!mayStopEarly)
                    NextStepRPC(player, logged);
            }
        }
    }

    protected virtual void PostLoseScout(Player player, bool success, int logged)
    {
    }

    #endregion

    #region Ask Pay

    protected (bool, int) AskLoseCoin(Player player, int logged)
    {
        mayStopEarly = true;
        if (logged >= 0)
        {
            Action action = () => LoseCoin(player, logged);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Pay {GetFile().coinAmount} Coin to {this.name}?", logged));
        }
        return (true, GetFile().coinAmount * -1);
    }

    protected (bool, int) AskLoseAction(Player player, int logged)
    {
        mayStopEarly = true;
        if (logged >= 0)
        {
            Action action = () => LoseAction(player, logged);
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Pay {GetFile().actionAmount} Action to {this.name}?", logged));
        }
        return (true, GetFile().actionAmount * -3);
    }

    protected void ChoosePay(Player player, Action ifDone, string text, int logged)
    {
        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, new List<int> { 0, 1 });
        else
            player.ChooseButton(new() { "Yes", "No" }, new(0, 250), text, Next);

        void Next()
        {
            if (player.choice == 0)
            {
                ifDone();
                PostPayment(player, true, logged);
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't use {this.name}.", LogAdd.Personal, logged);
                PostPayment(player, false, logged);
            }
        }
    }

    protected virtual void PostPayment(Player player, bool success, int logged)
    {
    }

    #endregion

    #endregion

}