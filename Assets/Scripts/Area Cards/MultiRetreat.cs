using UnityEngine;

public class MultiRetreat : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        if (!player.BoolFromAbilities(true, nameof(IgnoreArea), IgnoreArea.CheckParameters(), logged))
        {
            int area2 = player.CalcTroopScout(1).Item1;
            int area3 = player.CalcTroopScout(2).Item1;

            if (area2 > area3)
            {
                for (int i = 0; i<area2-area3; i++)
                    player.MoveTroopRPC(1, 0, logged);
            }
            else if (area3 > area2)
            {
                for (int i = 0; i < area3 - area2; i++)
                    player.MoveTroopRPC(2, 0, logged);
            }
        }
    }
}
