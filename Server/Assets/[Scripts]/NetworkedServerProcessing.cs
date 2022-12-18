using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

#region Protocol Signifiers
static public class ClientToServerSignifiers
{
    public const int Spectator = 1;
    public const int ChatMsg = 2;
    public const int restart = 3;
    public const int stopWatching = 4;
    public const int exit = 5;
    public const int move = 6;
    public const int dscnt = 7;
    public const int room = 8;
    public const int reg = 9;
    public const int logIn = 10;
    public const int FeelTheListOfReplays = 11;
    public const int RequestForReplay = 12;
}


static public class ServerToClientSignifiers
{
    public const int LoginApproved = 1;
    public const int LoginDenied = 2;
    public const int Player = 3;
    public const int Spectator = 4;
    public const int ChatMsg = 5;
    public const int ObsUpdateX = 6;
    public const int ObsUpdateO = 7;
    public const int PlayerUpdateX = 8;
    public const int PlayerUpdateO = 9;
    public const int winner = 10;
    public const int loser = 11;
    public const int tie = 12;
    public const int turn1 = 13;
    public const int turn2 = 14;
    public const int restart = 15;
    public const int FeelTheListOfReplays = 16;
    public const int RequestForReplay = 17;
}

#endregion
public class NetworkedServerProcessing : MonoBehaviour
{
    public List<string> activeAccounts;
    public List<string> savedAccounts;
    string savedAccountsFilePath = "savedAccounts.txt";
    [SerializeField] private List<Room> rooms;
    [SerializeField] private GameObject prefabRoom;
    [SerializeField] private GameObject GridForRooms;
    #region instance of that class
    private static NetworkedServerProcessing instance;
    public static NetworkedServerProcessing Instance
    {
        get { return instance; }
    }
    #endregion
    #region Start() + SendMessageToClient()
    private void Start()
    {
        instance = this;
        activeAccounts = new List<string>();
        var sr = new StreamReader(savedAccountsFilePath);
        string line = "";
        while ((line = sr.ReadLine()) != null)
        {
            savedAccounts.Add(line);
        }
        sr.Close();
    }
    static public void SendMessageToClient(string msg, int clientConnectionID)
    {
        NetworkedServer.instance.SendMessageToClient(msg, clientConnectionID);
    }
    #endregion

