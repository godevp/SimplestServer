using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using System.IO;




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

    public List<string> account1Names;
    public List<string> account2Names;
    public string account1File;
    public string account2File;


    private List<int> whoMoved;
    private List<int> whereMoved;
    public bool startTheReplay = false;

    private void Start()
    {
        whoMoved = new List<int>();
        whereMoved = new List<int>();
        account1Names = new List<string>();
        account2Names = new List<string>();
       
    }

    static public void SendMessageToClient(string msg, int clientConnectionID)
    {
        NetworkedServer.instance.SendMessageToClient(msg, clientConnectionID);
    }

    public void SetAccounts(string accountName, int idNumber)
    {
        if(idNumber == 1)
        {
            account1File = accountName + ".txt";
            ReadTheFile(account1File, account1Names);
        }
        if(idNumber == 2)
        {
            account2File = accountName + ".txt";
            ReadTheFile(account2File, account2Names);
        }
    }

    void ReadTheFile(string accountFile, List<string> accountFilesList)
    {
        accountFilesList.Clear();

        if (File.Exists(accountFile))
        {
            var sr = new StreamReader(accountFile);
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                Debug.Log(line);
                accountFilesList.Add(line);
            }
            sr.Close();
        }
    }

    void MovesSaver(int id, int slotNumber)
    {
        if(id == xPlayer)
        {
            whoMoved.Add(1);
        }    
        if(id == oPlayer)
        {
            whoMoved.Add(2);
        }
        whereMoved.Add(slotNumber - 1);
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
                SendMessageToClient(ServerToClientSignifiers.PlayerUpdateX.ToString() + ',' + (slotNumber - 1).ToString(), xPlayer); //that he need to set the buttonSprite to X
                SendMessageToClient(ServerToClientSignifiers.PlayerUpdateX.ToString() + ',' + (slotNumber - 1).ToString(), oPlayer);//that he need to set the buttonSprite to X
                UpdateObserver();

            }
            if (oPlayer == playerId && !slotsTaken[slotNumber - 1])
            {
                slotsByPlayer[slotNumber - 1] = oPlayer;
                slotsTaken[slotNumber - 1] = true;
                MovesSaver(oPlayer, slotNumber);
                SendMessageToClient(ServerToClientSignifiers.PlayerUpdateO.ToString() + ',' + (slotNumber - 1).ToString(), xPlayer); //that he need to set the buttonSprite to O
                SendMessageToClient(ServerToClientSignifiers.PlayerUpdateO.ToString() + ',' + (slotNumber - 1).ToString(), oPlayer);//that he need to set the buttonSprite to O
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
                SendMessageToClient(ServerToClientSignifiers.winner.ToString(),xPlayer);
                SendMessageToClient(ServerToClientSignifiers.loser.ToString(), oPlayer);
                gameOver = true;

                //need to save the game to file
                SaveTheFileForAcc(account1File , account1Names);
                SaveTheFileForAcc(account2File , account2Names);
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
                SendMessageToClient(ServerToClientSignifiers.winner.ToString(), oPlayer);
                SendMessageToClient(ServerToClientSignifiers.loser.ToString(), xPlayer);
                gameOver = true;
                //need to save the game to file
                SaveTheFileForAcc(account1File , account1Names);
                SaveTheFileForAcc(account2File , account2Names);
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
                    SendMessageToClient(ServerToClientSignifiers.tie.ToString(), oPlayer);
                    SendMessageToClient(ServerToClientSignifiers.tie.ToString(), xPlayer);
                    //need to save the game to file
                    SaveTheFileForAcc(account1File, account1Names);
                    SaveTheFileForAcc(account2File, account2Names);
                }
            }
           
        }

     
    }

    void SaveTheFileForAcc(string accFile, List<string> ListOfGames)
    {
        var s = new StreamWriter(System.DateTime.Now.Hour.ToString() + "." + System.DateTime.Now.Minute.ToString() + "." + System.DateTime.Now.Second.ToString() + id1.ToString() + id2.ToString()+ ".txt");
        ListOfGames.Add(System.DateTime.Now.Hour.ToString() + "." + System.DateTime.Now.Minute.ToString() + "." + System.DateTime.Now.Second.ToString() + id1.ToString() + id2.ToString() + ".txt"); 
        int o = 0;
        foreach(int c in whoMoved)
        {
            s.WriteLine(c.ToString() + ',' + whereMoved[o]);
            o++;
        }

        s.Close();

        var sw = new StreamWriter(accFile);
        foreach(string g in ListOfGames)
        {
            sw.WriteLine(g);
        }
        sw.Close();
    }



    public void setRandomPlayer()
    {
        var x = Random.Range(1, 3);
        if (x == 1)
        {
            xPlayer = id1;
            oPlayer = id2;
            SendMessageToClient(ServerToClientSignifiers.turn1.ToString(), id1);
            SendMessageToClient(ServerToClientSignifiers.turn2.ToString(), id2);
        }
        else
        {
            xPlayer = id2;
            oPlayer = id1;
            SendMessageToClient(ServerToClientSignifiers.turn2.ToString(), id1);
            SendMessageToClient(ServerToClientSignifiers.turn1.ToString(), id2);
        }
    }

    public void RestartRoom()
    {
        xPlayer = 0;
        oPlayer = 0;
        whoMoved.Clear();
        whereMoved.Clear();
        gameOver = false;
        for(int i = 0; i < slotsTaken.Count; i++)
        {
            slotsTaken[i] = false;
            slotsByPlayer[i] = 0;
        }
        SendMessageToClient(ServerToClientSignifiers.restart.ToString(), id1);
        SendMessageToClient(ServerToClientSignifiers.restart.ToString(), id2);
        foreach (var obs in spectatorsList)
        { SendMessageToClient(ServerToClientSignifiers.restart.ToString(), obs); }
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
                    SendMessageToClient(ServerToClientSignifiers.ObsUpdateX.ToString() + ',' + i.ToString(), obs);
                }
                else if (slotsByPlayer[i] == oPlayer)
                {
                    SendMessageToClient(ServerToClientSignifiers.ObsUpdateO.ToString() + ',' + i.ToString(), obs);
                }
                
            }
           
        }
    }
}
