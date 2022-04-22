using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.NetworkVariable;
using MLAPI.Messaging;
using TMPro;
using System;

public enum Team
{
	Blue = 0,
	Orange = 1
}
public class Player : NetworkBehaviour
{
	public int playerId;

	[SerializeField]
	protected NetworkVariableInt team = new NetworkVariableInt(new NetworkVariableSettings
	{
		WritePermission = NetworkVariablePermission.OwnerOnly,
		ReadPermission = NetworkVariablePermission.Everyone
	});

	[SerializeField]
	protected float minLaunchForce;
	[SerializeField]
	protected float maxLaunchForce;

	[SerializeField]
	protected float minThrowForce;
	[SerializeField]
	protected float maxThrowForce;

	[SerializeField]
	protected float chargeTime;
	[SerializeField]
	protected float ballOffset;

	[SerializeField]
	protected float strikeCooldown;

	[SerializeField]
	protected float smackCooldown;

	protected float chargeStart;
	protected float lastStrike;
	protected float lastSmack;

	protected bool isChargingLaunch;
	protected bool isChargingThrow;
	protected bool leftMouseBuffer;
	protected bool rightMouseBuffer;

	[SerializeField]
	bool isOnWall;
	[SerializeField]
	bool hasBall;

	GameManager manager;

	Rigidbody2D rbody;
	LineRenderer line;

	Image chargeBar;
	Image strikeBar;
	Image smackBar;

	GameObject fakeBall;

	GameObject ballObject;

	public void SetWall()
	{
		isOnWall = true;
	}
	public static List<int> teams = new List<int>()
	{
		1,
		1,
		2,
		1,
		2
	};
	public static List<Color> teamColours = new List<Color>()
	{
		new Color(1f, 1, 1f),
		new Color(0f, 0.3215686f, 1f),
		new Color(1f, 0.3607843f, 0f)
	};
	void Start()
	{
		if (IsOwner)
		{
			SetTeamServerRpc(teams[(int)OwnerClientId]);
		}
		manager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
		rbody = GetComponent<Rigidbody2D>();
		line = GetComponentInChildren<LineRenderer>();
		chargeBar = GameObject.Find("Chargebar").GetComponent<Image>();
		strikeBar = GameObject.Find("Strikebar").GetComponent<Image>();
		  smackBar = GameObject.Find("Smackbar").GetComponent<Image>();
		ballObject = GameObject.Find("Ball");
		fakeBall = transform.GetChild(1).gameObject;
		ResetPosition();
	}

	

	private void Awake()
	{
		team.OnValueChanged += OnTeamChanged;
	}


	private void OnDisable()
	{
		team.OnValueChanged -= OnTeamChanged;
	}

	private void OnTeamChanged(int previousValue, int newValue)
	{
		transform.GetChild(2).GetComponent<SpriteRenderer>().color = teamColours[newValue];
		transform.GetChild(3).GetComponent<TextMeshPro>().color = teamColours[newValue];
	}

