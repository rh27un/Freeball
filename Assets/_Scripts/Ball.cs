using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using MLAPI.Messaging;

public class Ball : NetworkBehaviour
{

	Rigidbody2D rbody;
	private void Start()
	{
		rbody = GetComponent<Rigidbody2D>();
	}

	[ClientRpc]
	public void HitBallClientRpc(Vector2 start, Vector2 dir, bool additive = false)
	{
		if(!additive)
			rbody.velocity = Vector2.zero;
		transform.position = start;
		rbody.AddForce(dir);
	}
}
