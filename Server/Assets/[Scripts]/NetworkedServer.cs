using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using Unity.VisualScripting;
using TMPro;

public struct Ident
{
    public const string LoginApproved = "1";
    public const string LoginDenied = "2";
    public const string Player = "3";
    public const string SecondPlayer = "4";
    public const short Spectator = 5;
    public const short ChatMsg = 6;
    public const string ObsUpdateX = "7";
    public const string ObsUpdateO = "8";
    public const string PlayerUpdateX = "9";
    public const string PlayerUpdateO = "10";
    public const string winner = "11";
    public const string loser = "12";
    public const string tie = "13";
    public const string turn1 = "14";
    public const string turn2 = "15";
    public const short restart = 16;
    public const short stopWatching = 17;
    public const short exit = 18;
    public const short move = 20;
    public const short dscnt = 21;
    public const short room = 22;
    public const short reg = 23;
    public const short logIn = 24;
}



public class NetworkedServer : MonoBehaviour
{
    public static NetworkedServer instance;
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;
    public List<string> savedAccounts;
    string savedAccountsFilePath = "savedAccounts.txt";
    [SerializeField] private GameObject prefabRoom;
    [SerializeField] private GameObject GridForRooms;
    [SerializeField] private List<Room> rooms;

    public List<string> activeAccounts;
    

    

    // Start is called before the first frame update
    void Start()
    {
        activeAccounts = new List<string>();
       instance = this;
        NetworkTransport.Init();
        ConnectionConfig config = new ConnectionConfig();
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);

        var sr = new StreamReader(savedAccountsFilePath);
        string line = "";
        while ((line = sr.ReadLine()) != null)
        {
            savedAccounts.Add(line);
        }
        sr.Close();

    }

    // Update is called once per frame
    void Update()
    {

        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                break;
        }

    }

    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    private void ProcessRecievedMsg(string msg, int id)
    {
        int smth = 0;
        Debug.Log(msg);
        string[] splitter = msg.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        if(int.TryParse(splitter[0],out smth))
        {
            switch (int.Parse(splitter[0]))//check the indetifier of the message
            {
                case Ident.logIn: //Log In
                    bool loginFound = false;
                    foreach (string account in savedAccounts)
                    {
                        string[] splitter2 = account.Split(',', System.StringSplitOptions.RemoveEmptyEntries);

                        if (splitter2[0] == splitter[1] && !activeAccounts.Contains(splitter[1]))
                        {
                            loginFound = true;
                            if (splitter2[1] == splitter[2])
                            {
                                SendMessageToClient(Ident.LoginApproved, id);
                                activeAccounts.Add(splitter[1]);
                            }
                            else
                                SendMessageToClient(Ident.LoginDenied, id);

                        }
                    }
                    if (!loginFound)
                    {
                        SendMessageToClient(Ident.LoginDenied, id);
                    }
                    break;

                case Ident.reg: //Registration

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
                        SendMessageToClient(Ident.LoginApproved, id);

                    }
                    break;

                case Ident.room: //room creation
                    bool canCreate = true;
                    for(int z = 0; z < rooms.Count; z++)
                    {
                        if (rooms[z].name == splitter[1] && rooms[z].id2 == 0)
                        {
                            canCreate = false;
                            rooms[z].id2 = id;
                            SendMessageToClient(Ident.Player, id);
                            rooms[z].setRandomPlayer();
                            break;
                        }
                        if (rooms[z].name == splitter[1] && rooms[z].id1 == 0)
                        {
                            canCreate = false;
                            rooms[z].id1 = id;
                            SendMessageToClient(Ident.Player, id);
                            rooms[z].setRandomPlayer();
                            break;
                        }
                        if (rooms[z].name == splitter[1] && rooms[z].id1 != 0 && rooms[z].id2 != 0)
                        {
                            canCreate = false;
                            break;
                        }
                    }
                    if(canCreate)
                    {
                        var newRoom = Instantiate(prefabRoom, GridForRooms.transform);
                        var roomName = newRoom.GetComponentInChildren<TMP_Text>().text = splitter[1];
                        newRoom.GetComponent<Room>().name = splitter[1];
                        newRoom.GetComponent<Room>().id1 = id;
                        rooms.Add(newRoom.GetComponent<Room>());
                        SendMessageToClient(Ident.Player, id);
                    }
                    break;

                case Ident.stopWatching: //spectator removal
                    foreach (Room _room in rooms)
                    {
                        if (_room.spectatorsList.Contains(id))
                        {
                            _room.spectatorsList.Remove(id);
                        }
                    }
                    break;

                case Ident.Spectator: //spectator adding
                    foreach(Room _room in rooms)
                    {
                        if (_room.name == splitter[1].ToString() && _room.id1 !=0 && _room.id2 != 0)
                        {
                            _room.spectatorsList.Add(id);
                            SendMessageToClient(Ident.Spectator.ToString(), id);
                            _room.UpdateObserver();
                        }
                    }
                    break;

                case Ident.exit: //when players exit the room, we need to delete it
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

                case Ident.restart://restart the game 
                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id || _room.id2 == id)
                        {
                            _room.RestartRoom();
                            break;
                        }
                    }
                    break;

                case Ident.dscnt://when somebody disonnects he's account is open
                    if (activeAccounts.Contains(splitter[1]))
                    {
                        activeAccounts.Remove(splitter[1]);
                    }
                    RoomRemove();
                    break;

                case Ident.ChatMsg://messages in chat

                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id && _room.id2 != 0)
                        {
                            SendMessageToClient(Ident.ChatMsg.ToString() + ',' + "id1 : " + splitter[1], _room.id2);
                            break;
                        }
                        if (_room.id2 == id && _room.id1 != 0)
                        {
                            SendMessageToClient(Ident.ChatMsg.ToString() + ',' + "id2 : " + splitter[1], _room.id1);
                            break;
                        }
                    }
                    break;

                case Ident.move: //After players move
                    foreach(Room _room in rooms)
                    {
                        if(_room.id1 == id || _room.id2 == id)
                        {
                            _room.GameLogicUpdate(int.Parse(splitter[1]),id);
                            break;
                        }
                    }
                    break;


                default:
                    break;
            }
        }
        
    }

    void RoomRemove()
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