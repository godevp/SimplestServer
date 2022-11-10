using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using Unity.VisualScripting;
using TMPro;

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


    //indetifires
    private const int newPlayer = 1;
    

    // Start is called before the first frame update
    void Start()
    {
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
                case 0: //Log In
                    bool loginFound = false;
                    foreach (string account in savedAccounts)
                    {
                        string[] splitter2 = account.Split(',', System.StringSplitOptions.RemoveEmptyEntries);

                        if (splitter2[0] == splitter[1])
                        {
                            Debug.Log("Login found");
                            loginFound = true;
                            if (splitter2[1] == splitter[2])
                                SendMessageToClient("LoginApproved", id);
                            else
                                SendMessageToClient("LoginDenied", id);

                        }
                    }
                    if (!loginFound)
                    {
                        Debug.Log("Login not found");
                        SendMessageToClient("LoginDenied", id);
                    }
                    break;
                case 1: //Registration

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
                        Debug.Log("new account added");
                        //here send back message that access achieved
                        SendMessageToClient("LoginApproved", id);

                    }
                    break;

                case 2:
                    bool canCreate = true;
                    for(int z = 0; z < rooms.Count; z++)
                    {
                        if (rooms[z].name == splitter[1] && rooms[z].id2 == 0)
                        {
                            canCreate = false;
                            rooms[z].id2 = id;
                            SendMessageToClient("SecondPlayer", id);
                            rooms[z].setRandomPlayer();
                            break;
                        }
                        if (rooms[z].name == splitter[1] && rooms[z].id1 == 0)
                        {
                            canCreate = false;
                            rooms[z].id1 = id;
                            SendMessageToClient("SecondPlayer", id);
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
                        SendMessageToClient("FirstPlayer", id);
                    }
                    break;
                case 1111:
                    foreach(Room _room in rooms)
                    {
                        if(_room.id1 == id || _room.id2 == id)
                        {
                            _room.GameLogicUpdate(int.Parse(splitter[1]),id);
                            break;
                        }
                    }
                    break;


                case 96:
                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id || _room.id2 == id)
                        {
                            _room.RestartRoom();
                            break;
                        }
                    }
                    break;
                case 69:
                    foreach (Room _room in rooms)
                    {
                        if (_room.id1 == id)
                        {
                            _room.id1 = 0;
                            break;
                        }
                        else if(_room.id2 == id)
                        {
                            _room.id2 = 0;
                            break;
                        }
                    }
                    for(int i = 0; i < rooms.Count; i++)
                    {
                        if(rooms[i].id1 == 0 && rooms[i].id2 == 0)
                        {
                            Destroy(rooms[i].gameObject);
                            rooms.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        
    }

}