    #region Receive messages
    public void ReceivedMessageFromClient(string msg, int id)
    {
        int smth = 0;
        Debug.Log(msg);
        string[] splitter = msg.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        if (int.TryParse(splitter[0], out smth))
        {
            switch (int.Parse(splitter[0]))//check the indetifier of the message
            {
                case ClientToServerSignifiers.logIn: //Log In
                    List<string> tempList = new List<string>();
                    foreach (string activeAcc in activeAccounts)
                    {
                        string[] actAccSplit = activeAcc.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                        tempList.Add(actAccSplit[0]);
                    }
                    bool loginFound = false;
                    foreach (string account in savedAccounts)
                    {
                        string[] splitter2 = account.Split(',', System.StringSplitOptions.RemoveEmptyEntries);


                        if (splitter2[0] == splitter[1] && !tempList.Contains(splitter[1]))
                        {

                            loginFound = true;
                            if (splitter2[1] == splitter[2])
                            {
                                Debug.Log(splitter[1]);
                                SendMessageToClient(ServerToClientSignifiers.LoginApproved.ToString(), id);
                                activeAccounts.Add(splitter[1] + ',' + id.ToString());
                                UpdatePlayersListOfReplays(splitter[1], id);
                            }
                            else
                                SendMessageToClient(ServerToClientSignifiers.LoginDenied.ToString(), id);

                        }
                    }
                    if (!loginFound)
                    {
                        SendMessageToClient(ServerToClientSignifiers.LoginDenied.ToString(), id);
                    }
                    break;

                case ClientToServerSignifiers.reg: //Registration

                    bool accountAlreadyExists = false;

                    foreach (string account in savedAccounts)
                    {
                        string[] splitter2 = account.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                        if (splitter2[0] == splitter[1])
                            accountAlreadyExists = true;
                    }
                    if (!accountAlreadyExists)
                    {
                        //Here we need to create saving of the new account
                        string newAcc = splitter[1] + ',' + splitter[2];
                        savedAccounts.Add(newAcc);
                        File.Delete(savedAccountsFilePath);
                        var sw = new StreamWriter(savedAccountsFilePath);
                        foreach (string account in savedAccounts)
                        {
                            sw.WriteLine(account);
                        }

                        sw.Close();
                        //here send back message that access achieved
                        SendMessageToClient(ServerToClientSignifiers.LoginApproved.ToString(), id);
                        activeAccounts.Add(splitter[1] + ',' + id.ToString());
                        UpdatePlayersListOfReplays(splitter[1], id);

                    }
                    break;

                case ClientToServerSignifiers.room: //room creation
                    bool canCreate = true;
                    for (int z = 0; z < rooms.Count; z++)
                    {
                        if (rooms[z].name == splitter[1] && rooms[z].id2 == 0)
                        {
                            canCreate = false;
                            rooms[z].id2 = id;
                            SendMessageToClient(ServerToClientSignifiers.Player.ToString(), id);
                            rooms[z].setRandomPlayer();
                            SetPlayerForRoom(id, 2, rooms[z]);
                            SetPlayerForRoom(rooms[z].id1, 1, rooms[z]);
                            break;
                        }
                        if (rooms[z].name == splitter[1] && rooms[z].id1 == 0)
                        {
                            canCreate = false;
                            rooms[z].id1 = id;
                            SendMessageToClient(ServerToClientSignifiers.Player.ToString(), id);
                            rooms[z].setRandomPlayer();
                            SetPlayerForRoom(id, 1, rooms[z]);
                            break;
                        }
                        if (rooms[z].name == splitter[1] && rooms[z].id1 != 0 && rooms[z].id2 != 0)
                        {
                            canCreate = false;
                            break;
                        }
                    }
                    if (canCreate)
                    {
                        var newRoom = Instantiate(prefabRoom, GridForRooms.transform);
                        var roomName = newRoom.GetComponentInChildren<TMP_Text>().text = splitter[1];
                        newRoom.GetComponent<Room>().name = splitter[1];
                        newRoom.GetComponent<Room>().id1 = id;


                        rooms.Add(newRoom.GetComponent<Room>());
                        SendMessageToClient(ServerToClientSignifiers.Player.ToString(), id);
                    }
                    break;

                case ClientToServerSignifiers.stopWatching: //spectator removal
                    foreach (Room _room in rooms)
                    {
                        if (_room.spectatorsList.Contains(id))
                        {
                            _room.spectatorsList.Remove(id);
                        }
                    }
                    break;

                case ClientToServerSignifiers.Spectator: //spectator adding
                    foreach (Room _room in rooms)
                    {
                        if (_room.name == splitter[1].ToString() && _room.id1 != 0 && _room.id2 != 0)
                        {
                            _room.spectatorsList.Add(id);
                            SendMessageToClient(ServerToClientSignifiers.Spectator.ToString(), id);
                            _room.UpdateObserver();
                        }
                    }
                    break;

                case ClientToServerSignifiers.exit: //when players exit the room, we need to delete it
                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id)
                        {
                            _room.id1 = 0;
                            break;
                        }
                        else if (_room.id2 == id)
                        {
                            _room.id2 = 0;
                            break;
                        }
                    }
                    RoomRemove();
                    break;

                case ClientToServerSignifiers.restart://restart the game 
                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id || _room.id2 == id)
                        {
                            _room.RestartRoom();
                            break;
                        }
                    }
                    break;

                case ClientToServerSignifiers.dscnt://when somebody disonnects he's account is open

                    if (activeAccounts.Contains(splitter[1]) && splitter.Length > 1)
                    {
                        activeAccounts.Remove(splitter[1]);
                    }
                    for (int i = 0; i < activeAccounts.Count; i++)
                    {
                        string[] spl = activeAccounts[i].Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                        if (spl[0] == splitter[1])
                        {
                            activeAccounts.RemoveAt(i);
                        }
                    }
                    RoomRemove();
                    break;

                case ClientToServerSignifiers.ChatMsg://messages in chat

                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id && _room.id2 != 0)
                        {
                            SendMessageToClient(ServerToClientSignifiers.ChatMsg.ToString() + ',' + "id1 : " + splitter[1], _room.id2);
                            break;
                        }
                        if (_room.id2 == id && _room.id1 != 0)
                        {
                            SendMessageToClient(ServerToClientSignifiers.ChatMsg.ToString() + ',' + "id2 : " + splitter[1], _room.id1);
                            break;
                        }
                    }
                    break;

                case ClientToServerSignifiers.move: //After players move
                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id || _room.id2 == id)
                        {
                            _room.GameLogicUpdate(int.Parse(splitter[1]), id);
                            break;
                        }
                    }
                    break;

                case ClientToServerSignifiers.RequestForReplay:
                    List<string> templist = new List<string>();
                    if (splitter.Length > 1 && File.Exists(splitter[1]))
                    {
                        var sr = new StreamReader(splitter[1]);
                        string line1 = "";
                        while ((line1 = sr.ReadLine()) != null)
                        {
                            if (!(templist.Contains(line1)))
                                templist.Add(line1);
                        }
                        sr.Close();
                    }

                    StartCoroutine(SendReplay(1.5f, templist, id));
                    break;

                case ClientToServerSignifiers.FeelTheListOfReplays:
                    foreach (string x in activeAccounts)
                    {
                        string[] spl = x.Split(',');
                        if (spl[1] == id.ToString())
                        {
                            UpdatePlayersListOfReplays(spl[0], id);
                            break;
                        }
                    }

                    break;

                default:
                    break;
            }
        }
    }
    #endregion

    #region supporting functions
    IEnumerator SendReplay(float delay, List<string> tempListt, int _id)
    {
        yield return new WaitForSeconds(delay);

        List<string> tempList2 = new List<string>();
        foreach (string acc in activeAccounts)
        {
            string[] sp = acc.Split(',');
            tempList2.Add(sp[1]);
        }
        if (tempListt.Count > 0 && tempList2.Contains(_id.ToString()))
        {
            SendMessageToClient(ServerToClientSignifiers.RequestForReplay.ToString() + ',' + tempListt[0], _id);
            tempListt.RemoveAt(0);

            StartCoroutine(SendReplay(delay, tempListt, _id));
            if (tempListt.Count == 0)
            {
                SendMessageToClient(ServerToClientSignifiers.RequestForReplay.ToString() + ',' + "obsExit", _id);
            }
        }

    }
    void SetPlayerForRoom(int id, int playerNumber, Room _room)
    {
        foreach (string acc in activeAccounts)
        {
            string[] accSplitter = acc.Split(',');
            if (accSplitter[1] == id.ToString())
            {
                _room.SetAccounts(accSplitter[0], playerNumber);
            }
        }
    }
    void RoomRemove()
    {
        if (rooms.Count > 0)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].id1 == 0 && rooms[i].id2 == 0)
                {
                    Destroy(rooms[i].gameObject);
                    rooms.RemoveAt(i);
                    i--;
                    break;
                }
            }
        }

    }

    static public void UpdatePlayersListOfReplays(string playerLogin, int playerID)
    {
        SendMessageToClient(ServerToClientSignifiers.FeelTheListOfReplays.ToString() + ',' + "clean", playerID);

        if (File.Exists(playerLogin + ".txt"))
        {

            var sr = new StreamReader(playerLogin + ".txt");
            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                SendMessageToClient(ServerToClientSignifiers.FeelTheListOfReplays.ToString() + ',' + line, playerID);
            }
            SendMessageToClient(ServerToClientSignifiers.FeelTheListOfReplays.ToString() + ',' + "done", playerID);
            sr.Close();

        }

    }
    #endregion
}
