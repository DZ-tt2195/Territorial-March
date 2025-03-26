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
    public List<int> decisions;
    public float math = 0;
    public NextStep toThisPoint;

    public string PrintDecisions()
    {
        string answer = "";
        foreach (int next in this.decisions)
            answer += $"{next}, ";
        return answer;
    }

    public DecisionChain(NextStep toThisPoint)
    {
        complete = false;
        decisions = new();
        this.toThisPoint = toThisPoint;
    }

    public DecisionChain(List<int> oldList, int toAdd, NextStep toThisPoint)
    {
        complete = false;
        decisions = new(oldList) {toAdd};
        //Debug.Log($"new chain at {toThisPoint.actionName}: {PrintDecisions()}");
        this.toThisPoint = toThisPoint;
    }
}

public enum PlayerType { Human, Bot }
public enum Resource { Coin, Play }

public class Player : PhotonCompatible
{

#region Variables

    [Foldout("Player info", true)]
    public int playerPosition { get; private set; }
    public PlayerType myType { get; private set; }
    public Photon.Realtime.Player realTimePlayer { get; private set; }
    public Dictionary<Resource, int> resourceDict = new();
    public int[] troopArray = new int[4];
    public int[] scoutArray = new int[4];

    [Foldout("Cards", true)]
    public List<Card> cardsInHand = new();
    [SerializeField] Transform privateDeck;
    [SerializeField] Transform privateDiscard;

    [Foldout("UI", true)]
    [SerializeField] TMP_Text resourceText;
    Button resignButton;
    Transform keepHand;

    [Foldout("Choices", true)]
    public int choice { get; private set; }
    public List<Action> inReaction = new();
    public NextStep currentStep { get; private set; }

    [Foldout("AI", true)]
    public bool simulating { get; private set; }
    List<DecisionChain> chainsToResolve = new();
    List<DecisionChain> finishedChains = new();
    public DecisionChain currentChain { get; private set; }
    public int chainTracker;

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
        if (PhotonNetwork.IsConnected && pv.AmOwner)
        {
            if (PhotonNetwork.CurrentRoom.MaxPlayers == 1 && Manager.inst.storePlayers.childCount == 0)
                DoFunction(() => SendName("Bot"), RpcTarget.AllBuffered);
            else
                DoFunction(() => SendName(PlayerPrefs.GetString("Online Username")), RpcTarget.AllBuffered);
            /*
            List<string> newList = new();
            newList.AddRange(CarryVariables.inst.cardScripts);
            List<string> shuffledCards = newList.Shuffle();

            for (int i = 0; i < shuffledCards.Count; i++)
            {
                int nextPosition = i;
                GameObject next = Manager.inst.MakeObject(CarryVariables.inst.cardPrefab.gameObject);
                DoFunction(() => AddCard(i, next.GetComponent<PhotonView>().ViewID, shuffledCards[i]), RpcTarget.AllBuffered);
            }
            */
        }
    }
    /*
    [PunRPC]
    void AddCard(int position, int ID, string cardName)
    {
        GameObject nextObject = PhotonView.Find(ID).gameObject;
        Card card = CarryVariables.inst.AddCardComponent(nextObject, cardName);
        card.transform.SetParent(deck);
        card.transform.SetSiblingIndex(position);
        card.transform.localPosition = new(0, -10000);
    }
    */
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

        /*
        myButton = Instantiate(CarryVariables.inst.playerButtonPrefab);
        myButton.transform.SetParent(Manager.inst.canvas.transform);
        myButton.transform.localScale = Vector3.one;
        myButton.transform.localPosition = new(-1100, 425 - (100 * playerPosition));
        myButton.transform.GetChild(0).GetComponent<TMP_Text>().text = this.name;
        myButton.onClick.AddListener(MoveScreen);
        */
        resourceDict = new()
        {
            { Resource.Coin, 0 },
            { Resource.Play, 0 },
        };
        UpdateResourceText();

        if (InControl())
        {
            if (this.myType == PlayerType.Human)
            {
                resignButton.onClick.AddListener(() => Manager.inst.DoFunction(() => Manager.inst.DisplayEnding(this.playerPosition), RpcTarget.All));
                Invoke(nameof(MoveScreen), 0.2f);
                pv.Owner.NickName = this.name;
            }

            //Manager.inst.DoFunction(() => Manager.inst.PlayerDone());
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
            obj.transform.parent.SetParent(privateDeck);
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
                Log.inst.AddTextRPC($"{this.name} draws {card.name}{parathentical}.", LogAdd.Personal, logged);
            else
                Log.inst.AddTextRPC($"{this.name} draws 1 Card{parathentical}.", LogAdd.Personal, logged);
        }
        SortHand();
    }

    void PutCardInHand(Card card)
    {
        cardsInHand.Add(card);
        card.transform.SetParent(keepHand);
        card.transform.localPosition = new Vector2(0, -1100);
        card.layout.FillInCards(card);
        card.layout.cg.alpha = 0;
    }

    public void SortHand()
    {
        float start = -1100;
        float end = 475;
        float gap = 225;

        float midPoint = (start + end) / 2;
        int maxFit = (int)((Mathf.Abs(start) + Mathf.Abs(end)) / gap);
        cardsInHand = cardsInHand.OrderBy(card => card.coinCost).ToList();

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
            Log.inst.AddTextRPC($"{this.name} discards {card.name}.", LogAdd.Personal, logged);
            StartCoroutine(card.MoveCard(new(0, -10000), 0.25f, Vector3.one));
        }
        SortHand();
    }

    #endregion

