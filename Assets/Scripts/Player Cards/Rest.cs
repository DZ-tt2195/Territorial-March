using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

public class Rest : PlayerCard
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
        IgnoreArea ignoreAbility = new(this, WhenDelete.UntilCamp, Ignore);
        return ignoreAbility;

        bool Ignore(int logged, object[] parameters)
        {
            Log.inst.AddTextRPC(player, $"{player.name} ignores this Area ({this.name}).", LogAdd.Personal, logged);
            return true;
        }
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
            mathResult = dataFile.startingCoin;
        recalculate = false;
    }
}
