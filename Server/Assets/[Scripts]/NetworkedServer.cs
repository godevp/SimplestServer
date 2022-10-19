using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;
    int socketPort = 5491;
    public List<string> savedAccounts;
    string savedAccountsFilePath = "savedAccounts.txt";

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
        Debug.Log(msg);
        string[] splitter = msg.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        if(int.Parse(splitter[0]) == 0)
        {
            bool loginFound = false;
            //Log In
            foreach (string account in savedAccounts)
            {
                string[] splitter2 = account.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                
                if(splitter2[0] == splitter[1])
                {
                    Debug.Log("Login found");
                    loginFound = true;
                    if(splitter2[1] == splitter[2])
                    {
                        Debug.Log("Access opened");
                        SendMessageToClient("LoginApproved", id);
                    }
                    else
                    {
                        Debug.Log("Bad password");
                        SendMessageToClient("LoginDenied", id);
                    }
                }
            }
            if(!loginFound)
            {
                Debug.Log("Login not found");
                SendMessageToClient("LoginDenied", id);
            }
        }
        else if(int.Parse(splitter[0]) == 1)
        {
            //Registration
            bool accountAlreadyExists = false;

            foreach(string account in savedAccounts)
            {
                string[] splitter2 = account.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
                if (splitter2[0] == splitter[1])
                    accountAlreadyExists = true;
            }
            if(!accountAlreadyExists)
            {
                //Here we need to create saving of the new account
                string newAcc = splitter[1] + ',' + splitter[2];
                savedAccounts.Add(newAcc);
                File.Delete(savedAccountsFilePath);
                var sw = new StreamWriter(savedAccountsFilePath);
                foreach(string account in savedAccounts)
                {
                    sw.WriteLine(account);
                }

                sw.Close();
                Debug.Log("new account added");
                //here send back message that access achieved

            }
        }
       

       // Debug.Log("msg recieved = " + msg + ".  connection id = " + id);
    }

}