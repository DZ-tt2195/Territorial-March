using UnityEngine;

public class Switcheroo : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        (int area2Troop, int area2Scout) = player.CalcTroopScout(1);
        (int area3Troop, int area3Scout) = player.CalcTroopScout(2);

        Log.inst.AddTextRPC(player, $"{player.name} moves {area3Troop} Troop and {area3Scout} Scout to Area 2.", LogAdd.Remember, logged);
        Log.inst.AddTextRPC(player, $"{player.name} moves {area2Troop} Troop and {area2Scout} Scout to Area 3.", LogAdd.Remember, logged);

        for (int i = 0; i < area2Troop; i++)
            player.MoveTroopRPC(1, 2, -1);
        for (int i = 0; i < area3Troop; i++)
            player.MoveTroopRPC(2, 1, -1);

        player.ChangeScoutRPC(1, area3Scout-area2Scout, -1);
        player.ChangeScoutRPC(2, area2Scout-area3Scout, -1);
    }

    public override int DoMath(Player player)
    {
        return this.dataFile.coinBonus;
    }
}
