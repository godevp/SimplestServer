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
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;
    public List<string> savedAccounts;
    public List<string> RoomNames;
    string savedAccountsFilePath = "savedAccounts.txt";
    [SerializeField] private GameObject prefabRoom;
    [SerializeField] private GameObject GridForRooms;


    //indetifires
    private const int newPlayer = 1;
    private const int turn1 = 111;
    private const int turn2 = 222;
    private const int yourTurn = 333;

    // Start is called before the first frame update
    void Start()
    {
       
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
                    for (int i = 0; i < RoomNames.Count; i++)
                    {
                        string[] dividerForRoom = RoomNames[i].Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                        Debug.Log("diverForRoom[0]: " + dividerForRoom[0] + " splitter[1]: " + splitter[1]);
                        if (dividerForRoom[0] == splitter[1] && dividerForRoom.Length < 3)
                        {
                            canCreate = false;
                            string temp = RoomNames[i];
                            RoomNames.RemoveAt(i);
                            RoomNames.Insert(i, temp + ',' + id.ToString());
                            Debug.Log(RoomNames[i]);
                            //send msg where we tell to join the stage with the game
                            SendMessageToClient("SecondPlayer", id);
                            var z = Random.Range(1, 2);
                            if(z == 1)
                            {

                                SendMessageToClient(turn1.ToString(), id);
                                SendMessageToClient(turn2.ToString(), int.Parse(dividerForRoom[1]));
                            }
                            else if(z == 2)
                            {
                                SendMessageToClient(turn2.ToString(), id);
                                SendMessageToClient(turn1.ToString(), int.Parse(dividerForRoom[1]));
                            }
                            
                            
                            break;
                        }
                        if (dividerForRoom[0] == splitter[1] && dividerForRoom.Length >= 3)
                        {
                            canCreate = false;
                            break;
                        }

                    }
                    if (canCreate)
                    {

                        var newRoom = Instantiate(prefabRoom, GridForRooms.transform);
                        var roomName = newRoom.GetComponentInChildren<TMP_Text>().text = splitter[1];
                        RoomNames.Add(splitter[1] + ',' + id.ToString());
                        SendMessageToClient("FirstPlayer", id);
                    }
                    

                    //here send message back to client which will lead to connection to this room. The Client should change state.
                    break;
                case 777:
                    for (int i = 0; i < RoomNames.Count; i++)
                    {
                        string[] dividerForRoom = RoomNames[i].Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                        if(int.Parse(dividerForRoom[1]) == id )
                        {
                            SendMessageToClient("Loser", int.Parse(dividerForRoom[2]));
                        }
                        else if (int.Parse(dividerForRoom[2]) == id)
                        {
                            SendMessageToClient("Loser", int.Parse(dividerForRoom[1]));
                        }
                    }


                        break;

                case 1111:
                    for (int i = 0; i < RoomNames.Count; i++)
                    {
                        string[] dividerForRoom = RoomNames[i].Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                        if(id == int.Parse(dividerForRoom[1]))
                        {
                            SendMessageToClient(yourTurn.ToString() + ',' + splitter[1], int.Parse(dividerForRoom[2]));
                            Debug.Log(splitter[1] + " <<<<<");
                        }
                        else if(id == int.Parse(dividerForRoom[2]))
                        {
                            SendMessageToClient(yourTurn.ToString() + ',' + splitter[1], int.Parse(dividerForRoom[1]));
                            Debug.Log(splitter[1] + " <<<<<");
                        }
                    }
                        break;
               
                default:
                    break;
            }
        }
        
    }

}