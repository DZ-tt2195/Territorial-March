using System.Collections.Generic;
using UnityEngine;

public class Secure : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        AddScout(player, GetFile(), logged);
    }

    protected override List<int> CanAdd(Player player)
    {
        List<int> controlThis = new();
        for (int i = 0; i < 4; i++)
        {
            if (player.areasControlled[i])
                controlThis.Add(i);
        }
        return controlThis;
    }
}
