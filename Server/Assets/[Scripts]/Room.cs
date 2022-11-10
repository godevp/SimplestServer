using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class Room : MonoBehaviour
{
    public string name;
    public int id1;
    public int id2;
    public int xPlayer;
    public int oPlayer;
    public List<int> spectatorsList;
    private const int Xmsg = 999;
    private const int Omsg = 888;
    private const int winner = 7777;
    private const int loser = 6666;
    private const int tie = 3333;
    private const int turn1 = 111;
    private const int turn2 = 222;
    public bool gameOver = false;
    [SerializeField] private List<bool> slotsTaken;
    [SerializeField] private List<int> slotsByPlayer;




    public void GameLogicUpdate(int slotNumber, int playerId)
    {
        if (!gameOver)
        {
            if (xPlayer == playerId && !slotsTaken[slotNumber - 1])
            {
                
                slotsByPlayer[slotNumber - 1] = xPlayer;
                slotsTaken[slotNumber - 1] = true;
                NetworkedServer.instance.SendMessageToClient(Xmsg.ToString() + ',' + (slotNumber - 1).ToString(), xPlayer); //that he need to set the buttonSprite to X
                NetworkedServer.instance.SendMessageToClient(Xmsg.ToString() + ',' + (slotNumber - 1).ToString(), oPlayer);//that he need to set the buttonSprite to X
                UpdateObserver();

            }
            if (oPlayer == playerId && !slotsTaken[slotNumber - 1])
            {
                slotsByPlayer[slotNumber - 1] = oPlayer;
                slotsTaken[slotNumber - 1] = true;
                NetworkedServer.instance.SendMessageToClient(Omsg.ToString() + ',' + (slotNumber - 1).ToString(), xPlayer); //that he need to set the buttonSprite to O
                NetworkedServer.instance.SendMessageToClient(Omsg.ToString() + ',' + (slotNumber - 1).ToString(), oPlayer);//that he need to set the buttonSprite to O
                UpdateObserver();
            }

            if ((slotsByPlayer[0] == xPlayer && slotsByPlayer[1] == xPlayer && slotsByPlayer[2] == xPlayer) ||
                  (slotsByPlayer[3] == xPlayer && slotsByPlayer[4] == xPlayer && slotsByPlayer[5] == xPlayer) ||
                  (slotsByPlayer[6] == xPlayer && slotsByPlayer[7] == xPlayer && slotsByPlayer[8] == xPlayer) ||
                  (slotsByPlayer[0] == xPlayer && slotsByPlayer[3] == xPlayer && slotsByPlayer[6] == xPlayer) ||
                  (slotsByPlayer[2] == xPlayer && slotsByPlayer[5] == xPlayer && slotsByPlayer[8] == xPlayer) ||
                  (slotsByPlayer[2] == xPlayer && slotsByPlayer[4] == xPlayer && slotsByPlayer[6] == xPlayer) ||
                  (slotsByPlayer[1] == xPlayer && slotsByPlayer[4] == xPlayer && slotsByPlayer[7] == xPlayer) ||
                  (slotsByPlayer[0] == xPlayer && slotsByPlayer[4] == xPlayer && slotsByPlayer[8] == xPlayer))
            {
                //send xplayer winner and oplayer loser
                NetworkedServer.instance.SendMessageToClient(winner.ToString(),xPlayer);
                NetworkedServer.instance.SendMessageToClient(loser.ToString(), oPlayer);
                gameOver = true;
            }
            else if ((slotsByPlayer[0] == oPlayer && slotsByPlayer[1] == oPlayer && slotsByPlayer[2] == oPlayer) ||
                  (slotsByPlayer[3] == oPlayer && slotsByPlayer[4] == oPlayer && slotsByPlayer[5] == oPlayer) ||
                  (slotsByPlayer[6] == oPlayer && slotsByPlayer[7] == oPlayer && slotsByPlayer[8] == oPlayer) ||
                  (slotsByPlayer[0] == oPlayer && slotsByPlayer[3] == oPlayer && slotsByPlayer[6] == oPlayer) ||
                  (slotsByPlayer[2] == oPlayer && slotsByPlayer[5] == oPlayer && slotsByPlayer[8] == oPlayer) ||
                  (slotsByPlayer[2] == oPlayer && slotsByPlayer[4] == oPlayer && slotsByPlayer[6] == oPlayer) ||
                  (slotsByPlayer[1] == oPlayer && slotsByPlayer[4] == oPlayer && slotsByPlayer[7] == oPlayer) ||
                  (slotsByPlayer[0] == oPlayer && slotsByPlayer[4] == oPlayer && slotsByPlayer[8] == oPlayer))
            {
                //send oPlayer winner and xplayer loser
                NetworkedServer.instance.SendMessageToClient(winner.ToString(), oPlayer);
                NetworkedServer.instance.SendMessageToClient(loser.ToString(), xPlayer);
                gameOver = true;
            }
            
            if (!gameOver)
            {
                bool allTaken = true;
                foreach (var b in slotsTaken)
                {
                    if (!b)
                    {
                        allTaken = false;
                        break;
                    }
                }
                if (allTaken)
                {
                    NetworkedServer.instance.SendMessageToClient(tie.ToString(), oPlayer);
                    NetworkedServer.instance.SendMessageToClient(tie.ToString(), xPlayer);
                }
            }
           
        }

        MovesSaver();
    }



    void MovesSaver()
    {

    }




    public void setRandomPlayer()
    {
        var x = Random.Range(1, 3);
        if (x == 1)
        {
            xPlayer = id1;
            oPlayer = id2;
            NetworkedServer.instance.SendMessageToClient(turn1.ToString(), id1);
            NetworkedServer.instance.SendMessageToClient(turn2.ToString(), id2);
        }
        else
        {
            xPlayer = id2;
            oPlayer = id1;
            NetworkedServer.instance.SendMessageToClient(turn2.ToString(), id1);
            NetworkedServer.instance.SendMessageToClient(turn1.ToString(), id2);
        }
    }

    public void RestartRoom()
    {
        xPlayer = 0;
        oPlayer = 0;
        gameOver = false;
        for(int i = 0; i < slotsTaken.Count; i++)
        {
            slotsTaken[i] = false;
            slotsByPlayer[i] = 0;
        }
        NetworkedServer.instance.SendMessageToClient(123.ToString(), id1);
        NetworkedServer.instance.SendMessageToClient(123.ToString(), id2);
        foreach (var obs in spectatorsList)
        { NetworkedServer.instance.SendMessageToClient(123.ToString(), obs); }
            setRandomPlayer();
        
    }
    public void UpdateObserver()
    {
        foreach (var obs in spectatorsList)
        {
            for(int i = 0; i < slotsByPlayer.Count; i++)
            {
                if (slotsByPlayer[i] == xPlayer)
                {
                    NetworkedServer.instance.SendMessageToClient("6" + ',' + i.ToString(), obs);
                }
                else if (slotsByPlayer[i] == oPlayer)
                {
                    NetworkedServer.instance.SendMessageToClient("7" + ',' + i.ToString(), obs);
                }
                
            }
           
        }
    }
}
