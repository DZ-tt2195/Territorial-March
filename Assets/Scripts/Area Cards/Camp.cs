using Photon.Pun;
using UnityEngine;

public class Camp : AreaCard
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
        Player player = Manager.inst.playersInOrder[playerPosition];

        player.DrawCardRPC(dataFile.cardAmount, logged);
        player.ResourceRPC(Resource.Play, dataFile.playAmount, logged);
        MyInstructions(player, logged);
    }

    protected override void PostPlaying(Player player, PlayerCard cardToPlay, CardData dataFile, int logged)
    {
        if (cardToPlay != null)
        {
            player.ResourceRPC(Resource.Play, -1, -1);
            MyInstructions(player, logged);
        }
    }

    void MyInstructions(Player player, int logged)
    {
        if (player.resourceDict[Resource.Play] >= 1)
            PlayCard(player, GetFile(), logged);
    }
}
