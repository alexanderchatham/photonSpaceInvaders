// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidsGameManager.cs" company="Exit Games GmbH">
//   Part of: Asteroid demo
// </copyright>
// <summary>
//  Game Manager for the Asteroid Demo
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
    public class spaceInvaderGameManager : MonoBehaviourPunCallbacks
    {
        public static spaceInvaderGameManager Instance = null;

        public Text InfoText;


        #region UNITY

        public void Awake()
        {
            Instance = this;
        }
    public Transform EnemyHolder;
    public bool right;

        public override void OnEnable()
        {
            base.OnEnable();

            CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        }

        public void Start()
        {
            Hashtable props = new Hashtable
            {
                {spaceInvadersGame.PLAYER_LOADED_LEVEL, true}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;
        }

        #endregion

        #region COROUTINES
        public int numberOfRows = 5;
        public int numberPerRow = 6;
        float xSpacing = 5f;
        float ySpacing = 9f;
        float yStart = 50f;
        private IEnumerator SpawnBadGuys()
        {
            for(int i = 0; i < numberOfRows; i++)
            {
                if(i%2 == 0)
                {

                for(int j = 0; j < numberPerRow;j++) 
                    {
                        PhotonNetwork.InstantiateRoomObject("Enemy", new Vector3((-5*xSpacing)+j*2*xSpacing,0,yStart - i *ySpacing), Quaternion.Euler(0,180,0), 0, null).transform.parent = EnemyHolder;
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                {
                for(int j = 0; j < numberPerRow-1;j++) 
                    {
                        PhotonNetwork.InstantiateRoomObject("Enemy", new Vector3((-4*xSpacing) + j*2 * xSpacing, 0,yStart - i *ySpacing), Quaternion.Euler(0, 180, 0), 0, null).transform.parent = EnemyHolder;
                        yield return new WaitForEndOfFrame();
                    }

                }
            }



    }

        private IEnumerator EndOfGame(string winner, int score)
        {
            float timer = 5.0f;

            while (timer > 0.0f)
            {
                InfoText.text = string.Format("Player {0} won with {1} points.\n\n\nReturning to login screen in {2} seconds.", winner, score, timer.ToString("n2"));

                yield return new WaitForEndOfFrame();

                timer -= Time.deltaTime;
            }

            PhotonNetwork.LeaveRoom();
        }

        #endregion

        #region PUN CALLBACKS

        public override void OnDisconnected(DisconnectCause cause)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("spaceInvaders");
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.Disconnect();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
            {
               // StartCoroutine(SpawnBadGuys());
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            CheckEndOfGame();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(spaceInvadersGame.PLAYER_LIVES))
            {
                CheckEndOfGame();
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }


            // if there was no countdown yet, the master client (this one) waits until everyone loaded the level and sets a timer start
            int startTimestamp;
            bool startTimeIsSet = CountdownTimer.TryGetStartTime(out startTimestamp);

            if (changedProps.ContainsKey(spaceInvadersGame.PLAYER_LOADED_LEVEL))
            {
                if (CheckAllPlayerLoadedLevel())
                {
                    if (!startTimeIsSet)
                    {
                        CountdownTimer.SetStartTime();
                    }
                }
                else
                {
                    // not all players loaded yet. wait:
                    Debug.Log("setting text waiting for players! ", this.InfoText);
                    InfoText.text = "Waiting for other players...";
                }
            }

        }

        #endregion
        public Transform[] spawnPoints;

        // called by OnCountdownTimerIsExpired() when the timer ended
        private void StartGame()
        {
            Debug.Log("StartGame!");

            // on rejoin, we have to figure out if the spaceship exists or not
            // if this is a rejoin (the ship is already network instantiated and will be setup via event) we don't need to call PN.Instantiate


            Vector3 position = spawnPoints[PhotonNetwork.LocalPlayer.GetPlayerNumber()].position;
            Quaternion rotation = Quaternion.identity;

            PhotonNetwork.Instantiate("Spaceship", position, rotation, 0);      // avoid this call on rejoin (ship was network instantiated before)

            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(SpawnBadGuys());
            }
        }
    enum EnemyState
    {
        left,
        right,
        down
    }
    EnemyState current = EnemyState.right;
    EnemyState last;
    public float downTime = 1f;
    float downTimer;
    public float enemyMoveSpeed = 8f;
    public void hitSide()
    {
        last = current;
        current = EnemyState.down;
        downTimer = downTime;
    }
    public float side = 40;
    void checkSides(Vector3 pos)
    {
        if (pos.x < -side && current == EnemyState.left)
            hitSide();
        if (pos.x > side && current == EnemyState.right)
            hitSide();
    }

    Enemy[] enemies;
    public void FixedUpdate()
    {
        if (PhotonNetwork.IsMasterClient)
        {

            enemies = FindObjectsOfType<Enemy>();
            //check enemy position
            foreach (var enemy in enemies)
            {
                if(enemy!= null)
                    checkSides(enemy.transform.position);
            }
            //move enemies
            switch (current)
            {
                case EnemyState.down:
                    downTimer -= Time.deltaTime;
                    EnemyHolder.Translate(Vector3.back * Time.deltaTime * enemyMoveSpeed);
                    if (downTimer < 0f)
                    {
                        if (last == EnemyState.right)
                            current = EnemyState.left;
                        else
                            current = EnemyState.right;
                    }
                    break;
                case EnemyState.left:
                    EnemyHolder.Translate(Vector3.left * Time.deltaTime * enemyMoveSpeed);
                    break;
                case EnemyState.right:
                    EnemyHolder.Translate(Vector3.right * Time.deltaTime * enemyMoveSpeed);
                    break;
            }
        }
    }
    private bool CheckAllPlayerLoadedLevel()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object playerLoadedLevel;

                if (p.CustomProperties.TryGetValue(spaceInvadersGame.PLAYER_LOADED_LEVEL, out playerLoadedLevel))
                {
                    if ((bool)playerLoadedLevel)
                    {
                        continue;
                    }
                }

                return false;
            }

            return true;
        }

        private void CheckEndOfGame()
        {
            bool allDestroyed = true;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object lives;
                if (p.CustomProperties.TryGetValue(spaceInvadersGame.PLAYER_LIVES, out lives))
                {
                    if ((int)lives > 0)
                    {
                        allDestroyed = false;
                        break;
                    }
                }
            }

            if (allDestroyed)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    StopAllCoroutines();
                }

                string winner = "";
                int score = -1;

                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    if (p.GetScore() > score)
                    {
                        winner = p.NickName;
                        score = p.GetScore();
                    }
                }

                StartCoroutine(EndOfGame(winner, score));
            }
        }

        private void OnCountdownTimerIsExpired()
        {
            StartGame();
        }
    }
