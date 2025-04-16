using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using MyBox;
using System;

[Serializable] public class DecisionChain
{
    public bool complete = false;
    public int currentArea;
    public int tracker;
    public List<int> decisions;
    public float math = 0;
    public NextStep toThisPoint;

    public DecisionChain(NextStep toThisPoint, int currentArea)
    {
        complete = false;
        this.currentArea = currentArea;
        decisions = new();
        this.toThisPoint = toThisPoint;
        this.tracker = 0;
        Debug.Log($"NEW CHAIN: {toThisPoint.source.name}: {toThisPoint.actionName} - {tracker} -> {CarryVariables.inst.PrintIntList(decisions)}");
    }

    public DecisionChain(NextStep toThisPoint, int currentArea, List<int> oldList, int toAdd)
    {
        complete = false;
        this.currentArea = currentArea;
        decisions = new(oldList) {toAdd};
        this.toThisPoint = toThisPoint;
        this.tracker = decisions.Count - 1;
        Debug.Log($"NEW CHAIN: {toThisPoint.source.name}: {toThisPoint.actionName} - {tracker} -> {CarryVariables.inst.PrintIntList(decisions)}");
    }
}

public enum PlayerType { Human, Bot }
public enum Resource { Coin, Action }

public class Player : PhotonCompatible
{

#region Variables

    [Foldout("Player info", true)]
    public int playerPosition { get; private set; }
    public PlayerType myType { get; private set; }
    public Photon.Realtime.Player realTimePlayer { get; private set; }

    [Foldout("Resources", true)]
    public Dictionary<Resource, int> resourceDict = new();
    int[] troopArray = new int[4];
    int[] scoutArray = new int[4];
    public bool[] areasControlled = new bool[4];

    [Foldout("Cards", true)]
    public List<Card> cardsInHand = new();
    [SerializeField] Transform privateDeck;
    [SerializeField] Transform privateDiscard;

    [Foldout("UI", true)]
    TMP_Text resourceText;
    Button myButton;
    Button resignButton;
    Transform keepHand;
    public List<TroopDisplay> myDisplays { get; private set; }

    [Foldout("Choices", true)]
    public int choice { get; private set; }
    public List<Action> inReaction = new();
    public NextStep currentStep { get; private set; }
    Action firstAction;

    [Foldout("AI", true)]
    private bool _simulating;
    public bool simulating
    {
        get { return _simulating; }
        private set
        {
            if (_simulating != value)
            {
                if (myType == PlayerType.Bot) Debug.Log($"{this.name} simulating - {value}");
                _simulating = value;
            }
        }
    }
    List<DecisionChain> chainsToResolve = new();
    List<DecisionChain> finishedChains = new();
    public DecisionChain currentChain { get; private set; }

    #endregion

#region Setup

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();

