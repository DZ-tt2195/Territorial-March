using System.Collections;
using System.Collections.Generic;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class Manager : PhotonCompatible
{

#region Variables

    public static Manager inst;

    [Foldout("Players", true)]
    public List<Player> playersInOrder;
    public Transform storePlayers { get; private set; }
    public Player currentPlayer { get; private set; }

    [Foldout("Gameplay", true)]
    public int turnNumber { get; private set; }
    List<Action> actionStack = new();
    int currentStep = -1;
    int waitingOnPlayers = 0;
    public List<AreaCard> listOfAreas = new();

    [Foldout("Master deck", true)]
    [SerializeField] Transform masterDeck;
    [SerializeField] Transform masterDiscard;
    int[] cardRequestArray;

    [Foldout("UI and Animation", true)]
    [SerializeField] TMP_Text instructions;
    public float opacity { get; private set; }
    bool decrease = true;
    public Canvas canvas { get; private set; }

    [Foldout("Ending", true)]
    [SerializeField] Transform endScreen;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] Button quitGame;

    #endregion

#region Setup

    protected override void Awake()
    {
        base.Awake();
        inst = this;
        turnNumber = 0;
        bottomType = this.GetType();
        canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        storePlayers = GameObject.Find("Store Players").transform;
    }

    private void FixedUpdate()
    {
        if (decrease)
            opacity -= 0.05f;
        else
            opacity += 0.05f;
        if (opacity < 0 || opacity > 1)
            decrease = !decrease;
    }

    public GameObject MakeObject(GameObject prefab)
    {
        if (PhotonNetwork.IsConnected)
            return PhotonNetwork.Instantiate(prefab.name, Vector3.zero, new());
        else
            return Instantiate(prefab);
    }

    private void Start()
    {
        if (PhotonNetwork.CurrentRoom.MaxPlayers == 1)
            MakeObject(CarryVariables.inst.playerPrefab.gameObject);
        MakeObject(CarryVariables.inst.playerPrefab.gameObject);

        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            for (int j = 0; j < 60; j++)
            {
                for (int i = 0; i < CarryVariables.inst.playerCardFiles.Count; i++)
                {
                    GameObject next = MakeObject(CarryVariables.inst.playerCardPrefab.gameObject);
                    DoFunction(() => AddPlayerCard(next.GetComponent<PhotonView>().ViewID, i), RpcTarget.AllBuffered);
                }
            }

            List<int> usedAreas = new() { 0, 1 };
            int firstRandom = UnityEngine.Random.Range(2, CarryVariables.inst.areaCardFiles.Count);
            usedAreas.Add(firstRandom);
            int secondRandom;
            do { secondRandom = UnityEngine.Random.Range(2, CarryVariables.inst.areaCardFiles.Count); }
            while (secondRandom == firstRandom);

            usedAreas = usedAreas.Shuffle();
            for (int i = 0; i < usedAreas.Count; i++)
            {
                GameObject next = MakeObject(CarryVariables.inst.areaCardPrefab.gameObject);
                DoFunction(() => AddAreaCard(next.GetComponent<PhotonView>().ViewID, i, usedAreas[i]), RpcTarget.AllBuffered);
            }
        }
        StartCoroutine(Setup());
    }

    [PunRPC]
    void AddPlayerCard(int ID, int fileNumber)
    {
        GameObject nextObject = PhotonView.Find(ID).gameObject;
        PlayerCardData data = CarryVariables.inst.playerCardFiles[fileNumber];

        nextObject.name = data.cardName;
        nextObject.transform.SetParent(masterDeck);
        nextObject.transform.localPosition = new(250 * masterDeck.childCount, 10000);

        Type type = Type.GetType(data.cardName.Replace(" ", ""));
        if (type != null)
            nextObject.AddComponent(type);
        else
            nextObject.AddComponent(Type.GetType(nameof(PlayerCard)));

        PlayerCard card = nextObject.GetComponent<PlayerCard>();
        card.AssignInfo(fileNumber);
    }

    [PunRPC]
    void AddAreaCard(int ID, int fileNumber, int areaNumber)
    {
        GameObject nextObject = PhotonView.Find(ID).gameObject;
        CardData data = CarryVariables.inst.areaCardFiles[fileNumber];

        nextObject.name = data.cardName;
        nextObject.transform.SetParent(canvas.transform);
        switch (areaNumber)
        {
            case 0:
                nextObject.transform.localPosition = new(-800, 500);
                return;
            case 1:
                nextObject.transform.localPosition = new(-400, 600);
                return;
            case 2:
                nextObject.transform.localPosition = new(0, 400);
                return;
            case 3:
                nextObject.transform.localPosition = new(400, 500);
                return;
        }

        Type type = Type.GetType(data.cardName.Replace(" ", ""));
        if (type != null)
            nextObject.AddComponent(type);
        else
            nextObject.AddComponent(Type.GetType(nameof(AreaCard)));

        AreaCard card = nextObject.GetComponent<AreaCard>();
        card.AssignInfo(fileNumber);
        card.AssignAreaNum(areaNumber);
        listOfAreas.Insert(areaNumber, card);
    }

    IEnumerator Setup()
    {
        CoroutineGroup group = new(this);
        group.StartCoroutine(WaitForPlayers());
        group.StartCoroutine(SinglePlayerWait());

        IEnumerator SinglePlayerWait()
        {
            if (PhotonNetwork.CurrentRoom.MaxPlayers == 1)
                yield return new WaitForSeconds(1f);
        }

        IEnumerator WaitForPlayers()
        {
            if (PhotonNetwork.IsConnected)
            {
                instructions.text = $"Waiting for more players ({storePlayers.childCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
                while (storePlayers.childCount < PhotonNetwork.CurrentRoom.MaxPlayers)
                {
                    instructions.text = $"Waiting for more players ({storePlayers.childCount}/{PhotonNetwork.CurrentRoom.MaxPlayers})";
                    yield return null;
                }
                instructions.text = $"All players are in.";
            }
        }

        while (group.AnyProcessing)
            yield return null;

        if (PhotonNetwork.IsMasterClient)
            ReadySetup();
    }

    #endregion

