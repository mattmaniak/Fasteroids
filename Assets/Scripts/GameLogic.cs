using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class MyComparer : Comparer<AsteroidDto>
{
    // Compares by Length, Height, and Width.
    public override int Compare( AsteroidDto x, AsteroidDto y )
    {
        if ( x.Position.x < y.Position.x)
            return -1;

        if ( x.Position.x == y.Position.x ) 
            return 0;

        return 1;
    }
}

public class GameLogic : MonoBehaviour
{
    [StructLayout(LayoutKind.Explicit)]
    struct FloatIntUnion
    {
        [FieldOffset(0)]
        public float f;

        [FieldOffset(0)]
        public int tmp;
    }

    #region Constants
    float AsteroidRadius = 0.20f;
    float AsteroidTranformValueZ = 0.4f;

    const int GridDimensionInt = 160;
    const float GridDimensionFloat = 160;
    const int TotalNumberOfAsteroids = GridDimensionInt * GridDimensionInt;

    public string ShipName;

    // pool sizes
    const int AsteroidPoolSize = 40; // in tests this never went above 35 so for safety I gave 5 more

    float FrustumSizeX = 3.8f;
    float FrustumSizeY = 2.3f;
    #endregion

    #region Private Fields
    // readonly fields and tables
    static readonly AsteroidDto[] _asteroids = new AsteroidDto[TotalNumberOfAsteroids];

    // this is were unused object goes upon death
    static readonly Vector3 _objectGraveyardPosition = new Vector3(-99999, -99999, 0.3f);

    // prefabs
    [SerializeField] GameObject _asteroidPrefab;
    [SerializeField] GameObject _spaceshipPrefab;

    // other references
    [SerializeField] Camera _mainCamera;
    [SerializeField] Button _restartButton;
    [SerializeField] Text _youLoseLabel;

    // object pools
    GameObject[] _asteroidPool;

    Vector3 _playerCachedPosition;

    Transform _playerTransform;
    bool _playerDestroyed;

    Fasteroids.DataLayer.SpaceshipRepository _repository = new Fasteroids.DataLayer.SpaceshipRepository();
    #endregion

    void Start()
    {
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);

        CreateObjectPoolsAndTables();
        InitializeAsteroidsGridLayout();
        System.Array.Sort(_asteroids, new MyComparer());

        _playerTransform = Instantiate(_spaceshipPrefab).transform;
        _playerTransform.position = new Vector3(
            GridDimensionFloat / 2f - 0.5f,
            GridDimensionFloat / 2f - 0.5f,
            0.3f);