        resignButton = GameObject.Find("Resign Button").GetComponent<Button>();
        keepHand = transform.Find("Keep Hand");
    }

    private void Start()
    {
        troopArray[0] = 12;
        if (PhotonNetwork.IsConnected && pv.AmOwner)
        {
            if (CarryVariables.inst.playWithBot && PhotonNetwork.CurrentRoom.MaxPlayers == 1 && Manager.inst.storePlayers.childCount == 0)
                DoFunction(() => SendName("Bot"), RpcTarget.AllBuffered);
            else
                DoFunction(() => SendName(PlayerPrefs.GetString("Online Username")), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void SendName(string username)
    {
        pv.Owner.NickName = username;
        this.name = username;
        this.transform.SetParent(Manager.inst.storePlayers);
    }

    internal void AssignInfo(int position, PlayerType type)
    {
        this.playerPosition = position;
        this.myType = type;
        Manager.inst.storePlayers.transform.localScale = Manager.inst.canvas.transform.localScale;
        this.transform.localPosition = Vector3.zero;
        if (PhotonNetwork.IsConnected)
            realTimePlayer = PhotonNetwork.PlayerList[pv.OwnerActorNr - 1];

        myDisplays = new();
        for (int i = 0; i<4; i++)
        {
            TroopDisplay display = Instantiate(CarryVariables.inst.troopDisplayPrefab);
            myDisplays.Add(display);
            display.AssignInfo(this.playerPosition, i);

            display.transform.SetParent(Manager.inst.canvas.transform);
            display.transform.localScale = Vector3.one;
            display.transform.localPosition = new(-800 + (i * 400), 225 - (playerPosition * 125));
        }

        myButton = Instantiate(CarryVariables.inst.playerButtonPrefab);
        myButton.transform.SetParent(Manager.inst.canvas.transform);
        myButton.transform.localScale = Vector3.one;
        myButton.transform.localPosition = new(-1125, 225 - (playerPosition * 125));
        myButton.onClick.AddListener(MoveScreen);
        resourceText = myButton.transform.GetChild(0).GetComponent<TMP_Text>();

        resourceDict = new()
        {
            { Resource.Coin, 0 },
            { Resource.Action, 0 },
        };
        UpdateTexts();

        if (InControl())
        {
            if (this.myType == PlayerType.Human)
            {
                resignButton.onClick.AddListener(() => Manager.inst.DoFunction(() => Manager.inst.DisplayEnding(this.playerPosition), RpcTarget.All));
                Invoke(nameof(MoveScreen), 0.2f);
                pv.Owner.NickName = this.name;
            }
            Manager.inst.DoFunction(() => Manager.inst.CompletedTurn(this.playerPosition), RpcTarget.MasterClient);
        }
    }

    #endregion

#region Cards

    [PunRPC]
    internal void ReceiveDeckCards(int[] cardIDs)
    {
        foreach (int nextNum in cardIDs)
        {
            GameObject obj = PhotonView.Find(nextNum).gameObject;
            obj.transform.SetParent(privateDeck);
        }
    }

    public void DrawCardRPC(int cardAmount, int logged, string source = "")
    {
        for (int i = 0; i < cardAmount; i++)
        {
            Card card = privateDeck.GetChild(0).GetComponent<Card>();
            Log.inst.RememberStep(this, StepType.Revert, () => DrawFromDeck(false, card.pv.ViewID, logged, source));
        }
    }

    [PunRPC]
    void DrawFromDeck(bool undo, int PV, int logged, string source)
    {
        Card card = PhotonView.Find(PV).GetComponent<Card>();
        string parathentical = source == "" ? "" : $" ({source})";

        if (undo)
        {
            cardsInHand.Remove(card);
            card.transform.SetParent(privateDeck.transform);
            card.transform.SetAsFirstSibling();
            StartCoroutine(card.MoveCard(new(0, -10000), 0.25f, Vector3.one));
        }
        else
        {
            PutCardInHand(card);
            if (InControl() && myType == PlayerType.Human)
                Log.inst.AddTextRPC(this, $"{this.name} draws {card.name}{parathentical}.", LogAdd.Personal, logged);
            else
                Log.inst.AddTextRPC(this, $"{this.name} draws 1 Card{parathentical}.", LogAdd.Personal, logged);
        }
        SortHand();
    }

    void PutCardInHand(Card card)
    {
        cardsInHand.Add(card);
        card.transform.SetParent(keepHand);
        card.transform.localPosition = new Vector2(0, -1100);
        card.layout.FillInCards(card.GetFile(), 0);
    }

    public void SortHand()
    {
        float start = -1100;
        float end = 475;
        float gap = 225;

        float midPoint = (start + end) / 2;
        int maxFit = (int)((Mathf.Abs(start) + Mathf.Abs(end)) / gap);
        UpdateTexts();

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            Card nextCard = cardsInHand[i];

            nextCard.transform.SetParent(keepHand);
            nextCard.transform.SetSiblingIndex(i);

            float offByOne = cardsInHand.Count - 1;
            float startingX = (cardsInHand.Count <= maxFit) ? midPoint - (gap * (offByOne / 2f)) : (start);
            float difference = (cardsInHand.Count <= maxFit) ? gap : gap * (maxFit / offByOne);

            Vector2 newPosition = new(startingX + difference * i, -540);
            StartCoroutine(nextCard.MoveCard(newPosition, 0.25f, Vector3.one));
            if (InControl() && myType == PlayerType.Human)
                StartCoroutine(nextCard.RevealCard(0.25f));
        }
    }

    public void DiscardPlayerCard(Card card, int logged)
    {
        Log.inst.RememberStep(this, StepType.Revert, () => DiscardFromHand(false, card.pv.ViewID, logged));
    }

    [PunRPC]
    void DiscardFromHand(bool undo, int PV, int logged)
    {
        Card card = PhotonView.Find(PV).GetComponent<Card>();

        if (undo)
        {
            PutCardInHand(card);
        }
        else
        {
            cardsInHand.Remove(card);
            card.transform.SetParent(privateDiscard);
            Log.inst.AddTextRPC(this, $"{this.name} discards {card.name}.", LogAdd.Personal, logged);
            StartCoroutine(card.MoveCard(new(0, -10000), 0.25f, Vector3.one));
        }
        SortHand();
    }

    #endregion

#region Troop Scout

    public void ChangeScoutRPC(int area, int amount, int logged, string source = "")
    {
        int actualAmount = amount;
        if (scoutArray[area] + amount < 0)
            actualAmount = -1 * scoutArray[area];
        Log.inst.RememberStep(this, StepType.Revert, () => ChangeScout(false, area, actualAmount, logged, source));
    }

    [PunRPC]
    void ChangeScout(bool undo, int area, int amount, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            scoutArray[area] -= amount;
        }
        else
        {
            scoutArray[area] += amount;
            if (amount > 0)
                Log.inst.AddTextRPC(this, $"{this.name} adds {Mathf.Abs(amount)} Scout to Area {(area + 1)}{parathentical}.", LogAdd.Personal, logged);
            else if (amount < 0)
                Log.inst.AddTextRPC(this, $"{this.name} removes {Mathf.Abs(amount)} Scout from Area {(area + 1)}{parathentical}.", LogAdd.Personal, logged);
        }
        UpdateTexts();
    }

    public void MoveTroopRPC(int oldArea, int newArea, int logged, string source = "")
    {
        if (troopArray[oldArea] == 0)
        {
            Debug.LogError($"can't advance troops from area {oldArea} (no troops there)");
            return;
        }
        if (oldArea == newArea)
        {
            Debug.LogError($"can't advance troops from area {oldArea} to {newArea}");
            return;
        }
        Log.inst.RememberStep(this, StepType.Revert, () => MoveTroop(false, oldArea, newArea, logged, source));
    }

    [PunRPC]
    void MoveTroop(bool undo, int oldArea, int newArea, int logged, string source)
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            troopArray[oldArea]++;
            troopArray[newArea]--;
        }
        else
        {
            troopArray[oldArea]--;
            troopArray[newArea]++;
            if (oldArea < newArea)
                Log.inst.AddTextRPC(this, $"{this.name} advances 1 Troop from Area {oldArea + 1} to Area {newArea + 1}{parathentical}.", LogAdd.Personal, logged);
            else
                Log.inst.AddTextRPC(this, $"{this.name} retreats 1 Troop from Area {oldArea + 1} to Area {newArea + 1}{parathentical}.", LogAdd.Personal, logged);
        }
        UpdateTexts();
    }

    public (int, int) CalcTroopScout(int area)
    {
        return (troopArray[area], scoutArray[area]);
    }

    public void UpdateAreaControl(int area, bool control, int logged)
    {
        if (this.areasControlled[area] != control)
        {
            this.areasControlled[area] = control;
            if (control)
                Log.inst.AddTextRPC(this, $"{this.name} gains control over Area {area + 1}.", LogAdd.Personal, logged);
            else
                Log.inst.AddTextRPC(this, $"{this.name} loses control over Area {area + 1}.", LogAdd.Personal, logged);
            UpdateTexts();
        }
    }

    #endregion

