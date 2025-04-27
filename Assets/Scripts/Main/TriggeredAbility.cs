using System;

public enum WhenDelete { UntilCamp, Manually }

[Serializable]
public class TriggeredAbility
{
    public PhotonCompatible source { get; protected set; }
    protected string comparison;
    protected Func<string, object[], bool> CanBeTriggered;
    public WhenDelete deletion { get; private set; }

    protected Action<int, object[]> WhenTriggered;
    protected Func<int, object[], int> GetNumber;
    protected Func<int, object[], bool> GetBool;

    protected TriggeredAbility(PhotonCompatible source, WhenDelete deletion, Action<int, object[]> voidAbility, Func<string, object[], bool> condition = null)
    {
        this.source = source;
        this.deletion = deletion;
        CanBeTriggered = condition;
        WhenTriggered = voidAbility;
    }

    protected TriggeredAbility(PhotonCompatible source, WhenDelete deletion, Func<int, object[], int> numberAbility, Func<string, object[], bool> condition = null)
    {
        this.source = source;
        this.deletion = deletion;
        CanBeTriggered = condition;
        GetNumber = numberAbility;
    }

    protected TriggeredAbility(PhotonCompatible source, WhenDelete deletion, Func<int, object[], bool> boolAbility, Func<string, object[], bool> condition = null)
    {
        this.source = source;
        this.deletion = deletion;
        CanBeTriggered = condition;
        GetBool = boolAbility;
    }

    public bool CheckAbility(string condition, object[] parameters = null)
    {
        try
        {
            if (CanBeTriggered != null)
                return CanBeTriggered(condition, parameters) && comparison == condition;
            else
                return comparison == condition;
        }
        catch
        {
            return false;
        }
    }

    public void ResolveAbility(int logged, object[] parameters = null)
    {
        WhenTriggered(logged, parameters);
    }

    public bool BoolAbility(int logged, object[] parameters = null)
    {
        return GetBool(logged, parameters);
    }

    public int NumberAbility(int logged, object[] parameters = null)
    {
        return GetNumber(logged, parameters);
    }
}

public class ControlArea : TriggeredAbility
{
    public ControlArea(PhotonCompatible source, WhenDelete deletion, Func<int, object[], bool> boolAbility, Func<string, object[], bool> condition = null) : base(source, deletion, boolAbility, condition)
    {
        comparison = nameof(ControlArea);
    }

    public static object[] CheckParameters(int area)
    {
        return new object[1] { area };
    }

    public static int ConvertParameters(object[] array)
    {
        return (int)array[0];
    }
}

public class IgnoreArea : TriggeredAbility
{
    public IgnoreArea(PhotonCompatible source, WhenDelete deletion, Func<int, object[], bool> boolAbility, Func<string, object[], bool> condition = null) : base(source, deletion, boolAbility, condition)
    {
        comparison = nameof(IgnoreArea);
    }

    public static object[] CheckParameters()
    {
        return new object[0];
    }
}