#region Master Cards

    void ReadySetup()
    {
        storePlayers.Shuffle();
        masterDeck.Shuffle();
        AddStep(FirstDraw);

        cardRequestArray = new int[storePlayers.childCount];
        for (int i = 0; i < storePlayers.childCount; i++)
            cardRequestArray[i] = 15;

        waitingOnPlayers = storePlayers.childCount;
        for (int i = 0; i < storePlayers.childCount; i++)
        {
            GameObject nextPlayer = storePlayers.transform.GetChild(i).gameObject;
            DoFunction(() => AddPlayer(nextPlayer.GetComponent<PhotonView>().ViewID, i, nextPlayer.name.Equals("Bot") ? 1 : 0));
        }
    }

    void FirstDraw()
    {
        foreach (Player player in playersInOrder)
            player.DoFunction(() => player.InitialHand(2), player.realTimePlayer);
    }

    [PunRPC]
    void AddPlayer(int PV, int position, int playerType)
    {
        Player nextPlayer = PhotonView.Find(PV).GetComponent<Player>();
        playersInOrder ??= new();
        playersInOrder.Insert(position, nextPlayer);
        instructions.text = "";
        nextPlayer.AssignInfo(position, (PlayerType)playerType);
    }

    void SendOutCards()
    {
        for (int i = 0; i < cardRequestArray.Length; i++)
        {
            List<int> cardList = new();
            for (int j = 0; j < cardRequestArray[i]; j++)
            {
                if (masterDeck.childCount == 0)
                {
                    Debug.Log("shuffled discard pile");
                    masterDiscard.Shuffle();
                    while (masterDiscard.childCount > 0)
                        masterDiscard.GetChild(0).transform.SetParent(masterDeck);
                }
                GameObject next = masterDeck.GetChild(0).gameObject;
                next.transform.SetParent(null);
                cardList.Add(next.GetComponent<PhotonView>().ViewID);
            }
            playersInOrder[i].DoFunction(() => playersInOrder[i].ReceiveDeckCards(cardList.ToArray()));
        }
    }

    [PunRPC]
    internal void ReceivePlayerDiscard(int[] cardIDs, int playerPosition, int cardRequest)
    {
        foreach (int nextNum in cardIDs)
        {
            GameObject obj = PhotonView.Find(nextNum).gameObject;
            obj.transform.parent.SetParent(masterDiscard);
        }
        cardRequestArray[playerPosition] = cardRequest;
    }

#endregion