	public void ResetPosition()
	{
		if (!IsLocalPlayer)
			return;

		rbody.velocity = Vector2.zero;
		
		transform.position = manager.spawns[(int)OwnerClientId].position;
		line.enabled = true;
		line.SetPosition(0, transform.position);
		line.SetPosition(1, transform.position);
		isChargingLaunch = false;
		isChargingThrow = false;
		isOnWall = false;
		hasBall = false;
	}
	// Update is called once per frame
	void Update()
	{
		
		if (!manager.GameOn)
		{
			chargeBar.fillAmount = 0f;
			strikeBar.fillAmount = 0f;
			smackBar.fillAmount = 0f;
			return;
		}
		if(!IsOwner)
		{
			return;
		}

		//if (Input.GetKeyDown(KeyCode.Return))
		//{
		//	if(IsClient)
		//		TestServerRpc("Hello");
		//}
		int layerMask = 1 << 8;
		var directionToMouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
		var result = Physics2D.Raycast(transform.position, directionToMouse, 100f, layerMask);
		
		if (isOnWall || hasBall)
		{
			line.SetPosition(0, transform.position);
			line.SetPosition(1, result.point);
		}
		else
		{
			line.enabled = false;
		}

		if(Input.GetMouseButtonDown(0) || leftMouseBuffer) // Left Mouse : Launch / Strike off
		{
			if(isOnWall && !hasBall)	// Launch off wall
			{
				leftMouseBuffer = false;
				rightMouseBuffer = false;
				isChargingLaunch = true;
				chargeStart = Time.time;
			}
			else
			{
				leftMouseBuffer = true;
			}
		} 
		else if(Input.GetMouseButtonDown(1) || rightMouseBuffer) // Right Mouse : Throw
		{
			if (hasBall)
			{
				leftMouseBuffer = false;
				rightMouseBuffer = false;
				isChargingThrow = true;
				chargeStart = Time.time;
			}
			else
			{
				rightMouseBuffer = true;
			}
		}


		if (isChargingLaunch || isChargingThrow)
		{

			float t = Mathf.Clamp((Time.time - chargeStart) / (chargeTime), 0f, 1f);

			chargeBar.fillAmount = t;

			if (Input.GetMouseButtonUp(0) && isChargingLaunch) // Release to Launch
			{
				rbody.constraints = RigidbodyConstraints2D.FreezeRotation;
				float launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, t);
				rbody.AddForce(directionToMouse * launchForce);
				isChargingLaunch = false;
				isOnWall = false;
				lastStrike = Time.time;
			} 
			else if (Input.GetMouseButtonUp(1) && isChargingThrow)
			{
				float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, t);
				MoveBallServerRpc(transform.position + directionToMouse * ballOffset, directionToMouse * throwForce, false);
				fakeBall.SetActive(false);
				ballObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
				ballObject.transform.position = transform.position + directionToMouse * ballOffset;
				ballObject.GetComponent<Rigidbody2D>().AddForce(directionToMouse * throwForce);
				rbody.AddForce(-directionToMouse * throwForce);
				isChargingThrow = false;
				hasBall = false;
				lastSmack = Time.time;
			}

		}
		else
		{
			chargeBar.fillAmount = 0f;
			var colliders = Physics2D.OverlapCapsuleAll(transform.position, new Vector2(1.5f, 2.25f), CapsuleDirection2D.Vertical, transform.eulerAngles.z);
			if (Input.GetMouseButtonUp(0))	// Strike off
			{
				leftMouseBuffer = false;
				if(colliders.Length > 0 && Time.time > lastStrike + strikeCooldown)
				{
					lastStrike = Time.time;
					foreach (var collider in colliders)
					{
						if (collider.tag == "Wall" && !hasBall)
						{

							rbody.velocity = Vector2.zero;
							float launchForce = maxLaunchForce;
							rbody.AddForce(directionToMouse * launchForce);
							
						} 
						else if (collider.tag == "Pal" && collider.gameObject != gameObject)
						{
							float launchForce = maxLaunchForce;
							rbody.AddForce(directionToMouse * launchForce);
							collider.GetComponent<Rigidbody2D>().AddForce(-directionToMouse * launchForce);
						}
					}
				}
			}
			else if (Input.GetMouseButtonUp(1)) // Smack
			{
				rightMouseBuffer = false;
				if (colliders.Length > 0 && Time.time > lastSmack + smackCooldown)
				{
					lastSmack = Time.time;
					foreach (var collider in colliders)
					{
						if (collider.tag == "Ball")
						{
							float throwForce = maxThrowForce;
							MoveBallServerRpc(transform.position + directionToMouse * ballOffset, directionToMouse * throwForce, false);
							ballObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
							ballObject.transform.position = transform.position + directionToMouse * ballOffset;
							ballObject.GetComponent<Rigidbody2D>().AddForce(directionToMouse * throwForce);
							rbody.AddForce(-directionToMouse * throwForce);
						}
					}
				}
			}
		}
		strikeBar.fillAmount = Mathf.Clamp(1 - ((Time.time - lastStrike) / strikeCooldown), 0f, 1f);
		  smackBar.fillAmount = Mathf.Clamp(1 - ((Time.time - lastSmack) / smackCooldown), 0f, 1f);
	}
	private void OnCollisionEnter2D(Collision2D collision)
	{

		//if (!IsOwner)
		//	return;
		Debug.Log("Collided " + team.Value);
		var collider = collision.collider;

		if(collider.tag == "Wall")
		{
			isOnWall = true;
			rbody.velocity = Vector2.zero;
			line.enabled = true;
			rbody.constraints = RigidbodyConstraints2D.FreezeAll;
		}
		else if(collider.tag == "Ball")
		{
			hasBall = true;
			MoveBallServerRpc(new Vector2(-69f, -69f), Vector2.zero, true);
			ballObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			ballObject.transform.position = new Vector2(-69f, -69f);
			line.enabled = true;
			isChargingLaunch = false;
		}
	}
	[ServerRpc]
	private void SetTeamServerRpc(int _team)
	{
		if (_team > 3 || _team < 1)
			return;

		team.Value = _team;
	}
	[ServerRpc]
	private void MoveBallServerRpc(Vector2 start, Vector2 dir, bool showFake)
	{
		MoveBallClientRpc(start, dir, showFake);
	}

	[ClientRpc]
	private void MoveBallClientRpc(Vector2 start, Vector2 dir, bool showFake)
	{
		fakeBall.SetActive(showFake);
		ballObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
		ballObject.transform.position = start;
		ballObject.GetComponent<Rigidbody2D>().AddForce(dir);
	}
}
