using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using System;
using TMPro;
using System.Text;

public class ServerStart : MonoBehaviour
{
	NetworkManager netManager;

	[SerializeField] protected TMP_InputField nameInput;

	public static Dictionary<ulong, string> playerNames;

	protected GameObject nameRequired;
	private void Awake()
	{
		NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
		NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += HandeClientDisconnected;
		nameRequired = GameObject.Find("Name Required");
		nameRequired.SetActive(false);
	}

	private void HandleServerStarted()
	{
		if(NetworkManager.Singleton.IsHost)
		{
			HandleClientConnected(NetworkManager.Singleton.LocalClientId);
		}
	}

	private void HandleClientConnected(ulong clientId)
	{
		if (clientId == NetworkManager.Singleton.LocalClientId)
			Destroy(transform.GetChild(0).gameObject);


	}
	private void HandeClientDisconnected(ulong clientId)
	{
		if (NetworkManager.Singleton.IsServer)
		{
			playerNames.Remove(clientId);
		}
	}

	private void OnDestroy()
	{
		if (NetworkManager.Singleton == null)
			return;

		NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
		NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback -= HandeClientDisconnected;

	}


	public static string GetPlayerName(ulong clientId)
	{
		if (playerNames.TryGetValue(clientId, out string playerName))
			return playerName;
		return null;
	}

	public void StartHost()
	{
		if (string.IsNullOrWhiteSpace(nameInput.text))
		{
			nameRequired.SetActive(true);
			return;

		}
		playerNames = new Dictionary<ulong, string>();
		playerNames[NetworkManager.Singleton.LocalClientId] = nameInput.text;

		NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
		NetworkManager.Singleton.StartHost();

	}

	private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
	{
		string name = Encoding.ASCII.GetString(connectionData);
		Debug.Log(name);
		playerNames[clientId] = name;
		bool approved = !string.IsNullOrEmpty(name);
		callback(true, null, approved, null, null);
	}

	public void StartClient()
	{
		NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(nameInput.text);
		NetworkManager.Singleton.StartClient();
		nameRequired.SetActive(true);

	}
}
