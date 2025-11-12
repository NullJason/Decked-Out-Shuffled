using System.Collections;
using UnityEngine;

public class PlayerName : EventAction
{
    public override void DoEventAction()
    {
        // TODO display input ui for inputting name which will then apply that name to the player.
        Player.PlayerCanMove = false;
    }
}