#region Resources

    public void ResourceRPC(Resource resource, int amount, int logged, string source = "")
    {
        int actualAmount = amount;
        if (resourceDict[resource] + amount < 0)
            actualAmount = -1 * resourceDict[resource];

        Log.inst.RememberStep(this, StepType.Revert, () => ChangeResource(false, (int)resource, actualAmount, logged, source));
    }

    [PunRPC]
    void ChangeResource(bool undo, int resource, int amount, int logged, string source = "")
    {
        string parathentical = source == "" ? "" : $" ({source})";
        if (undo)
        {
            resourceDict[(Resource)resource] -= amount;
        }
        else
        {
            resourceDict[(Resource)resource] += amount;
            if (amount > 0)
                Log.inst.AddTextRPC(this, $"{this.name} gets +{Mathf.Abs(amount)} {(Resource)resource}{parathentical}.", LogAdd.Personal, logged);
            else if (amount < 0)
                Log.inst.AddTextRPC(this, $"{this.name} loses {Mathf.Abs(amount)} {(Resource)resource}{parathentical}.", LogAdd.Personal, logged);
        }
        UpdateTexts();
    }

    public void UpdateTexts()
    {
        if (simulating)
            return;

        resourceText.text = KeywordTooltip.instance.EditText($"{this.name}:\n{cardsInHand.Count} Card, {resourceDict[Resource.Action]} Action, {resourceDict[Resource.Coin]} Coin");

        foreach (TroopDisplay display in myDisplays)
        {
            (int troop, int scout) = CalcTroopScout(display.areaPosition);
            display.UpdateText($"{this.name}: {troop} Troop + {scout} Scout",
                areasControlled[display.areaPosition] ? Color.yellow : Color.gray);
        }
    }

    #endregion