#region Resources

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
                Log.inst.AddTextRPC($"{this.name} adds {Mathf.Abs(amount)} Scout to Area {(area + 1)}{parathentical}.", LogAdd.Personal, logged);
            else if (amount < 0)
                Log.inst.AddTextRPC($"{this.name} removes {Mathf.Abs(amount)} Scout from Area {(area + 1)}{parathentical}.", LogAdd.Personal, logged);
        }
    }

    public void MoveTroopRPC(int oldArea, int newArea, int logged, string source = "")
    {
        if (troopArray[oldArea] == 0 || oldArea == newArea)
            return;
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
                Log.inst.AddTextRPC($"{this.name} advances 1 Troop from Area {oldArea + 1} to Area {newArea + 1}{parathentical}.", LogAdd.Personal, logged);
            else
                Log.inst.AddTextRPC($"{this.name} retreats 1 Troop from Area {oldArea + 1} to Area {newArea + 1}{parathentical}.", LogAdd.Personal, logged);
        }
    }

    void UpdateResourceText()
    {
        resourceText.text = KeywordTooltip.instance.EditText(
            $"{resourceDict[Resource.Coin]} Coin, {cardsInHand.Count} Card");
    }

    public void ResourceRPC(Resource resource, int amount, int logged, string source = "")
    {
        int actualAmount = amount;
        if (resourceDict[resource] + amount < 0)
            actualAmount = -1 * resourceDict[resource];

        Log.inst.RememberStep(this, StepType.Revert, () => ChangeResource(false, (int)resource, actualAmount, logged, source));
        UpdateResourceText();
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
                Log.inst.AddTextRPC($"{this.name} gets +{Mathf.Abs(amount)} {(Resource)resource}{parathentical}.", LogAdd.Personal, logged);
            else
                Log.inst.AddTextRPC($"{this.name} loses {Mathf.Abs(amount)} {(Resource)resource}{parathentical}.", LogAdd.Personal, logged);
        }
    }

    #endregion

