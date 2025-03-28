using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MyBox;
using Photon.Pun;
using System.Linq.Expressions;
using System;

public enum StepType { None, UndoPoint, Revert, Wait }
public enum LogAdd { Personal, Public, Remember }

[Serializable]
public class NextStep
{
    public StepType stepType { get; private set; }
    [TextArea(5, 5)] public string actionName { get; private set; }
    public PhotonCompatible source { get; private set; }
    public Expression<Action> action { get; private set; }
    public bool completed = false;

    internal NextStep(PhotonCompatible source, StepType stepType, Expression<Action> action)
    {
        this.source = source;
        this.action = action;
        this.actionName = $"{action.ToString().Replace("() => ", "")}";
        ChangeType(stepType);
    }

    internal void ChangeType(StepType stepType)
    {
        if (this.stepType == StepType.UndoPoint && stepType != StepType.UndoPoint)
        {
            Log.inst.undoToThis = null;
            Debug.Log("undopoint canceled");
        }

        this.stepType = stepType;
        completed = stepType != StepType.UndoPoint;
    }
}

public class Log : PhotonCompatible
{

#region Variables

    public static Log inst;

    [Foldout("Log", true)]
        Scrollbar scroll;
        [SerializeField] RectTransform RT;
        GridLayoutGroup gridGroup;
        [SerializeField] LogText textBoxClone;
        Vector2 startingSize;
        Vector2 startingPosition;

    [Foldout("Undos", true)]
        public List<LogText> undosInLog = new();
        public NextStep undoToThis;
        [SerializeField] Button undoButton;
        bool currentUndoState = false;
        public List<NextStep> historyStack = new();
        public int currentDecisionInStack = -1;
        public float waitTime { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();

        inst = this;
        waitTime = 0.25f;
        gridGroup = RT.GetComponent<GridLayoutGroup>();
        scroll = this.transform.GetChild(1).GetComponent<Scrollbar>();

        startingSize = RT.sizeDelta;
        startingPosition = RT.transform.localPosition;
        undoButton.onClick.AddListener(() => DisplayUndoBar(!currentUndoState));
    }

    #endregion

#region Add To Log

    public static string Article(string followingWord)
    {
        if (followingWord.StartsWith('A')
            || followingWord.StartsWith('E')
            || followingWord.StartsWith('I')
            || followingWord.StartsWith('O')
            || followingWord.StartsWith('U'))
        {
            return $"an {followingWord}";
        }
        else
        {
            return $"a {followingWord}";
        }
    }

    public void AddTextRPC(string logText, LogAdd type, int indent = 0)
    {
        switch (type)
        {
            case LogAdd.Personal:
                AddText(false, logText, indent);
                break;
            case LogAdd.Public:
                DoFunction(() => AddText(false, logText, indent));
                break;
            case LogAdd.Remember:
                RememberStep(this, StepType.Revert, () => AddText(false, logText, indent));
                break;
        }
    }

    [PunRPC]
    void AddText(bool undo, string logText, int indent = 0)
    {
        if (undo || indent < 0)
            return;

        LogText newText = Instantiate(textBoxClone, RT.transform);
        newText.name = $"Log {RT.transform.childCount}";
        ChangeScrolling();

        newText.textBox.text = "";
        for (int i = 0; i < indent; i++)
            newText.textBox.text += "     ";

        newText.textBox.text += string.IsNullOrEmpty(logText) ? "" : char.ToUpper(logText[0]) + logText[1..];
        newText.textBox.text = KeywordTooltip.instance.EditText(newText.textBox.text);

        if (undoToThis != null)
        {
            if (undoToThis.action != null)
            {
                newText.step = undoToThis;
                //Debug.Log($"NEW UNDO IN LOG: {logText} - {undoToThis.action}");
                undosInLog.Insert(0, newText);
            }
            undoToThis = null;
        }
    }

    void ChangeScrolling()
    {
        int goPast = Mathf.FloorToInt((startingSize.y / gridGroup.cellSize.y) - 1);
        //Debug.Log($"{RT.transform.childCount} vs {goPast}");
        if (RT.transform.childCount > goPast)
        {
            RT.sizeDelta = new Vector2(startingSize.x, startingSize.y + ((RT.transform.childCount - goPast) * gridGroup.cellSize.y));
            if (scroll.value <= 0.2f)
            {
                RT.transform.localPosition = new Vector3(RT.transform.localPosition.x, RT.transform.localPosition.y + gridGroup.cellSize.y / 2, 0);
                scroll.value = 0;
            }
        }
        else
        {
            RT.sizeDelta = startingSize;
            RT.transform.localPosition = startingPosition;
            scroll.value = 0;
        }
    }

    private void Update()
    {
        undosInLog.RemoveAll(item => item == null);
        undoButton.gameObject.SetActive(undosInLog.Count > 0);

        if (Application.isEditor && Input.GetKeyDown(KeyCode.Space))
            AddTextRPC($"test {RT.transform.childCount}", LogAdd.Personal);
    }

