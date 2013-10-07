﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player {
	public GameObject avatar;
	public NetworkPlayer playerInfo;
	public float playerHealth;
	public string name;
	public int score;
}

public class NetworkManager : MonoBehaviour {

	public GameObject playerPrefab;

	// NETWORK CONSTANTS
	const int DEFAULT_PORT = 31337;
	const int MAX_CONNECTIONS = 16;
	public List<Player> otherPlayers;
	public Player my;
	private int killsToWin;
	private guiGame mainGUI;
	private GameManager gameManager;
	private PlayerWeapons weaponController;
	private static NetworkManager instance = null;
	
	public static NetworkManager Instance {
		get { return instance; }
	}
	
	void Awake() {
		if( instance != null && instance != this ) {
			Destroy (this.gameObject);
			return;
		} else {
			instance = this;
		}
		DontDestroyOnLoad(gameObject);
	}

	void Start() {
		otherPlayers = new List<Player>();
		gameManager = GameObject.FindGameObjectWithTag( Tags.GameController ).GetComponent<GameManager>();
		weaponController = my.avatar.GetComponent<PlayerWeapons>();
	}

	public static void StartServer() {
		bool useNAT = !Network.HavePublicAddress();
		Network.InitializeServer( MAX_CONNECTIONS, DEFAULT_PORT, useNAT );
	}

	public static void ConnectToServer(string ip) {
		Network.Connect( ip, DEFAULT_PORT );
	}

	public static void DisconnectFromServer() {
		Network.Disconnect();
	}

	public void EnterGame() {
		GameObject myAvatar = gameManager.SpawnPlayer();
		my = new Player();
		my.avatar = myAvatar;
		my.playerInfo = Network.player;
		
		gameManager.AssignCamera( myAvatar );	
		mainGUI = myAvatar.GetComponent<guiGame>();
		my.name = mainGUI.id;

		// Tell other players we've connected
		networkView.RPC( "GetNewPlayerState", RPCMode.Others, my.playerInfo, my.name, myAvatar.networkView.viewID, myAvatar.transform.position, myAvatar.transform.rotation );

		// Request each other player's state at the time of connection
		networkView.RPC( "RequestIntialPlayerState", RPCMode.OthersBuffered, Network.player );
	}
	
	public Player FindPlayer( NetworkPlayer player ) {
		if( player == my.playerInfo ) { 
			return my; 
		} else {
			return otherPlayers.Find( ( x => x.playerInfo == player ) );
		}
	} 
	
	public Player FindPlayerByViewID( NetworkViewID viewID ) {
		if ( viewID == my.avatar.networkView.viewID ) {
			return my;	
		} else {
			return otherPlayers.Find ( (x => x.avatar.networkView.viewID == viewID ) );	
		}
	}
	
	public int FindPlayerIndex( NetworkViewID viewID ) { // cannot find yourself
		return otherPlayers.FindIndex( (x => x.avatar.networkView.viewID == viewID ) );
	}
	
	IEnumerator RestartMatch() {
		yield return new WaitForSeconds(10);	
		
		//close final scoreboard
		mainGUI.ToggleFinalScoreboard();
		
		//reset everything and respawn
		my.playerHealth = 100;
		my.score = 0;
		gameManager.RespawnPlayer( my.avatar );
		weaponController.WeaponsReset();
		
		for (int i = 0; i < otherPlayers.Count; i++) {
			otherPlayers[i].playerHealth = 100;
			otherPlayers[i].score = 0;
			gameManager.RespawnPlayer( otherPlayers[i].avatar );	
		}
	}

	/////////////////////////
	//	EVENTS
	/////////////////////////

	// Called when the server goes up
	void OnServerInitialized() {
		Debug.Log( "Server Initialized" );
		Network.SetSendingEnabled(0,false);
		Network.isMessageQueueRunning = false;
		Network.SetLevelPrefix ( 2 );
		Application.LoadLevel( 2 );
		
		killsToWin = PlayerOptions.host_killsToWin;
		
		Network.isMessageQueueRunning = true;
		Network.SetSendingEnabled(0,true);
	}

	// Called when a player connects (server side)
	void OnPlayerConnected( NetworkPlayer playerInfo ) {
		//Tell the new player the kill limit
		networkView.RPC("SpecifyKillLimit", playerInfo, killsToWin);
	}

	// Called when the player connects (client side)
	void OnConnectedToServer() {	
		Network.SetSendingEnabled(0,false);
		Network.isMessageQueueRunning = false;
		Network.SetLevelPrefix( 2 );
		Application.LoadLevel( 2 );
		Network.isMessageQueueRunning = true;
		Network.SetSendingEnabled(0,true);
	}