        AssignSpaceShipName();
    }

    void Update()
    {
        _playerCachedPosition = _playerTransform.position;

        HandleInput();

        UpdateAsteroids();
        System.Array.Sort( _asteroids, new MyComparer() );

        CheckCollisionsBetweenAsteroids();
        CheckCollisionsWithShip();
        ShowVisibleAsteroids();

        if (!_playerDestroyed)
            _mainCamera.transform.position = new Vector3(_playerTransform.position.x, _playerTransform.position.y, 0f);
    }

    float playerRadius = 0.08f;
    float playerSpeed = 1.25f;

    void HandleInput()
    {
        if (!_playerDestroyed)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                _playerTransform.position += _playerTransform.up * Time.deltaTime * playerSpeed;
            else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                _playerTransform.position -= _playerTransform.up * Time.deltaTime * playerSpeed;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                _playerTransform.Rotate(new Vector3(0, 0, 3f));
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                _playerTransform.Rotate(new Vector3(0, 0, -3f));
        }

        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    /// <summary>
    /// Updates asteroids' position or respawns them when time comes.
    /// </summary>
    void UpdateAsteroids()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < TotalNumberOfAsteroids; ++i)
        {
            ref AsteroidDto a = ref _asteroids[i];

            if (a.DestroyedThisFrame == false)
            {
                a.Position += a.Direction * a.Speed;

                continue;
            }

            a.TimeLeftToRespawn -= deltaTime;
            if (a.TimeLeftToRespawn <= 0)
                RespawnAsteroid(ref a);
        }
    }

    void RespawnAsteroid(ref AsteroidDto a)
    {
        // iterate until you find a position outside of player's frustum
        // it is not the most mathematically correct solution
        // as the asteroids dispersion will not be even (those that normally would spawn inside the frustum 
        // will spawn right next to the frustum's edge instead)
        float posX = Random.Range(0, GridDimensionFloat);
        if (posX > _playerTransform.position.x)
        {
            // tried to spawn on the right side of the player
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.x;
            if (value2 < 0)
                value2 *= -1;

            if (value1 - value2 < FrustumSizeX)
                posX += FrustumSizeX;
        }
        else
        {
            // left side
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.x;
            if (value2 < 0)
                value2 *= -1;

            if (value2 - value1 < FrustumSizeX)
                posX -= FrustumSizeX;
        }

        float posY = Random.Range(0, GridDimensionFloat);
        if (posY > _playerTransform.position.y)
        {
            // tried to spawn above the player
            float value1 = posY;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.y;
            if (value2 < 0)
                value2 *= -1;

            if (value1 - value2 < FrustumSizeY)
                posY += FrustumSizeY;
        }
        else
        {
            // below
            float value1 = posX;
            if (value1 < 0)
                value1 *= -1;

            float value2 = _playerTransform.position.y;
            if (value2 < 0)
                value2 *= -1;

            if (value2 - value1 < FrustumSizeY)
                posY -= FrustumSizeY;
        }

        // respawn
        a.Position = new Vector3(posX, posY, 0);
    }

    /// <summary>
    /// Check if there is any collision between any two asteroids in the game.
    /// Updates the game state if any collision has been found.
    /// </summary>
    void CheckCollisionsBetweenAsteroids()
    {
        // the last one is the last to the right it does not need to be processed because
        // its collisions are already handled by the ones preceding him
        for (int indexA = 0; indexA < TotalNumberOfAsteroids - 1; indexA++)
        {
            int indexB = indexA + 1;
            ref AsteroidDto a = ref _asteroids[indexA];
            ref AsteroidDto b = ref _asteroids[indexB];

            float difX = b.Position.x - a.Position.x;
            if (difX >= AsteroidRadius + AsteroidRadius)
                continue; // b is too far on x axis

            // a is destroyed
            if (a.DestroyedThisFrame)
                continue;

            // check for other asteroids
            while (indexB < TotalNumberOfAsteroids - 1)
            {
                float difY = b.Position.y - a.Position.y;
                if (difY < 0)
                    difY *= -1;

                if (difY >= AsteroidRadius + AsteroidRadius)
                {
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius + AsteroidRadius)
                        break; // b is too far on x axis
                    continue;
                }

                // b is destroyed
                if (b.DestroyedThisFrame)
                {
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius + AsteroidRadius)
                        break; // b is too far on x axis
                    continue;
                }

                float distance = FastSqrt(difX * difX + difY * difY);
                if (distance < AsteroidRadius + AsteroidRadius)
                {
                    // collision! mark both as destroyed in this frame and break the loop
                    a.DestroyedThisFrame = true; // destroyed
                    b.DestroyedThisFrame = true; // destroyed
                    a.TimeLeftToRespawn = 1f;
                    b.TimeLeftToRespawn = 1f;
                    ++indexA; // increase by one here and again in the for loop
                    break;
                }
                else
                {
                    // no collision with this one but it maybe with the next one
                    // as long as the x difference is lower than Radius * 2
                    b = ref _asteroids[++indexB];
                    difX = b.Position.x - a.Position.x;
                    if (difX >= AsteroidRadius + AsteroidRadius)
                        break; // b is too far on x axis
                }
            };
        }
    }

    /// <summary>
    /// Check if there is any collision between any asteroid and any laser or player.
    /// Updates the game state if any collision has been found.
    /// </summary>
    void CheckCollisionsWithShip()
    {
        float lowestX = _playerTransform.position.x;
        float highestX = _playerTransform.position.x;

        // find the range within collision is possible
        for (int i = 0; i < TotalNumberOfAsteroids; i++)
        {
            ref AsteroidDto a = ref _asteroids[i];

            // omit destroyed
            if (a.DestroyedThisFrame)
                continue;

            if (a.Position.x < lowestX)
            {
                float value = lowestX - a.Position.x;
                if (value < 0)
                    value *= -1;

                if (value > AsteroidRadius + playerRadius)
                    continue; // no collisions possible
            }
            else if (a.Position.x > highestX)
            {
                float value = highestX - a.Position.x;
                if (value < 0)
                    value *= -1;

                if (value > AsteroidRadius + playerRadius)
                    break; // no collisions possible neither for this nor for all the rest
            }

            if (_playerDestroyed)
                continue;

            // check collision with the player
            float distance = FastSqrt(
                (_playerTransform.position.x - a.Position.x) * (_playerTransform.position.x - a.Position.x)
                + (_playerTransform.position.y - a.Position.y) * (_playerTransform.position.y - a.Position.y));

            if (distance < AsteroidRadius + playerRadius)
            {
                // this asteroid destroyed player
                a.DestroyedThisFrame = true;
                GameOverFunction();
            }
        }
    }

    // TODO: maybe that would be good to handle editor window
    public void SpawnAsteroid_Editor(Vector2 position, float angle) { }

    void GameOverFunction()
    {
        _playerDestroyed = true;
        _playerTransform.gameObject.SetActive(false);
        _restartButton.gameObject.SetActive(true);
        _youLoseLabel.gameObject.SetActive(true);
    }

    public void RestartGame()
    {
        _playerTransform.gameObject.SetActive(true);
        _playerTransform.position = new Vector3(
            GridDimensionFloat / 2f - 0.5f,
            GridDimensionFloat / 2f - 0.5f,
            0.3f);
        _playerTransform.rotation = new Quaternion(0, 0, 0, 0);

        _playerDestroyed = false;
        _restartButton.gameObject.SetActive(false);
        _youLoseLabel.gameObject.SetActive(false);

        InitializeAsteroidsGridLayout();
    }

    void ShowVisibleAsteroids()
    {
        int poolElementIndex = 0;

        for (int i = 0; i < TotalNumberOfAsteroids; i++)
        {
            ref AsteroidDto a = ref _asteroids[i];

            if (a.DestroyedThisFrame)
                continue;

            // is visible in x?
            float value = _playerTransform.position.x - a.Position.x;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeX)
                continue;

            // is visible in y?
            value = _playerTransform.position.y - a.Position.y;
            if (value < 0)
                value *= -1;
            if (value > FrustumSizeY)
                continue;

            // take first from the pool
            _asteroidPool[poolElementIndex++].gameObject.transform.position = new Vector3(
                a.Position.x,
                a.Position.y,
                AsteroidTranformValueZ);
        }

        // unused objects go to the graveyard
        while (poolElementIndex < AsteroidPoolSize)
            _asteroidPool[poolElementIndex++].transform.position = _objectGraveyardPosition;
    }

    #region Initializers
    void InitializeAsteroidsRandomPosition()
    {
        for (int x = 0, i = 0; x < GridDimensionInt; x++)
            for (int y = 0; y < GridDimensionInt; y++)
            {
                _asteroids[i++] = new AsteroidDto()
                {
                    Position = new Vector3(
                        Random.Range(0, GridDimensionFloat),
                        Random.Range(0, GridDimensionFloat),
                        0),
                    RotationSpeed = Random.Range(0, 5f),
                    Direction = new Vector3(Random.Range(-1, 1f), Random.Range(-1, 1f), 0),
                    Speed = Random.Range(0.01f, 0.05f),
                    DestroyedThisFrame = false
                };
            }
    }

    void InitializeAsteroidsGridLayout()
    {
        for (int x = 0, i = 0; x < GridDimensionInt; x++)
            for (int y = 0; y < GridDimensionInt; y++)
                _asteroids[i++] = new AsteroidDto()
                {
                    Position = new Vector3(x, y, 0),
                    RotationSpeed = Random.Range(0, 5f),
                    Direction = new Vector3(Random.Range(-1, 1f), Random.Range(-1, 1f), 0),
                    Speed = Random.Range(0.01f, 0.05f),
                    DestroyedThisFrame = false
                };
    }

    void CreateObjectPoolsAndTables()
    {
        _asteroidPool = new GameObject[AsteroidPoolSize];
        for (int i = 0; i < AsteroidPoolSize; i++)
        {
            _asteroidPool[i] = Instantiate(_asteroidPrefab.gameObject);
            _asteroidPool[i].transform.position = _objectGraveyardPosition;
        }
    }
    #endregion

    // not written by me, I found it on the Internet
    // it is around 10 - 15% faster than the Mathf.Sqrt from Unity.Mathematics 
    // (which probably uses the inverse square root method from Quake 3 based on its cost).
    // but that comes for a cost of less accurate approximation (from 0.5% to 5% less accurate)
    float FastSqrt(float number)
    {
        if (number == 0)
            return 0;

        FloatIntUnion u;
        u.tmp = 0;
        u.f = number;
        u.tmp -= 1 << 23; /* Subtract 2^m. */
        u.tmp >>= 1; /* Divide by 2. */
        u.tmp += 1 << 29; /* Add ((b + 1) / 2) * 2^m. */
        return u.f;
    }

    void AssignSpaceShipName()
    {
        var SpaceShipConfig = _repository.LoadData();

        if (SpaceShipConfig == null)
        {
            ShipName = "Avenger";
            Debug.Log("Unable to read ShipName from JSON. Set to default.");
            return;
        }
        // Debug.Log("Name from file: " + SpaceShipConfig.Name); // Check if works.        
        ShipName = SpaceShipConfig.Name;
    }
}
