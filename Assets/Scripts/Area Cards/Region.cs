using Photon.Pun;
using UnityEngine;

public class Region : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    [PunRPC]
    internal override void ResolveArea(int playerPosition, int logged)
    {
        base.ResolveArea(playerPosition, logged);
        MyInstructions(Manager.inst.playersInOrder[playerPosition], logged);
    }

    protected override void PostAdvance(Player player, bool success, CardData dataFile, int logged)
    {
        if (success)
        {
            player.ResourceRPC(Resource.Coin, -dataFile.coinAmount, logged);
            MyInstructions(player, logged);
        }
    }

    void MyInstructions(Player player, int logged)
    {
        if (player.resourceDict[Resource.Coin] >= dataFile.coinAmount)
            AskAdvance(player, GetFile(), logged);
    }
}
