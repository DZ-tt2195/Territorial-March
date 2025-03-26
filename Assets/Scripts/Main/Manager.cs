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
        StartCoroutine(Setup());
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
        {
            yield return null;
        }

        if (PhotonNetwork.IsMasterClient)
            ReadySetup();
    }

    #endregion

#region Master Cards

    void ReadySetup()
    {
        storePlayers.Shuffle();

        cardRequestArray = new int[storePlayers.childCount];
        for (int i = 0; i < storePlayers.childCount; i++)
            cardRequestArray[i] = 12;

        for (int i = 0; i < storePlayers.childCount; i++)
        {
            GameObject nextPlayer = storePlayers.transform.GetChild(i).gameObject;
            DoFunction(() => AddPlayer(nextPlayer.GetComponent<PhotonView>().ViewID, i, nextPlayer.name.Equals("Bot") ? 1 : 0));
        }
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

        void CheckDeadPlayers()
        {
            bool keepPlaying = true;
            foreach (Player player in playersInOrder)
            {
                /*
                if (player.myBase.myHealth <= 0)
                    keepPlaying = false;
                */
            }

            if (keepPlaying)
                Continue();
            else
                DoFunction(() => DisplayEnding(-1), RpcTarget.All);
        }
    }

    [PunRPC]
    internal void CompletedTurn()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Continue();
        }
        else
        {
            waitingOnPlayers--;
            /*
            if (waitingOnPlayers == 0)
            {
                foreach (Player player in playersInOrder)
                    player.pv.RPC(nameof(player.ShareSteps), player.realTimePlayer);
                Continue();
            }*/
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

    public Player OpposingPlayer(Player player)
    {
        return OpposingPlayer(player.playerPosition);
    }

    public Player OpposingPlayer(int position)
    {
        return (position == 0) ? playersInOrder[1] : playersInOrder[0];
    }

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
    internal void SetCurrentPlayer(int playerPosition)
    {
        currentPlayer = playersInOrder[playerPosition];
    }

    #endregion

}
