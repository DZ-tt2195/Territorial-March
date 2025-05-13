using UnityEngine;

public class Debt : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        AddAbilityRPC(player);
    }

    protected override TriggeredAbility SetupAbility(Player player)
    {
        StartCamp campAbility = new(this, WhenDelete.OnceUsed, PayDebt);
        return campAbility;

        void PayDebt(int logged, object[] parameters)
        {
            Player player = StartCamp.ConvertParameters(parameters);
            player.ResourceRPC(Resource.Coin, -1 * dataFile.coinAmount, logged, this.name);
        }
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
            mathResult = dataFile.startingCoin;
        recalculate = false;
    }
}