#region AI Simulate

    public void StartTurn(Action action, int currentArea)
    {
        currentChain = null;
        chainsToResolve.Clear();
        finishedChains.Clear();

        this.DoFunction(() => this.ChangeButtonColor(false));
        firstAction = action;

        if (myType == PlayerType.Bot)
        {
            simulating = true;
            action();

            currentChain = new(Log.inst.historyStack[0], currentArea);
            chainsToResolve.Add(currentChain);
            Debug.Log($"NEW SIMULATION starting with score of {PlayerScore()}");

            StartCoroutine(FindAIRoute());
            PopStack();
        }
        else
        {
            simulating = false;
            StartCoroutine(Wait());

            IEnumerator Wait()
            {
                yield return new WaitForSeconds(Log.inst.waitTime);
                foreach (Player player in Manager.inst.playersInOrder)
                {
                    if (player.myType == PlayerType.Bot)
                    {
                        while (player.simulating)
                            yield return null;
                    }
                }
                action();
                PopStack();
            }
        }
    }

    IEnumerator FindAIRoute()
    {
        while (chainsToResolve.Count > 0)
        {
            yield return null;
        }

        finishedChains = finishedChains.Shuffle();
        currentChain = finishedChains.OrderByDescending(chain => chain.math).FirstOrDefault();
        Debug.Log($"Best chain: {currentChain.math} -> {CarryVariables.inst.PrintIntList(currentChain.decisions)}");

        Log.inst.InvokeUndo(this, Log.inst.historyStack[0]);
        Log.inst.historyStack.Clear();

        DoFunction(() => ChangeButtonColor(true));
        Manager.inst.UpdateControl(-1);
        Manager.inst.DoFunction(() => Manager.inst.CompletedTurn(this.playerPosition), RpcTarget.MasterClient);
        simulating = false;
    }

    public void AIDecision(Action Next, List<int> possibleDecisions)
    {
        if (possibleDecisions.Count == 1)
        {
            this.inReaction.Add(Next);
            this.DecisionMade(possibleDecisions[0]);
        }
        else if (this.currentChain.tracker < this.currentChain.decisions.Count)
        {
            int nextChoice = currentChain.decisions[currentChain.tracker];
            this.inReaction.Add(Next);
            this.currentChain.tracker++;
            this.DecisionMade(nextChoice);
        }
        else
        {
            foreach (int next in possibleDecisions)
                chainsToResolve.Add(new(currentStep, currentChain.currentArea, currentChain.decisions ?? new(), next));
            chainsToResolve.Remove(currentChain);

            FindNewestChain();
            currentStep.action.Compile().Invoke();
        }
    }

    public List<int> ConvertToHundred(List<int> listOfInts, bool optional)
    {
        List<int> newList = new();
        if (optional)
            newList.Add(-1);

        for (int i = 0; i < listOfInts.Count; i++)
        {
            if (listOfInts[i] >= 0)
                newList.Add(listOfInts[i] + 100);
        }
        return newList;
    }

    #endregion