#region Turn

    [PunRPC]
    internal void YourTurn()
    {
        Log.inst.historyStack.Clear();
        Log.inst.currentDecisionInStack = -1;

        chainsToResolve.Clear();
        finishedChains.Clear();
        chainTracker = -1;

        Manager.inst.DoFunction(() => Manager.inst.Instructions($"Waiting on {this.name}..."));
        Manager.inst.DoFunction(() => Manager.inst.SetCurrentPlayer(this.playerPosition));

        Log.inst.AddTextRPC("", LogAdd.Public, 0);
        Log.inst.AddTextRPC($"{this.name}'s turn.", LogAdd.Public, 0);
        Log.inst.RememberStep(this, StepType.UndoPoint, () => MayPlayCard());

        if (myType == PlayerType.Bot)
        {
            currentChain = new(Log.inst.historyStack[0]);
            chainsToResolve.Add(currentChain);
            StartCoroutine(FindAIRoute());
        }
        else
        {
            simulating = false;
            PopStack();
        }
    }

    IEnumerator FindAIRoute()
    {
        yield return new WaitForSeconds(1f);
        simulating = true;
        PopStack();

        while (chainsToResolve.Count > 0)
        {
            yield return null;
        }

        //Debug.Log($"{finishedChains.Count} chains finished");
        finishedChains = finishedChains.Shuffle();
        currentChain = finishedChains.OrderByDescending(chain => chain.math).FirstOrDefault();

        string answer = $"Best chain: {currentChain.math} -> ";
        foreach (int nextInt in currentChain.decisions)
            answer += $"{nextInt} ";
        //Debug.Log(answer);

        finishedChains.Clear();
        Log.inst.InvokeUndo(Log.inst.historyStack[0]);

        simulating = false;
        Log.inst.RememberStep(this, StepType.UndoPoint, () => MayPlayCard());
        chainTracker = -1;
        PopStack();
    }

    internal void MayPlayCard()
    {
        List<string> actions = new() { $"End Turn" };

        if (myType == PlayerType.Bot)
        {
            if (chainTracker < currentChain.decisions.Count)
            {
                int next = currentChain.decisions[chainTracker];
                //Debug.Log($"resolved continue turn with choice {next}");
                inReaction.Add(ActionResolution);
                DecisionMade(next);
            }
            else
            {
                List<int> choices = new() { -1 };
                for (int i = 0; i < cardsInHand.Count; i++)
                    choices.Add(i + 100);
                NewChains(choices);
            }
        }
        else
        {
            ChooseButton(actions, Vector3.zero, (cardsInHand.Count) == 0 ? "Can't play cards." : "What to play?", ActionResolution);
            ChooseCardOnScreen(cardsInHand, (cardsInHand.Count) == 0 ? "You can't play any cards." : "What to play?", null);
        }

        void ActionResolution()
        {
            int convertedChoice = choice - 100;
            if (convertedChoice < cardsInHand.Count && convertedChoice >= 0)
            {
                Card toPlay = cardsInHand[convertedChoice];
                Log.inst.AddTextRPC($"{this.name} plays {toPlay.name}.", LogAdd.Remember, 0);

                DiscardPlayerCard(toPlay, -1);
                toPlay.OnPlayEffect(this, 0);
            }
            else
            {
                if (myType == PlayerType.Bot && !currentChain.complete)
                {
                    FinishChain();
                }
                else
                {
                    Log.inst.AddTextRPC($"{this.name} ends their turn.", LogAdd.Remember);
                    Manager.inst.DoFunction(() => Manager.inst.Instructions($""));
                    Log.inst.ShareSteps();
                }
            }
        }
    }

    void FinishChain()
    {
        currentChain.complete = true;
        chainsToResolve.Remove(currentChain);
        finishedChains.Add(currentChain);

        currentChain.math = PlayerScore(this) - PlayerScore(Manager.inst.OpposingPlayer(this));
        //Debug.Log($"CHAIN ENDED with score {currentChain.math}. decisions: {currentChain.PrintDecisions()}");
        currentChain = null;

        float PlayerScore(Player player)
        {
            /*
            int answer = player.myBase.myHealth + player.cardsInHand.Count * 3;

            foreach ((Card card, Entity entity) in Manager.inst.GatherAbilities())
                answer += card.CoinEffect(player, entity, -1);

            if (player == this)
                answer -= coins;

            foreach (Row row in Manager.inst.allRows)
            {
                MovingTroop troop = row.playerTroops[playerPosition];
                if (troop != null && troop.calcHealth >= 1)
                    answer += troop.calcPower + troop.calcHealth;
            }

            if (player.myBase.myHealth <= 0)
                return -Mathf.Infinity;
            else
                return answer;
            */
            return 0;
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
            PopStack();
        else if (listOfCards.Count == 1 && action != null)
            DecisionMade(0);
        else
            StartCoroutine(haveCardsEnabled);

        IEnumerator KeepCardsOn()
        {
            float elapsedTime = 0f;
            while (elapsedTime < 0.3f)
            {
                for (int j = 0; j < listOfCards.Count; j++)
                {
                    Card nextCard = listOfCards[j];
                    int buttonNumber = j + 100;

                    nextCard.button.onClick.RemoveAllListeners();
                    nextCard.button.interactable = true;
                    nextCard.button.onClick.AddListener(() => DecisionMade(buttonNumber));
                    nextCard.border.gameObject.SetActive(true);
                }
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        void Disable()
        {
            StopCoroutine(haveCardsEnabled);

            foreach (Card nextCard in listOfCards)
            {
                nextCard.button.onClick.RemoveAllListeners();
                nextCard.button.interactable = false;
                nextCard.border.gameObject.SetActive(false);
            }
        }
    }

    public void ChooseSlider(int min, int max, string changeInstructions, Action action)
    {
        SliderChoice slider = Instantiate(CarryVariables.inst.sliderPopup);
        slider.StatsSetup(this, "Choose a number.", min, max, new(0, -1000));

        inReaction.Add(() => Destroy(slider.gameObject));
        inReaction.Add(action);
        Manager.inst.Instructions(changeInstructions);
    }

    #endregion

#region Resolve

    public void NewChains(List<int> decisionNumbers)
    {
        foreach (int next in decisionNumbers)
            chainsToResolve.Add(new(currentChain.decisions ?? new(), next, currentStep));
        chainsToResolve.Remove(currentChain);

        FindNewestChain();
        currentStep.action.Compile().Invoke();
    }

    void FindNewestChain()
    {
        bool needUndo = false;

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
                        {
                            needUndo = true;
                            break;
                        }
                    }
                }

                chainsToResolve.RemoveAt(i);
                currentChain = newChain;
                currentStep = newChain.toThisPoint;
                //Debug.Log($"switched chains (undo {needUndo}), {currentChain.toThisPoint.actionName}. decisions: {currentChain.PrintDecisions()}");
                break;
            }
        }

        if (needUndo)
        {
            //Debug.Log($"AI UNDO to {currentStep.actionName}");
            Log.inst.InvokeUndo(currentStep);
        }
    }

    public void PopStack()
    {
        if (Log.inst.currentDecisionInStack >= 0)
        {
            int number = Log.inst.currentDecisionInStack;
            Log.inst.RememberStep(Log.inst, StepType.Revert, () => Log.inst.DecisionComplete(false, number));
        }

        List<Action> newActions = new();
        for (int i = 0; i < inReaction.Count; i++)
            newActions.Add(inReaction[i]);

        inReaction.Clear();
        foreach (Action action in newActions)
            action();

        if (currentChain == null && myType == PlayerType.Bot)
            FindNewestChain();

        for (int i = Log.inst.historyStack.Count - 1; i >= 0; i--)
        {
            NextStep step = Log.inst.historyStack[i];
            if (step.stepType == StepType.UndoPoint && !step.completed)
            {
                currentStep = step;
                Log.inst.currentDecisionInStack = i;
                Log.inst.undoToThis = step;

                if (currentChain != null)
                {
                    currentChain.toThisPoint = step;
                    chainTracker++;
                }

                StartCoroutine(RunFunction());
                IEnumerator RunFunction()
                {
                    if (!simulating && myType == PlayerType.Bot)
                        yield return new WaitForSeconds(Log.inst.waitTime);
                    step.action.Compile().Invoke();
                }
                break;
            }
        }
    }

    public void DecisionMade(int value)
    {
        choice = value;
        //Debug.Log($"made choice of {value}");
        PopStack();
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

    #endregion

    void GiveToManager()
    {
        List<int> cardList = new();
        foreach (Transform next in privateDiscard)
            cardList.Add(next.GetComponent<PhotonView>().ViewID);
        Manager.inst.DoFunction(() => Manager.inst.ReceivePlayerDiscard
            (cardList.ToArray(), this.playerPosition, 12 - this.privateDeck.childCount));
    }

}


