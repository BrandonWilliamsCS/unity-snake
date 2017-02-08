using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [HideInInspector]
    public int MinX { get; set; }
    [HideInInspector]
    public int MaxX { get; set; }
    [HideInInspector]
    public int MinY { get; set; }
    [HideInInspector]
    public int MaxY { get; set; }
    [HideInInspector]
    public Action OnEat { get; set; }

    #region Unity UI Configurable
    public float movesPerSecond = 2f;
    #endregion

    #region Unity Injected
    private BoxCollider2D boxCollider;
    #endregion

    private Vector3 currentDirection = Vector3.right;
    private BodyTilePool bodyTileSource = new BodyTilePool();
    private LinkedList<BodyTile> bodyTiles = new LinkedList<BodyTile>();
    private Snake snake = new Snake(Vector3.right);
    private bool eating;

    public bool Occupies(Vector3 position)
    {
        if (transform.position == position)
            return true;
        foreach (var bodyTile in bodyTiles)
            if (bodyTile.Tile.transform.position == position)
                return true;
        return false;
    }

    private IEnumerator ProcessTicks()
    {
        while (movesPerSecond > float.Epsilon)
        {
            yield return new WaitForSeconds(1 / movesPerSecond);
            var tickDirection = currentDirection;
            var eating = this.eating;
            this.eating = false;
            if (CanMoveToward(tickDirection))
            {
                RotateHead(tickDirection);
                AdvanceSnake(tickDirection, eating);
                // TODO: speed up snake
            }
            else
            {
                GameOver();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        OnEat();
        eating = true;
    }

    private void RotateHead(Vector3 direction)
    {
        transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
    }

    private void AdvanceSnake(Vector3 direction, bool growing)
    {
        // If the snake has more than a head, we need to grow the head and shrink the tail.
        if (bodyTiles.Count == 0 && !growing)
            snake.Move(direction);
        else
        {
            GrowBodyToward(direction);
            if (!growing)
            {
                ShrinkTail();
            }
        }
        // now move the head itself as well.
        transform.position += direction;
    }

    private void GrowBodyToward(Vector3 direction)
    {
        var addTail = bodyTiles.Count == 0;
        var tileType = new BodyTileType(snake.CurrentFacing, direction, addTail);
        var tile = bodyTileSource.GetTile(tileType);
        var tileObject = tile.Tile;
        tileObject.transform.position = gameObject.transform.position;
        
        bodyTiles.AddFirst(tile);
        snake.Grow(direction);
    }

    private void ShrinkTail()
    {
        snake.Shrink();

        // remove the tail
        var lastTile = bodyTiles.Last.Value;
        bodyTiles.RemoveLast();
        bodyTileSource.ReturnTile(lastTile);

        // If there is a body piece left, replace it with a new tail
        lastTile = bodyTiles.Last.Value;
        bodyTiles.RemoveLast();
        bodyTileSource.ReturnTile(lastTile);

        var tailType = new BodyTileType(snake.CurrentFacing, snake.TailFacing, true);
        var newTailTile = bodyTileSource.GetTile(tailType);
        newTailTile.Tile.transform.position = lastTile.Tile.transform.position;
        bodyTiles.AddLast(newTailTile);
    }

    private bool CanMoveToward(Vector3 direction)
    {
        var destination = transform.position + direction;

        if (destination.x > MaxX || destination.x < MinX || destination.y > MaxY || destination.y < MinY)
        {
            Debug.Log(string.Format("Player hit wall: ({0}, {1})", destination.x, destination.y));
            return false;
        }

        boxCollider.enabled = false;
        var hit = Physics2D.Linecast(transform.position, destination);
        boxCollider.enabled = true;

        return hit.collider == null || hit.collider.isTrigger;
    }

    private void GameOver()
    {
        movesPerSecond = 0;
    }

    #region Game Loop Events
    // Use this for initialization
    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        StartCoroutine(ProcessTicks());
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDirection();
    }
    #endregion

    private void UpdateDirection()
    {
        int horizontal = (int)Input.GetAxisRaw("Horizontal");
        if (horizontal != 0)
            currentDirection = horizontal > 0 ? Vector3.right : Vector3.left;
        else
        {
            int vertical = (int)Input.GetAxisRaw("Vertical");
            if (vertical != 0)
                currentDirection = vertical > 0 ? Vector3.up : Vector3.down;
        }
    }

    #region Models, etc., that probably belong elsewhere
    private class Snake
    {
        public Vector3 CurrentFacing { get { return bodySegments.First.Value.GrowDirection; } }
        public Vector3 TailFacing { get { return bodySegments.Last.Value.GrowDirection; } }
        private LinkedList<SnakeSegment> bodySegments = new LinkedList<SnakeSegment>();

        public Snake(Vector3 initialFacing)
        {
            bodySegments.AddFirst(new SnakeSegment(initialFacing));
        }

        public void Move(Vector3 direction)
        {
            if (bodySegments.Count != 1 || bodySegments.First.Value.length != 1)
                throw new Exception("Can't move non-trivial snake body.");

            bodySegments.Clear();
            bodySegments.AddFirst(new SnakeSegment(direction));
        }

        public void Grow(Vector3 direction)
        {
            if (bodySegments.First.Value.GrowDirection != direction)
            {
                bodySegments.AddFirst(new SnakeSegment(direction));
            }
            bodySegments.First.Value.length++;
        }

        public void Shrink()
        {
            bodySegments.Last.Value.length--;
            if (bodySegments.Count > 1 && bodySegments.Last.Value.length == 1)
            {
                bodySegments.RemoveLast();
            }
        }

        private class SnakeSegment
        {
            public Vector3 GrowDirection { get; private set; }
            public int length { get; set; }

            public SnakeSegment(Vector3 growDirection)
            {
                length = 1;
                GrowDirection = growDirection;
            }
        }
    }

    private class BodyTile
    {
        public GameObject Tile { get; private set; }
        public BodyTileType Type { get; private set; }

        public BodyTile(GameObject tile, BodyTileType type)
        {
            Tile = tile;
            Type = type;
        }
    }

    private struct BodyTileType
    {
        public Vector3 FromDirection { get; private set; }
        public Vector3 ToDirection { get; private set; }
        public bool IsTail { get; private set; }

        public BodyTileType(Vector3 fromDirection, Vector3 toDirection, bool isTail)
        {
            FromDirection = fromDirection;
            ToDirection = toDirection;
            IsTail = isTail;
        }
    }

    private class BodyTilePool
    {
        public BodyTile GetTile(BodyTileType bodyTileType)
        {
            string name;
            if (bodyTileType.IsTail)
                name = string.Format("SnakeTail{0}", GetDirectionString(bodyTileType.ToDirection));
            else if (bodyTileType.FromDirection == bodyTileType.ToDirection)
                name = string.Format("SnakeSegment{0}", GetDirectionString(bodyTileType.ToDirection));
            else
                name = string.Format("SnakeBend{0}{1}", GetDirectionString(bodyTileType.FromDirection), GetDirectionString(bodyTileType.ToDirection));
            var preFab = Resources.Load<GameObject>(name);
            var clonedTile = Instantiate(preFab);
            return new BodyTile(clonedTile, bodyTileType);
            // TODO: pool tiles
            // TODO: enable
        }

        public void ReturnTile(BodyTile tile)
        {
            // TODO: re-pool and disable
            tile.Tile.SetActive(false);
            Destroy(tile.Tile);
        }

        private string GetDirectionString(Vector3 direction)
        {
            if (direction == Vector3.up)
                return "Up";
            else if (direction == Vector3.down)
                return "Down";
            else if (direction == Vector3.left)
                return "Left";
            else if (direction == Vector3.right)
                return "Right";
            else
                return string.Empty;
        }
    }
    #endregion
}
