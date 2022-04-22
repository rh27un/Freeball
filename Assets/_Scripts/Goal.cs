using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    protected GameManager manager;
    [SerializeField]
    protected Team scoringTeam;

	private void Start()
	{
		manager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if(collision.collider.tag == "Ball")
		{
			manager.Score(scoringTeam);
		}
	}
}
