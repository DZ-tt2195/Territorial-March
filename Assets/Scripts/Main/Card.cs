using UnityEngine;
using UnityEngine.UI;
using MyBox;
using System.Collections;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using System;

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
        if (this.layout.cg.alpha == 1)
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

        this.layout.cg.alpha = 1;
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

    protected void Advance(bool undo, Player player, CardData dataFile, int logged)
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

    protected void ChangeSideCount(bool undo, int change)
    {
        if (undo)
            sideCounter -= change;
        else
            sideCounter += change;
    }

    protected void SetSideCount(bool undo, int newNumber)
    {
        ChangeSideCount(undo, newNumber - sideCounter);
    }

    #endregion

    #region +/- Resources

    protected void DrawCard(Player player, CardData dataFile, int logged)
    {
        player.DrawCardRPC(dataFile.cardAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void AddCoin(Player player, CardData dataFile, int logged)
    {
        player.ResourceRPC(Resource.Coin, dataFile.coinAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void LoseCoin(Player player, CardData dataFile, int logged)
    {
        player.ResourceRPC(Resource.Coin, -1 * dataFile.coinAmount, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
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
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void SetToCoin(Player player, CardData dataFile, int logged)
    {
        SetAllStats(player.resourceDict[Resource.Coin], dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void SetToControl(Player player, CardData dataFile, int logged)
    {
        int areasControlled = player.areasControlled.Count(control => control);
        SetAllStats(areasControlled, dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void SetToNotControl(Player player, CardData dataFile, int logged)
    {
        int areasNotControlled = player.areasControlled.Count(control => !control);
        SetAllStats(areasNotControlled, dataFile);
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    #endregion

    #region Booleans

    protected void HandOrMore(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count >= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void HandOrLess(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count <= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void CoinOrMore(Player player, CardData dataFile, int logged)
    {
        if (player.resourceDict[Resource.Coin] >= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void CoinOrLess(Player player, CardData dataFile, int logged)
    {
        if (player.resourceDict[Resource.Coin] <= dataFile.miscAmount)
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    #endregion

    #region Play

    protected void PlayCard(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count == 0)
        {
            Log.inst.AddTextRPC($"{player.name} can't play anything.", LogAdd.Personal, logged);
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
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
            player.AIDecision(Next, player.ConvertToCardNums(player.cardsInHand, true));
        }
        else
        {
            player.ChooseButton(actions, Vector3.zero, (player.cardsInHand.Count) == 0 ? "Can't play cards." : "What to play?", Next);
            player.ChooseCardOnScreen(player.cardsInHand, (player.cardsInHand.Count) == 0 ? "You can't play any cards." : "What to play?", null);
        }

        player.ChooseButton(new() { "Decline" }, new(0, 250), $"Choose a card to play with {this.name}.", Next);
        player.ChooseCardOnScreen(player.cardsInHand, "", null);

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice < player.cardsInHand.Count && convertedChoice >= 0)
            {
                Card toPlay = player.cardsInHand[convertedChoice];
                Log.inst.AddTextRPC($"{this.name} plays {toPlay.name}.", LogAdd.Remember, logged);
                player.DiscardPlayerCard(toPlay, -1);
                //toPlay.OnPlayEffect(this, 0);
            }
            else
            {
                Log.inst.AddTextRPC($"{player.name} doesn't play a card.", LogAdd.Remember, logged);
            }
            Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
        }
    }

    protected virtual void PostPlaying(Player player, PlayerCard cardToPlay, CardData dataFile, int logged)
    {
    }

    #endregion

    #region Discard

    protected void DiscardCard(Player player, CardData dataFile, int logged)
    {
        if (player.cardsInHand.Count <= dataFile.cardAmount)
            DiscardAll(player, dataFile, logged);
        else
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, false, logged));
    }

    void DiscardAll(Player player, CardData dataFile, int logged)
    {
        for (int i = 0; i < player.cardsInHand.Count; i++)
            player.DiscardPlayerCard(player.cardsInHand[0], logged);
        PostDiscarding(player, true, dataFile, logged);
        Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
    }

    protected void AskDiscard(Player player, CardData dataFile, int logged)
    {
        mayStopEarly = true;
        if (player.cardsInHand.Count < dataFile.cardAmount)
            return;

        Log.inst.RememberStep(this, StepType.Revert, () => SetSideCount(false, 0));
        Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, true, logged));
    }

    void ChooseDiscard(Player player, CardData dataFile, bool optional, int logged)
    {
        string parathentical = (dataFile.cardAmount == 1) ? "" : $" ({sideCounter}/{dataFile.cardAmount})";
        List<string> actions = new();
        if (optional) actions.Add("Don't Play");

        if (player.myType == PlayerType.Bot)
        {
            player.AIDecision(Next, player.ConvertToCardNums(player.cardsInHand, true));
        }
        else
        {
            if (optional)
                player.ChooseButton(new() { "Decline" }, new(0, 250), $"Discard a card to {this.name}{parathentical}.", Next);
            player.ChooseCardOnScreen(player.cardsInHand, "", null);
        }

        void Next()
        {
            int convertedChoice = player.choice - 100;
            if (convertedChoice < player.cardsInHand.Count && convertedChoice >= 0)
            {
                Card toPlay = player.cardsInHand[convertedChoice];
                player.DiscardPlayerCard(toPlay, logged);
                PostDiscarding(player, true, dataFile, logged);

                if (sideCounter == dataFile.cardAmount)
                {
                    PostDiscarding(player, true, dataFile, logged);
                    Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
                }
                else
                {
                    Log.inst.RememberStep(this, StepType.UndoPoint, () => ChooseDiscard(player, dataFile, false, logged));
                }
            }
            else
            {
                if (optional)
                    Log.inst.AddTextRPC($"{player.name} doesn't discard to {this.name}.", LogAdd.Personal, logged);
                PostDiscarding(player, false, dataFile, logged);

                if (!mayStopEarly)
                    Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
            }
        }
    }

    protected virtual void PostDiscarding(Player player, bool success, CardData dataFile, int logged)
    {
    }

    #endregion

    #region Ask Pay

    protected void AskLoseCoin(Player player, CardData dataFile, int logged)
    {
        if (player.resourceDict[Resource.Coin] < dataFile.coinAmount)
            return;

        Action action = () => AddCoin(player, dataFile, logged);
        if (dataFile.coinAmount == 0)
            action();
        else
            Log.inst.RememberStep(this, StepType.UndoPoint, () => ChoosePay(player, action, $"Pay {dataFile.coinAmount} Coin to {this.name}?", dataFile, logged));
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
                Log.inst.RememberStep(this, StepType.Revert, () => Advance(false, player, dataFile, logged));
            }
            else
            {
                Log.inst.AddTextRPC($"{player.name} doesn't use {this.name}.", LogAdd.Personal, logged);
            }
        }
    }

    #endregion

    #endregion

}