#region Gameplay Loop

    void AddStep(Action action, int position = -1)
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            if (position < 0 || currentStep < 0)
                actionStack.Add(action);
            else
                actionStack.Insert(currentStep + position, action);
        }
    }

    [PunRPC]
    public void Instructions(string text)
    {
        instructions.text = KeywordTooltip.instance.EditText(text);
    }

    [PunRPC]
    public void Continue()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
            Invoke(nameof(NextAction), 0.25f);
    }

    void NextAction()
    {
        if (currentStep < actionStack.Count - 1)
        {
            Log.inst.AddTextRPC("", LogAdd.Public);

            SendOutCards();
            Log.inst.DoFunction(() => Log.inst.ResetHistory());
            DoFunction(() => UpdateAllDisplays());

            if (playersInOrder != null)
                waitingOnPlayers = playersInOrder.Count;
            else if (PhotonNetwork.IsConnected)
                waitingOnPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            else
                waitingOnPlayers = 1;

            currentStep++;
            actionStack[currentStep]();
        }
        else
        {
            AddBasicLoop();
            NextAction();
        }
    }

    void AddBasicLoop()
    {
        /*
        AddStep(NewResources);
        PlayerSteps(0);
        PlayerSteps(1);
        AddStep(TroopsAttack);
        AddStep(CheckDeadPlayers);

        AddStep(NewResources);
        PlayerSteps(1);
        PlayerSteps(0);
        AddStep(TroopsAttack);
        AddStep(CheckDeadPlayers);
        */
    }

    [PunRPC]
    internal void CompletedTurn()
    {
        if (PhotonNetwork.IsConnected)
        {
            waitingOnPlayers--;

            if (waitingOnPlayers == 0)
            {
                foreach (Player player in playersInOrder)
                    Log.inst.pv.RPC(nameof(Log.ShareSteps), player.realTimePlayer);
                Continue();
            }
        }
        else
        {
            Continue();
        }
    }

    #endregion

#region Ending

    [PunRPC]
    public void DisplayEnding(int resignPosition)
    {
        scoreText.text = "";
        Popup[] allPopups = FindObjectsByType<Popup>(FindObjectsSortMode.None);
        foreach (Popup popup in allPopups)
            Destroy(popup.gameObject);

        /*
        List<Player> playerLifeInOrder = playersInOrder.OrderByDescending(player => player.myBase.myHealth).ToList();
        int nextPlacement = 1;

        Log.inst.AddTextRPC("", LogAdd.Personal);
        Log.inst.AddTextRPC("The game has ended.", LogAdd.Personal);
        Instructions("The game has ended.");

        Player resignPlayer = null;
        if (resignPosition >= 0)
        {
            resignPlayer = playersInOrder[resignPosition];
            Log.inst.AddTextRPC($"{resignPlayer.name} has resigned.", LogAdd.Personal);
        }

        for (int i = 0; i < playerLifeInOrder.Count; i++)
        {
            Player player = playerLifeInOrder[i];
            if (player != resignPlayer)
            {
                EndstatePlayer(player, false);
                if (i == 0 || playerLifeInOrder[i - 1].myBase.myHealth != player.myBase.myHealth)
                    nextPlacement++;
            }
        }

        if (resignPlayer != null)
            EndstatePlayer(resignPlayer, true);
        scoreText.text = KeywordTooltip.instance.EditText(scoreText.text);

        endScreen.gameObject.SetActive(true);
        quitGame.onClick.AddListener(Leave);
*/
    }

    void EndstatePlayer(Player player, bool resigned)
    {
        //scoreText.text += $"\n\n{player.name} - {player.myBase.myHealth} Health {(resigned ? $"[Resigned]" : "")}";
        scoreText.text += "\n";
    }

    void Leave()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("1. Lobby");
        }
        else
        {
            SceneManager.LoadScene("0. Loading");
        }
    }

    #endregion

#region Misc

    public Player FindThisPlayer()
    {
        foreach (Player player in playersInOrder)
        {
            if (player.InControl())
                return player;
        }
        return null;
    }

    [PunRPC]
    void UpdateAllDisplays()
    {
        for (int i = 0; i < 4; i++)
        {
            Player mostInArea = null;
            int highestValue = int.MinValue;

            foreach (Player player in playersInOrder)
            {
                (int troop, int scout) = player.CalcTroopScout(i);
                int totalUnits = troop + scout;

                if (totalUnits > highestValue)
                {
                    mostInArea = player;
                    highestValue = totalUnits;
                }
                else if (totalUnits == highestValue)
                {
                    mostInArea = null;
                }
            }

            foreach (Player player in playersInOrder)
                player.UpdateAreaControl(i, player == mostInArea);
        }
    }

    #endregion

}
