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
    bool mayStopEarly;

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

    protected void DoNextStep(bool undo, Player player, CardData dataFile, int logged)
    {
        if (dataFile.useSheets)
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

    protected void DrawCard(Player player, CardData dataFile, int logged)
    {
        player.DrawCardRPC(dataFile.cardAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void AddCoin(Player player, CardData dataFile, int logged)
    {
        player.ResourceRPC(Resource.Coin, dataFile.coinAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void LoseCoin(Player player, CardData dataFile, int logged)
    {
        player.ResourceRPC(Resource.Coin, -1 * dataFile.coinAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void AddPlay(Player player, CardData dataFile, int logged)
    {
        player.ResourceRPC(Resource.Play, dataFile.playAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void LosePlay(Player player, CardData dataFile, int logged)
    {
        player.ResourceRPC(Resource.Play, -1 * dataFile.playAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    #endregion

    #region Setters

    protected void SetAllStats(int number, CardData dataFile)
    {
        float multiplier = (dataFile.miscAmount > 0) ? dataFile.miscAmount : -1f / dataFile.miscAmount;
        dataFile.cardAmount = (int)Mathf.Floor(number * multiplier);
        dataFile.coinAmount = (int)Mathf.Floor(number * multiplier);
        dataFile.scoutAmount = (int)Mathf.Floor(number * multiplier);
        dataFile.playAmount = (int)Mathf.Floor(number * multiplier);
        dataFile.troopAmount = (int)Mathf.Floor(number * multiplier);
    }

    protected void SetToHand(Player player, CardData dataFile, int logged)
    {
        SetAllStats(player.cardsInHand.Count, dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void SetToCoin(Player player, CardData dataFile, int logged)
    {
        SetAllStats(player.resourceDict[Resource.Coin], dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void SetToControl(Player player, CardData dataFile, int logged)
    {
        int areasControlled = player.areasControlled.Count(control => control);
        SetAllStats(areasControlled, dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void SetToNotControl(Player player, CardData dataFile, int logged)
    {
        int areasNotControlled = player.areasControlled.Count(control => !control);
        SetAllStats(areasNotControlled, dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    #endregion

    #region Booleans

    protected void HandOrMore(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count >= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void HandOrLess(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count <= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void CoinOrMore(Player player, CardData dataFile, int logged)
    {
        if (player.resourceDict[Resource.Coin] >= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    protected void CoinOrLess(Player player, CardData dataFile, int logged)
    {
        if (player.resourceDict[Resource.Coin] <= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    #endregion

    #region Play

    protected void PlayCard(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count == 0)
        {
            Log.inst.AddTextRPC(player, $"{player.name} can't play anything.", LogAdd.Personal, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
        }
        else
        {
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePlay(player, dataFile, logged));
        }
    }

    void ChoosePlay(Player player, CardData dataFile, int logged)
    {
        List<string> actions = new() { $"Don't Play" };
        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(player.cardsInHand, true));
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
                PlayerCard toPlay = (PlayerCard) player.cardsInHand[convertedChoice];
                Log.inst.AddTextRPC(player, $"{player.name} plays {toPlay.name}.", LogAdd.Remember, logged);

                PostPlaying(player, toPlay, dataFile, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));

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
    }

    #endregion

    #region Discard

    protected void DiscardCard(Player player, CardData dataFile, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        if (player.cardsInHand.Count <= dataFile.cardAmount)
            DiscardAll(player, dataFile, logged);
        else
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, false, logged));
    }

    protected void AskDiscard(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        if (player.cardsInHand.Count < dataFile.cardAmount)
            return;

        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, true, logged));
    }

    void DiscardAll(Player player, CardData dataFile, int logged)
    {
        for (int i = 0; i < player.cardsInHand.Count; i++)
            player.DiscardPlayerCard(player.cardsInHand[0], logged);
        PostDiscarding(player, true, dataFile, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
    }

    void ChooseDiscard(Player player, CardData dataFile, bool optional, int logged)
    {
        string parathentical = (dataFile.cardAmount == 1) ? "" : $" ({sideCounter+1}/{dataFile.cardAmount})";
        List<string> actions = new();
        if (optional) actions.Add("Don't Discard");

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToHundred(player.cardsInHand, optional));
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
                if (player.cardsInHand.Count == 1)
                    Log.inst.undoToThis = null;
                player.ChooseCardOnScreen(player.cardsInHand, "", Next);
            }
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice < player.cardsInHand.Count && convertedChoice >= 0)
            {
                Card toPlay = player.cardsInHand[convertedChoice];
                player.DiscardPlayerCard(toPlay, logged);
                Log.inst.RememberStep(this, StepType.Revert, () => ChangeSideCount(false, 1));
                PostDiscarding(player, true, dataFile, logged);

                if (sideCounter == dataFile.cardAmount)
                {
                    PostDiscarding(player, true, dataFile, logged);
                    Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
                }
                else
                {
                    Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, false, logged));
                }
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

    protected void AdvanceTroop(Player player, CardData dataFile, int logged)
    {
        (int total, List<int> canAdvance) = CanAdvance(player);
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, dataFile, canAdvance, false, logged));
    }

    protected void AskAdvance(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canAdvance) = CanAdvance(player);
        if (total < dataFile.troopAmount)
            return;
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAdvanceOne(player, dataFile, canAdvance, true, logged));
    }

    void ChooseAdvanceOne(Player player, CardData dataFile, List<int> canAdvance, bool optional, int logged)
    {
        string parathentical = (dataFile.troopAmount == 1) ? "" : $" ({sideCounter+1}/{dataFile.troopAmount})";
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
                player.ChooseButton(actions, new(0, 250), $"Advance a troop with {this.name}{parathentical}.", Next);
                player.ChooseTroopDisplay(canAdvance, "", null);
            }
            else
            {
                if (canAdvance.Count <= 1)
                    Log.inst.undoToThis = null;
                player.ChooseTroopDisplay(canAdvance, $"Advance a troop with {this.name}{parathentical}.", Next);
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

    void ChooseAdvanceTwo(Player player, CardData dataFile, int chosenTroop, int logged)
    {
        List<int> newPositions = new();
        if (chosenTroop == 0)
        {
            newPositions.Add(1);
            newPositions.Add(2);
        }
        else
        {
            Log.inst.undoToThis = null;
            newPositions.Add(3);
        }

        if (player.myType == PlayerType.Bot)
            player.AIDecision(Resolve, player.ConvertToHundred(newPositions, false));
        else
            player.ChooseTroopDisplay(newPositions, "Where to advance this troop?", Resolve);

        void Resolve()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice >= 0)
                player.MoveTroopRPC(chosenTroop, convertedChoice, logged);
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

    protected void RetreatTroop(Player player, CardData dataFile, int logged)
    {
        (int total, List<int> canRetreat) = CanRetreat(player);
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, dataFile, canRetreat, false, logged));
    }

    protected void AskRetreat(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canRetreat) = CanRetreat(player);
        if (total < dataFile.troopAmount)
            return;
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseRetreatOne(player, dataFile, canRetreat, true, logged));
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
                if (canRetreat.Count <= 1)
                    Log.inst.undoToThis = null;
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
            if (newPositions.Count == 1)
                Log.inst.undoToThis = null;
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
        return new() { 0, 1, 2, 3};
    }

    protected void AddScout(Player player, CardData dataFile, int logged)
    {
        List<int> canAdd = CanAdd(player);
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseAddScout(player, dataFile, canAdd, logged));
    }

    void ChooseAddScout(Player player, CardData dataFile, List<int> canAdd, int logged)
    {
        string parathentical = (dataFile.scoutAmount == 1) ? "" : $" ({sideCounter + 1}/{dataFile.scoutAmount})";

        if (player.myType == PlayerType.Bot)
            player.AIDecision(Next, player.ConvertToHundred(canAdd, false));
        else
            player.ChooseTroopDisplay(canAdd, "Add a scout to an Area.", Next);

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

    protected void LoseScout(Player player, CardData dataFile, int logged)
    {
        (int total, List<int> canLose) = CanLose(player);
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, dataFile, canLose, false, logged));
    }

    protected void AskLoseScout(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        (int total, List<int> canLose) = CanLose(player);
        if (total < dataFile.scoutAmount)
            return;
        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseLoseScout(player, dataFile, canLose, true, logged));
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
                player.ChooseButton(actions, new(0, 250), $"Lose a scout with {this.name}{parathentical}.", Next);
                player.ChooseTroopDisplay(canLose, "", null);
            }
            else
            {
                if (canLose.Count <= 1)
                    Log.inst.undoToThis = null;
                player.ChooseTroopDisplay(canLose, $"Lose a scout with {this.name}{parathentical}.", Next);
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

    protected void AskLoseCoin(Player player, CardData dataFile, int logged)
    {
        if (player.resourceDict[Resource.Coin] < dataFile.coinAmount)
            return;

        Action action = () => LoseCoin(player, dataFile, logged);
        if (dataFile.coinAmount == 0)
            action();
        else
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Pay {dataFile.coinAmount} Coin to {this.name}?", dataFile, logged));
    }

    protected void AskLosePlay(Player player, CardData dataFile, int logged)
    {
        if (player.resourceDict[Resource.Play] < dataFile.playAmount)
            return;

        Action action = () => LosePlay(player, dataFile, logged);
        if (dataFile.playAmount == 0)
            action();
        else
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Pay {dataFile.playAmount} Play to {this.name}?", dataFile, logged));
    }

    void ChoosePay(Player player, Action ifDone, string text, CardData dataFile, int logged)
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
                Log.inst.RememberStep(this, StepType.Revert, () => DoNextStep(false, player, dataFile, logged));
            }
            else
            {
                Log.inst.AddTextRPC(player, $"{player.name} doesn't use {this.name}.", LogAdd.Personal, logged);
            }
        }
    }

    #endregion

    #endregion

}