#region End Turn

    internal void EndTurn()
    {
        if (myType == PlayerType.Bot)
        {
            if (currentChain.complete)
            {
                Done();
            }
            else
            {
                currentChain.currentArea = (currentChain.currentArea == 3) ? 0 : currentChain.currentArea + 1;
                AreaCard nextArea = Manager.inst.listOfAreas[currentChain.currentArea];
                if (nextArea is Camp)
                    FinishChain();
                else
                    nextArea.AreaInstructions(this, 0);

                Log.inst.RememberStep(this, StepType.Revert, () => UpdateControl(false));
                PopStack();
            }
        }
        else
        {
            if (Log.inst.undosInLog.Count >= 2)
                ChooseButton(new() { "End Turn" }, Vector3.zero, "Last chance to undo anything.", Done);
            else
                Done();
        }

        void Done()
        {
            List<int> cardList = new();
            foreach (Transform next in privateDiscard)
                cardList.Add(next.GetComponent<PhotonView>().ViewID);
            Manager.inst.DoFunction(() => Manager.inst.ReceivePlayerDiscard
                (cardList.ToArray(), this.playerPosition, 15 - this.privateDeck.childCount));

            DoFunction(() => ChangeButtonColor(true));
            Manager.inst.Instructions("Waiting on other players...");

            if (myType == PlayerType.Human)
            {
                Log.inst.DisplayUndoBar(false);
                Log.inst.undosInLog.Clear();
                Manager.inst.DoFunction(() => Manager.inst.CompletedTurn(this.playerPosition), RpcTarget.MasterClient);
            }
        }
    }

    [PunRPC]
    void UpdateControl(bool undo)
    {
        Manager.inst.UpdateControl(-1);
    }

    void FinishChain()
    {
        currentChain.complete = true;
        finishedChains.Add(currentChain);

        currentChain.math = PlayerScore();
        Debug.Log($"CHAIN ENDED with score {currentChain.math}. decisions: {CarryVariables.inst.PrintIntList(currentChain.decisions)}");
        chainsToResolve.Remove(currentChain);
        currentChain = null;
    }

    float PlayerScore()
    {
        if (troopArray[3] == 12)
        {
            return Mathf.Infinity;
        }
        else
        {
            int answer = cardsInHand.Count * 3 + resourceDict[Resource.Action] * 3 + resourceDict[Resource.Coin];
            for (int i = 0; i < 4; i++)
            {
                answer += scoutArray[i] * 2;

                if (i == 1 || i == 2)
                    answer += troopArray[i] * i * 4;
                else if (i == 3)
                    answer += troopArray[i] * i * 8;

                if (areasControlled[i])
                    answer += 2;
            }

            return answer;
        }
    }

    public void DoBotTurn()
    {
        if (this.myType == PlayerType.Bot && firstAction != null)
        {
            //Debug.Log("do bot turn");
            firstAction();
            this.currentChain.tracker = 0;
            PopStack();
        }
    }

    #endregion

