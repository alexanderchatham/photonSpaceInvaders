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
using TMPro;
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
                        PhotonNetwork.InstantiateRoomObject("Enemy", new Vector3((-5*xSpacing)+j*2*xSpacing,0,yStart - i *ySpacing), Quaternion.Euler(0,180,0), 0, null);
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                {
                for(int j = 0; j < numberPerRow-1;j++) 
                    {
                        PhotonNetwork.InstantiateRoomObject("Enemy", new Vector3((-4*xSpacing) + j*2 * xSpacing, 0,yStart - i *ySpacing), Quaternion.Euler(0, 180, 0), 0, null);
                        yield return new WaitForEndOfFrame();
                    }

                }
            }

        photonView.RPC("setEnemiesSpawned", RpcTarget.AllViaServer);
    }
    [PunRPC]
    void setEnemiesSpawned()
    {
        enemiesSpawned = true;

    }
    public TextMeshProUGUI endText;

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
            PhotonNetwork.SetMasterClient(PhotonNetwork.PlayerList[0]);
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;

        }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        enemies = FindObjectsOfType<Enemy>();
        foreach (var e in enemies)
        {
            e.syncEnemies();
        }
        base.OnPlayerEnteredRoom(newPlayer);
    }
    public override void OnJoinedRoom()
    {
        StartGame();
        base.OnJoinedRoom();
    }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            CheckEndOfGame();

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
                    //InfoText.text = "Waiting for other players...";
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
        Vector3 position;
        if (PhotonNetwork.LocalPlayer.GetPlayerNumber() >= 2)
            position = spawnPoints[PhotonNetwork.LocalPlayer.GetPlayerNumber()].position;
        else
            position = spawnPoints[0].position;
            Quaternion rotation = Quaternion.identity;

            PhotonNetwork.Instantiate("Spaceship", position, rotation, 0);      // avoid this call on rejoin (ship was network instantiated before)

            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(SpawnBadGuys());
            }
        }
    public bool enemiesSpawned = false;
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
                        {
                            current = EnemyState.left;
                            photonView.RPC("changeState", RpcTarget.AllViaServer, 0);
                        }
                        else
                        {
                            current = EnemyState.right;
                            photonView.RPC("changeState", RpcTarget.AllViaServer, 1);
                        }
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
    [PunRPC]
    void changeState(int i)
    {
        switch (i)
        {
            case 0:
                current = EnemyState.left;
                break;
            case 1:
                current = EnemyState.right;
                break;
            case 2:
                current = EnemyState.down;
                break;
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
            bool playersDied = true;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object lives;
                if (p.CustomProperties.TryGetValue(spaceInvadersGame.PLAYER_LIVES, out lives))
                {
                    if ((int)lives > 0)
                    {
                        playersDied = false;
                        break;
                    }
                }
            }
            if(enemies == null)
        {
            enemies = FindObjectsOfType<Enemy>();
            if (enemies.Length == 0)
                Debug.Log("no enemies found");
        }
            if (playersDied||enemiesSpawned && enemies.Length == 0 )
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    StopAllCoroutines();
                }
            photonView.RPC("showEndPanel", RpcTarget.AllViaServer);
        }
        }
    public void leaveGame()
    {
        PhotonNetwork.LeaveRoom();
    }
    [PunRPC]
    public void showEndPanel()
    {

        string winner = "";
        int score = -1;
        int playercounter = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (playercounter == 0)
            {
                player1.text = p.NickName + "\n" + p.GetScore();
                playercounter++;
                score = p.GetScore();
            }
            else if (playercounter == 1)
            {
                player2.text = p.NickName + "\n" + p.GetScore();
                if (p.GetScore() > score)
                {
                    endText.text = "Player 2 Wins!";
                    Debug.Log("Player 2 Wins!");
                }
                else if (p.GetScore() < score)
                {
                    endText.text = "Player 1 Wins!";
                    Debug.Log("Player 1 Wins!");
                }
                else if (p.GetScore() == score)
                {
                    endText.text = "It's a tie!";
                    Debug.Log("It's a tie!");
                }
            }
        }
        endPanel.SetActive(true);
        playercounter = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.IsLocal)
            {
                if (playercounter == 0)
                {
                    player1.transform.parent.GetComponent<Outline>().enabled = true;
                }
                if (playercounter == 1)
                {
                    player2.transform.parent.GetComponent<Outline>().enabled = true;
                }
            }
            playercounter++;
        }
        }
    public GameObject endPanel;
    public TextMeshProUGUI player1;
    public TextMeshProUGUI player2;
        private void OnCountdownTimerIsExpired()
        {
            StartGame();
        }
    }