	// Called when a player disconnects (server side)
	void OnPlayerDisconnected( NetworkPlayer player ) {
		Player disconnectedPlayer = FindPlayer( player );
		otherPlayers.Remove( disconnectedPlayer );
		mainGUI.UpdateAllPlayers();
		
		Network.RemoveRPCs( player );
		Network.DestroyPlayerObjects( player ); //added to test if this changes "lingering"
		networkView.RPC("RemoveObject", RPCMode.Others, player );
	}
	
	// Called when we disconnect (client side)
	void OnDisconnectedFromServer( NetworkDisconnection info ) {
		if ( info == NetworkDisconnection.LostConnection ) {
		}
		otherPlayers.Clear();
		Network.RemoveRPCs(networkView.viewID);
		Network.Destroy(networkView.viewID);
		Application.LoadLevel( Levels.Main );
	}

	void OnLevelWasLoaded( int levelID ) {
		if ( gameManager.IsGameplayLevel( levelID ) ) {
			gameManager.CreateLevelObjects();
			EnterGame();
		}
	}

	/////////////////////////
	//	RPCS
	/////////////////////////
	[RPC]
	void RequestIntialPlayerState( NetworkPlayer requester ) {
		networkView.RPC( "GetCurrentPlayerState", requester, my.playerInfo, my.name, my.avatar.networkView.viewID, my.avatar.transform.position, my.avatar.transform.rotation );
	}

	[RPC]
	void GetNewPlayerState( NetworkPlayer playerInfo, string playerName, NetworkViewID avatarID, Vector3 initialPosition, Quaternion initialRotation ) {
		Player newPlayer = new Player();
		newPlayer.playerInfo = playerInfo;
		newPlayer.name = playerName;
		GameObject playerAvatar = NetworkView.Find( avatarID ).gameObject;
		newPlayer.avatar = playerAvatar;
		otherPlayers.Add( newPlayer );
	}

	[RPC]
	void GetCurrentPlayerState( NetworkPlayer playerInfo, string playerName, NetworkViewID avatarID, Vector3 initialPosition, Quaternion initialRotation ) {
		Player newPlayer = new Player();
		newPlayer.playerInfo = playerInfo;
		newPlayer.name = playerName;
		GameObject playerAvatar = gameManager.SpawnPlayer( initialPosition, initialRotation );
		playerAvatar.networkView.viewID = avatarID;
		newPlayer.avatar = playerAvatar;
		otherPlayers.Add( newPlayer );
	}

	[RPC]
	void RemoveObject( NetworkPlayer player ) {
		Player disconnectedPlayer = FindPlayer( player );
		otherPlayers.Remove( disconnectedPlayer );
		
		NetworkViewID id = disconnectedPlayer.avatar.networkView.viewID;
		mainGUI.UpdateAllPlayers();
		Destroy( NetworkView.Find( id ).gameObject );
		Debug.Log( "Object with NetworkViewId " + id.ToString() + " removed" );
	}
	
	[RPC]
	void StopRendering( NetworkPlayer deadplayer ) {
		Player dead = FindPlayer( deadplayer );
		dead.avatar.SetActive(false);
	}
	
	[RPC]
	void ResumeRendering( NetworkPlayer respawnedPlayer ) {
		Player alive = FindPlayer( respawnedPlayer );
		alive.avatar.SetActive(true);
	}
	
	[RPC]
	void ReportDeath( NetworkViewID deadPlayerID, NetworkViewID killerID ) {
		Player deadPlayer = FindPlayerByViewID( deadPlayerID );
		Player killerPlayer = FindPlayerByViewID( killerID );
				
		// if killer is same as dead player (ie. suicide), then reduce dead player's score by 1
		if (deadPlayerID == killerID) { 
			deadPlayer.score--;
		} else { // increase killer's score by one
			
			killerPlayer.score++;
			if( killerPlayer.score >= killsToWin ) {
				Debug.Log(killerPlayer.name + " won!");
				
				//kill all players
				gameManager.KillPlayer( my.avatar );
				
				for (int i = 0; i < otherPlayers.Count; i++) {
					gameManager.KillPlayer( otherPlayers[i].avatar );	
				}
				
				//open final scoreboard
				mainGUI.ToggleFinalScoreboard();
				
				//restart the level, respawn players
				StartCoroutine( RestartMatch() );
			}
		}
	}
	
	[RPC]
	void SpecifyKillLimit( int limit ) {
		killsToWin = limit;
	}
}
