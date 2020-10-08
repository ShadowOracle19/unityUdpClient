using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using UnityEngine.UIElements;
using System.Drawing;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;//an instance of the UDP client
    //public GameObject playerGO; //our player object
    void Start()
    {
        //connect to the client
        //all this is explained in week 1-4 slides
        udp = new UdpClient();
        udp.Connect("18.223.109.241", 12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        float repeatThing = 1.0f / 30.0f;

        InvokeRepeating("HeartBeat", repeatThing, repeatThing);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }

    public enum commands
    {
        NEW_CLIENT,
        GAME_UPDATE,
        PLAYER_DISCONNECTED,
        CLIENT_ID
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
    }

    [Serializable]
    public class Player
    {
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }

        public string id;
        public receivedColor color;
        public Vector3 pos;
        public bool init = true;
        public GameObject cube = null;
    }

    [Serializable]
    public class NewPlayer
    {
        public Player newPlayer;
    }

    [Serializable]
    public class PlayerLeft
    {
        public Player playerLeft;
    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    public struct playerUniqueID
    {
        public string playerID;
    }

    playerUniqueID uniqueID;

    public struct PlayerData
    {
        public Vector3 playerLoc;
        public string heartbeat;
    }

    public PlayerData playerData;
    public Message latestMessage;
    public GameState lastestGameState;
    public NewPlayer ConnectingPlayer;
    public PlayerLeft lastestPlayerLeft;

    public List<Player> PlayerList;

    public bool connectingPlayerSpawned = false;

    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);

        Debug.Log(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    ConnectingPlayer = JsonUtility.FromJson<NewPlayer>(returnData);
                    connectingPlayerSpawned = true;
                    break;
                case commands.GAME_UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.PLAYER_DISCONNECTED:
                    lastestPlayerLeft = JsonUtility.FromJson<PlayerLeft>(returnData);
                    break;
                case commands.CLIENT_ID:
                    uniqueID = JsonUtility.FromJson<playerUniqueID>(returnData);
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
        if (connectingPlayerSpawned)
        {
            // Debug.Log(lastestNewPlayer.newPlayer.id);
            PlayerList.Add(ConnectingPlayer.newPlayer);
            PlayerList.Last().cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            PlayerList.Last().cube.AddComponent<PlayerCybeBehvaiour>();


            connectingPlayerSpawned = false;
        }
    }

    void UpdatePlayers()
    {
        for(int i = 0; i < lastestGameState.players.Length; i++)
        {
            for(int k = 0; k < PlayerList.Count(); k++)
            {
                if(lastestGameState.players[i].id == PlayerList[k].id)
                {
                    PlayerList[k].cube.GetComponent<PlayerCybeBehvaiour>().playerRef = PlayerList[k];
                    PlayerList[k].color = lastestGameState.players[i].color;
                    PlayerList[k].cube.transform.position = lastestGameState.players[i].pos;
                }
                
            }
            
        }
    }

    void DestroyPlayers()
    {
        foreach(Player player in PlayerList)
        {
            if(player.id == lastestPlayerLeft.playerLeft.id)
            {
                PlayerList.Remove(player);
            }
        }
    }

    void HeartBeat()
    {
        playerData.playerLoc = new Vector3(0.0f, 0.0f, 0.0f);

        foreach(Player player in PlayerList)
        {
            if(player.id == uniqueID.playerID)
            {
                playerData.playerLoc = player.cube.transform.position;
                continue;
            }
        }
        playerData.heartbeat = "heartbeat";

        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
