using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.NetworkVariable;
using TMPro;
using System;

public class PlayerName : NetworkBehaviour
{
	protected NetworkVariableString displayName = new NetworkVariableString();
	public override void NetworkStart()
	{
		if (!IsServer)
			return;
		var newName = ServerStart.GetPlayerName(OwnerClientId);
		if (newName == null)
			Debug.LogError("No name");
		else
			displayName.Value = newName;
	}
	private void OnEnable()
	{
		displayName.OnValueChanged += HandleDisplayNameChanged;
	}
	private void OnDisable()
	{
		displayName.OnValueChanged -= HandleDisplayNameChanged;
	}

	private void HandleDisplayNameChanged(string previousValue, string newValue)
	{
		GetComponent<TextMeshPro>().text = newValue;
	}
}
