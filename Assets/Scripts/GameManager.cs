using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    #region Unity UI Configurable
    public int arenaWidth = 12;
    public int arenaHeight = 12;
    #endregion
    
    private Player player;
    private GameObject foodTile;

	#region Game Loop Events
	// Use this for initialization
	void Awake()
	{
        player = GameObject.Find("Player").GetComponent<Player>();
        player.MinX = 0;
        player.MaxX = arenaWidth - 1;
        player.MinY = 0;
        player.MaxY = arenaWidth - 1;
        player.OnEat = MoveFood;
    }
	
	// Update is called once per frame
	void Start()
    {
        var foodPreFab = Resources.Load<GameObject>("Food");
        foodTile = Instantiate(foodPreFab);
        MoveFood();
    }
    #endregion

    private Vector2 RandomUnoccupiedSpace()
    {
        int x = Random.Range(0, arenaWidth - 1);
        int y = Random.Range(0, arenaWidth - 1);
        Vector2 position = new Vector2(x, y);
        while (player.Occupies(position))
        {
            x = Random.Range(0, arenaWidth - 1);
            y = Random.Range(0, arenaWidth - 1);
            position = new Vector2(x, y);
        }
        return position;
    }

    private void MoveFood()
    {
        foodTile.transform.position = RandomUnoccupiedSpace();
    }
}
