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
    public bool gameOver = false;
    [SerializeField] private List<bool> slotsTaken;
    [SerializeField] private List<int> slotsByPlayer;


    private List<int> whoMoved;
    private List<int> whereMoved;
    public bool startTheReplay = false;

    private void Start()
    {
        whoMoved = new List<int>();
        whereMoved = new List<int>();

        StartCoroutine(MyUpdate(1.5f));//used for replay
    }

    private IEnumerator MyUpdate(float delay)
    {

        if(startTheReplay)
        {
            //make the logic here for the replay
             
        }
        yield return new WaitForSeconds(delay);
        StartCoroutine(MyUpdate(delay));
    }

    public void GameLogicUpdate(int slotNumber, int playerId)
    {
        if (!gameOver)
        {
            if (xPlayer == playerId && !slotsTaken[slotNumber - 1])
            {
                
                slotsByPlayer[slotNumber - 1] = xPlayer;
                slotsTaken[slotNumber - 1] = true;
                MovesSaver(xPlayer, slotNumber);
                NetworkedServer.instance.SendMessageToClient(Ident.PlayerUpdateX + ',' + (slotNumber - 1).ToString(), xPlayer); //that he need to set the buttonSprite to X
                NetworkedServer.instance.SendMessageToClient(Ident.PlayerUpdateX + ',' + (slotNumber - 1).ToString(), oPlayer);//that he need to set the buttonSprite to X
                UpdateObserver();

            }
            if (oPlayer == playerId && !slotsTaken[slotNumber - 1])
            {
                slotsByPlayer[slotNumber - 1] = oPlayer;
                slotsTaken[slotNumber - 1] = true;
                MovesSaver(oPlayer, slotNumber);
                NetworkedServer.instance.SendMessageToClient(Ident.PlayerUpdateO + ',' + (slotNumber - 1).ToString(), xPlayer); //that he need to set the buttonSprite to O
                NetworkedServer.instance.SendMessageToClient(Ident.PlayerUpdateO + ',' + (slotNumber - 1).ToString(), oPlayer);//that he need to set the buttonSprite to O
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
                NetworkedServer.instance.SendMessageToClient(Ident.winner,xPlayer);
                NetworkedServer.instance.SendMessageToClient(Ident.loser, oPlayer);
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
                NetworkedServer.instance.SendMessageToClient(Ident.winner, oPlayer);
                NetworkedServer.instance.SendMessageToClient(Ident.loser, xPlayer);
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
                    NetworkedServer.instance.SendMessageToClient(Ident.tie, oPlayer);
                    NetworkedServer.instance.SendMessageToClient(Ident.tie, xPlayer);
                }
            }
           
        }

     
    }


    public void Replay()
    {
        for(int i = 0; i < whoMoved.Count;i++)
        {
            StartCoroutine(myTimer(2,i));
            i--;
        }
 
    }
    private IEnumerator myTimer(float timer, int index)
    {
        yield return new WaitForSeconds(timer);
    }
 
    void MovesSaver(int id, int slotNumber)
    {
        whoMoved.Add(id);
        whereMoved.Add(slotNumber - 1);
    }


    public void setRandomPlayer()
    {
        var x = Random.Range(1, 3);
        if (x == 1)
        {
            xPlayer = id1;
            oPlayer = id2;
            NetworkedServer.instance.SendMessageToClient(Ident.turn1, id1);
            NetworkedServer.instance.SendMessageToClient(Ident.turn2, id2);
        }
        else
        {
            xPlayer = id2;
            oPlayer = id1;
            NetworkedServer.instance.SendMessageToClient(Ident.turn2, id1);
            NetworkedServer.instance.SendMessageToClient(Ident.turn1, id2);
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
        NetworkedServer.instance.SendMessageToClient(Ident.restart.ToString(), id1);
        NetworkedServer.instance.SendMessageToClient(Ident.restart.ToString(), id2);
        foreach (var obs in spectatorsList)
        { NetworkedServer.instance.SendMessageToClient(Ident.restart.ToString(), obs); }
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
                    NetworkedServer.instance.SendMessageToClient(Ident.ObsUpdateX + ',' + i.ToString(), obs);
                }
                else if (slotsByPlayer[i] == oPlayer)
                {
                    NetworkedServer.instance.SendMessageToClient(Ident.ObsUpdateO + ',' + i.ToString(), obs);
                }
                
            }
           
        }
    }
}