    /*
    void OnEnable()
    {
        Application.logMessageReceived += DebugMessages;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= DebugMessages;
    }

    void DebugMessages(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error)
        {
            AddText($"");
            AddText($"the game crashed :(");
        }
    }
    */
    #endregion

#region Undos

    public void DisplayUndoBar(bool flash)
    {
        currentUndoState = flash;
        for (int i = 0; i < undosInLog.Count; i++)
        {
            LogText next = undosInLog[i];
            next.button.onClick.RemoveAllListeners();
            next.button.interactable = flash;
            next.undoBar.gameObject.SetActive(false);

            if (flash)
            {
                next.undoBar.gameObject.SetActive(flash);
                NextStep toThis = next.step;
                next.button.onClick.AddListener(() => InvokeUndo(toThis));
            }
        }
    }

    public void InvokeUndo(NextStep toThisPoint)
    {
        LogText targetText = undosInLog.Find(line => line.step == toThisPoint);

        int counter = 0;
        for (int i = RT.transform.childCount; i > targetText.transform.GetSiblingIndex(); i--)
        {
            Destroy(RT.transform.GetChild(i - 1).gameObject);
            counter++;
        }

        ChangeScrolling();
        scroll.value = 0;

        if (undoToThis != null)
        {
            undoToThis = null;
            //Debug.Log("undo point cancelled");
        }
        DisplayUndoBar(false);

        Popup[] allPopups = FindObjectsByType<Popup>(FindObjectsSortMode.None);
        foreach (Popup popup in allPopups)
            Destroy(popup.gameObject);

        Card[] allCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (Card card in allCards)
        {
            card.button.interactable = false;
            card.button.onClick.RemoveAllListeners();
            card.border.gameObject.SetActive(false);
        }

        for (int i = historyStack.Count - 1; i >= 0; i--)
        {
            NextStep next = historyStack[i];

            if (next.stepType == StepType.Revert)
            {
                (string instruction, object[] parameters) = next.source.TranslateFunction(next.action);

                object[] newParameters = new object[parameters.Length];
                newParameters[0] = true;
                for (int j = 1; j < parameters.Length; j++)
                    newParameters[j] = parameters[j];

                next.source.StringParameters(instruction, newParameters);
                historyStack.RemoveAt(i);
            }
            else if (next.stepType == StepType.UndoPoint)
            {
                Manager.inst.currentPlayer.chainTracker--;

                if (next == toThisPoint || i == 0)
                {
                    if (Manager.inst.currentPlayer.myType == PlayerType.Human)
                    {
                        currentDecisionInStack = -1;
                        Manager.inst.currentPlayer.inReaction.Clear();
                        Manager.inst.currentPlayer.PopStack();
                    }
                    break;
                }
                else
                {
                    historyStack.RemoveAt(i);
                }
            }
        }
    }

    #endregion

#region Steps

    public void RememberStep(PhotonCompatible source, StepType type, Expression<Action> action)
    {
        NextStep newStep = new(source, type, action);
        historyStack.Add(newStep);

        //Debug.Log($"step {currentStep}: {action}");
        if (type != StepType.UndoPoint)
            newStep.action.Compile().Invoke();
    }

    public void ShareSteps()
    {
        StartCoroutine(OnlineShare());

        IEnumerator OnlineShare()
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom.MaxPlayers >= 2)
            {
                foreach (NextStep step in historyStack)
                {
                    if (step.stepType == StepType.Wait)
                    {
                        yield return new WaitForSeconds(waitTime);
                    }
                    else if (step.stepType == StepType.Revert)
                    {
                        (string instruction, object[] parameters) = step.source.TranslateFunction(step.action);
                        DoFunction(() => StepForOthers(step.source.pv.ViewID, instruction, parameters), RpcTarget.Others);
                    }
                }
            }
        }
    }

    [PunRPC]
    void StepForOthers(int PV, string instruction, object[] parameters)
    {
        PhotonCompatible source = PhotonView.Find(PV).GetComponent<PhotonCompatible>();
        source.StringParameters(instruction, parameters);
    }

    [PunRPC]
    internal void ResetHistory()
    {
        historyStack.Clear();
        undosInLog.Clear();
        DisplayUndoBar(false);
        currentDecisionInStack = -1;
    }

    [PunRPC]
    internal void DecisionComplete(bool undo, int stepNumber)
    {
        NextStep step = historyStack[stepNumber];
        if (undo)
        {
            step.completed = false;
            //Debug.Log($"turned off: {stepNumber}, {step.actionName}");
        }
        else
        {
            step.completed = true;
            //Debug.Log($"turned on: {stepNumber}, {step.actionName}");
        }
    }

    public IEnumerator AddWait()
    {
        yield return new WaitForSeconds(waitTime);
        this.RememberStep(this, StepType.Wait, () => WaitFunction());
    }

    [PunRPC]
    void WaitFunction()
    {
    }

    #endregion

}