#region Decide

    public void ChooseButton(List<string> possibleChoices, Vector2 position, string changeInstructions, Action action)
    {
        Popup popup = Instantiate(CarryVariables.inst.textPopup);
        popup.StatsSetup(this, changeInstructions, position);

        for (int i = 0; i < possibleChoices.Count; i++)
            popup.AddTextButton(possibleChoices[i]);

        inReaction.Add(() => Destroy(popup.gameObject));
        if (action != null)
        {
            inReaction.Add(action);
            Manager.inst.Instructions(changeInstructions);
        }
    }

    public void ChooseCardOnScreen(List<Card> listOfCards, string changeInstructions, Action action)
    {
        IEnumerator haveCardsEnabled = KeepCardsOn();
        inReaction.Add(Disable);
        if (action != null)
        {
            inReaction.Add(action);
            Manager.inst.Instructions(changeInstructions);
        }

        if (listOfCards.Count == 0 && action != null)
        {
            Log.inst.undoToThis = null;
            PopStack();
        }
        else if (listOfCards.Count == 1 && action != null)
        {
            Log.inst.undoToThis = null;
            DecisionMade(0+100);
        }
        else
        {
            StartCoroutine(haveCardsEnabled);
        }

        IEnumerator KeepCardsOn()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                for (int j = 0; j < listOfCards.Count; j++)
                {
                    Card nextCard = listOfCards[j];
                    int number = j + 100;
                    ButtonToggle(nextCard.button, nextCard.border.gameObject, true, number);
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void Disable()
        {
            StopCoroutine(haveCardsEnabled);
            foreach (Card nextCard in listOfCards)
                ButtonToggle(nextCard.button, nextCard.border.gameObject, false);
        }
    }

    public void ChooseSlider(int min, int max, string changeInstructions, Action action)
    {
        SliderChoice slider = Instantiate(CarryVariables.inst.sliderPopup);
        slider.StatsSetup(this, "Choose a number.", min, max, new(0, -1000));

        inReaction.Add(() => Destroy(slider.gameObject));
        if (action != null)
        {
            inReaction.Add(action);
            Manager.inst.Instructions(changeInstructions);
        }
    }

    public void ChooseTroopDisplay(List<int> possibleChoices, string changeInstructions, Action action)
    {
        IEnumerator haveButtonsEnabled = KeepDisplaysOn();
        inReaction.Add(Disable);
        if (action != null)
        {
            inReaction.Add(action);
            Manager.inst.Instructions(changeInstructions);
        }

        if (possibleChoices.Count == 0 && action != null)
        {
            Log.inst.undoToThis = null;
            PopStack();
        }
        else if (possibleChoices.Count == 1 && action != null)
        {
            Log.inst.undoToThis = null;
            DecisionMade(possibleChoices[0]+100);
        }
        else
        {
            StartCoroutine(haveButtonsEnabled);
        }
        IEnumerator KeepDisplaysOn()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                for (int j = 0; j < possibleChoices.Count; j++)
                {
                    if (possibleChoices[j] >= 0)
                    {
                        TroopDisplay nextDisplay = myDisplays[possibleChoices[j]];
                        int number = nextDisplay.areaPosition + 100;
                        ButtonToggle(nextDisplay.button, nextDisplay.border.gameObject, true, number);
                    }
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void Disable()
        {
            StopCoroutine(haveButtonsEnabled);
            foreach (TroopDisplay nextDisplay in myDisplays)
                ButtonToggle(nextDisplay.button, nextDisplay.border.gameObject, false);
        }
    }

    void ButtonToggle(Button button, GameObject border, bool enable, int newNumber = -1)
    {
        button.onClick.RemoveAllListeners();
        button.interactable = enable;
        border.SetActive(enable);
        if (enable)
            button.onClick.AddListener(() => DecisionMade(newNumber));
    }

    #endregion

#region Resolve

    public void DecisionMade(int value)
    {
        choice = value;
        //Debug.Log($"made choice of {value}");
        PopStack();
    }

    void FindNewestChain()
    {
        bool needUndo = false;
        //Debug.Log(chainsToResolve.Count);

        for (int i = chainsToResolve.Count - 1; i >= 0; i--)
        {
            DecisionChain newChain = chainsToResolve[i];
            if (!newChain.complete)
            {
                if (currentChain == null)
                {
                    needUndo = true;
                }
                else
                {
                    for (int j = 0; j < currentChain.decisions.Count; j++)
                    {
                        if (currentChain.decisions[j] != newChain.decisions[j])
                            needUndo = true;
                    }
                }

                chainsToResolve.RemoveAt(i);
                currentChain = newChain;
                currentStep = newChain.toThisPoint;
                //Debug.Log($"switched chains (undo {needUndo}), {currentChain.toThisPoint.actionName}");
                break;
            }
        }
        if (needUndo)
        {
            Debug.Log($"AI UNDO to {currentStep.actionName} (tracker: {currentChain.tracker} - {CarryVariables.inst.PrintIntList(currentChain.decisions)})");
            Log.inst.InvokeUndo(this, currentStep);
        }
    }

    public void PopStack()
    {
        for (int i = Log.inst.historyStack.Count - 1; i >= 0; i--)
        {
            if (Log.inst.historyStack[i] == currentStep)
            {
                if (currentStep.stepType == StepType.Holding)
                    Log.inst.historyStack.RemoveAt(i);
                else
                    Log.inst.RememberStep(Log.inst, StepType.Revert, () => Log.inst.DecisionComplete(false, i));
                break;
            }
        }

        List<Action> newActions = new();
        for (int i = 0; i < inReaction.Count; i++)
            newActions.Add(inReaction[i]);

        inReaction.Clear();
        foreach (Action action in newActions)
            action();

        StartCoroutine(Delay());
    }

    IEnumerator Delay()
    {
        if (myType == PlayerType.Bot)
        {
            yield return null;
            if (currentChain == null)
                FindNewestChain();
        }

        for (int i = Log.inst.historyStack.Count - 1; i >= 0; i--)
        {
            NextStep step = Log.inst.historyStack[i];

            if ((step.stepType is StepType.UndoPoint or StepType.Holding) && !step.completed)
            {
                currentStep = step;
                if (currentChain != null && step.stepType == StepType.UndoPoint)
                {
                    Log.inst.undoToThis = step;
                    currentChain.toThisPoint = step;
                    //Debug.Log($"do {step.actionName} with ${resourceDict[Resource.Coin]} (tracker: {currentChain.tracker} - {CarryVariables.inst.PrintIntList(currentChain.decisions)})");
                }

                step.action.Compile().Invoke();
                if (step.stepType == StepType.Holding)
                    PopStack();
                break;
            }
        }
        yield return null;
    }

    #endregion

#region Helpers

    public bool InControl()
    {
        if (PhotonNetwork.IsConnected)
            return this.pv.AmOwner;
        else
            return true;
    }

    void MoveScreen()
    {
        foreach (Transform transform in Manager.inst.storePlayers)
            transform.localPosition = new(0, -10000);
        this.transform.localPosition = Vector3.zero;
    }

    [PunRPC]
    internal void ChangeButtonColor(bool done)
    {
        myButton.image.color = (done) ? Color.yellow : Color.gray;
    }

    #endregion

}