using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;
public class GameManager : NetworkBehaviour
{
	public bool GameOn { get { return netGameOn.Value;  } }

	public GameObject ballPrefab;

	[SerializeField]
	protected float totalTime;

	protected NetworkVariableFloat netCountDown = new NetworkVariableFloat(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.OwnerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});
	protected NetworkVariableFloat netGameTime = new NetworkVariableFloat(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.OwnerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});
	[SerializeField]
	protected NetworkVariableBool netGameOn = new NetworkVariableBool(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.OwnerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});
	[SerializeField]
	protected NetworkVariableBool netCountDownOn = new NetworkVariableBool(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.OwnerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});
	protected float countDown;
	protected float countDownStart;
	protected float gameTime;
	protected bool gameOn;
	protected bool countDownOn;

	protected TextMeshProUGUI time;
	protected TextMeshProUGUI blueScore;
	protected TextMeshProUGUI orangeScore;

	protected NetworkManager netManager;

	protected NetworkVariableFloat blueScoreVal = new NetworkVariableFloat(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.OwnerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});
	protected NetworkVariableFloat orangeScoreVal = new NetworkVariableFloat(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.OwnerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});
	protected Dictionary<Team, int> scores = new Dictionary<Team, int>
	{
		{ Team.Blue, 0 },
		{ Team.Orange, 0 }
	};
	public List<Transform> spawns = new List<Transform>();
	private void Start()
	{
		netManager = GetComponentInParent<NetworkManager>();
		time = GameObject.Find("GameTime").GetComponent<TextMeshProUGUI>();
		blueScore = GameObject.Find("BlueScore").GetComponent<TextMeshProUGUI>();
		orangeScore = GameObject.Find("OrangeScore").GetComponent<TextMeshProUGUI>();

		if (NetworkManager.Singleton.IsHost)
		{
			netGameTime.Value = 0;
			netGameOn.Value = false;
		}
		Time.timeScale = 0.00f;

		countDownOn = true;
	}

	public void StartGame()
	{
		Time.timeScale = 1f;
		gameTime = 0;
		if (NetworkManager.Singleton.IsHost)
		{
			netGameOn.Value = true;
		}
		countDownOn = false;
		foreach(var go in GameObject.FindGameObjectsWithTag("Player"))
		{
			go.GetComponent<Player>().SetWall();
		}
	}

	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space) && NetworkManager.Singleton.IsHost)
		{
			Debug.Log("starting countdown");
			netCountDownOn.Value = true;
			countDownStart = Time.unscaledTime;
			netCountDown.Value = 5f;
		}
		if(netGameOn.Value)
		{
			if (NetworkManager.Singleton.IsHost)
				netGameTime.Value += Time.deltaTime;
			time.text = TimeSpan.FromMilliseconds((int)(netGameTime.Value * 1000)).ToString(@"mm\:ss\.ff");
		}
		if(netCountDownOn.Value && countDownOn)
		{
			if (NetworkManager.Singleton.IsHost)
				netCountDown.Value -= Time.unscaledDeltaTime;
			time.text = TimeSpan.FromMilliseconds((int)(netCountDown.Value * 1000)).ToString(@"mm\:ss\.ff");
			if(netCountDown.Value <= 0f)
			{
				StartGame();
			}
		}
	}

	public void Score(Team team)
	{
		if(IsHost)
		{
			ScoreServerRpc(team);
		}
	}

	[ServerRpc]
	public void ScoreServerRpc(Team team)
	{
		scores[team]++;
		netGameOn.Value = false;
		netCountDown.Value = 5f;
		netCountDownOn.Value = true;
		UpdateScoresServerRpc(scores[Team.Blue], scores[Team.Orange]);
		ScoreClientRpc();
	} 

	[ClientRpc]
	public void ScoreClientRpc()
	{
		gameOn = false;
		countDown = 5f;
		countDownOn = true;
	}
	[ServerRpc]
	private void UpdateScoresServerRpc(int blue, int orange)
	{
		GameObject.FindGameObjectWithTag("Ball").GetComponent<Rigidbody2D>().velocity = Vector2.zero;
		GameObject.FindGameObjectWithTag("Ball").transform.position = Vector2.zero;
		UpdateScoresClientRpc(blue, orange);

	}
	[ClientRpc]
	private void UpdateScoresClientRpc(int blue, int orange)
	{
		blueScore.text = blue.ToString();
		orangeScore.text = orange.ToString();

		foreach (var player in GameObject.FindGameObjectsWithTag("Pal"))
			player.GetComponent<Player>().ResetPosition();

		Debug.Log("starting countdown");
		countDownOn = true;
		Time.timeScale = 0f;
	}
}
