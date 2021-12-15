// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Asteroid.cs" company="Exit Games GmbH">
//   Part of: Asteroid Demo
// </copyright>
// <summary>
//  Asteroid Component
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;

using Random = UnityEngine.Random;
using Photon.Pun.UtilityScripts;
using Photon.Pun;

    public class Enemy : MonoBehaviour
    {
        public string color;
        public int lives = 1;
        private bool isDestroyed;
        public Material[] materials;
        private PhotonView photonView;

#pragma warning disable 0109
        private new Rigidbody rigidbody;
#pragma warning restore 0109

        #region UNITY

        public void Awake()
        {
            photonView = GetComponent<PhotonView>();

            rigidbody = GetComponent<Rigidbody>();
        
            
        }
    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {

            photonView.RPC("setColor", RpcTarget.AllViaServer, Random.Range(1, 4));
        }
    }
    [PunRPC]
        public void setColor(int i)
    {
        switch (i)
        {
            case 1:
                GetComponent<Renderer>().material = materials[0];
                color = "green";
                lives = 1;
                break;
            case 2:
                GetComponent<Renderer>().material = materials[1];
                color = "blue";
                lives = 2;
                break;
            case 3:
                GetComponent<Renderer>().material = materials[2];
                color = "red";
                lives = 3;
                break;


        }
    }

        public void Update()
        {
            if (!photonView.IsMine)
            {
                return;
            }

        }

        public void OnCollisionEnter(Collision collision)
        {
            if (isDestroyed)
            {
                return;
            }

            if (collision.gameObject.CompareTag("Bullet") && PhotonNetwork.IsMasterClient)
            {
                lives--;
                if (lives < 1)
                {

                    Bullet bullet = collision.gameObject.GetComponent<Bullet>();
                    switch (color)
                    {
                        case "green":
                            bullet.Owner.AddScore(1,transform.position.z);
                            break;
                        case "blue":
                            bullet.Owner.AddScore(2,transform.position.z);
                            break;
                        case "red":
                            bullet.Owner.AddScore(3,transform.position.z);
                         break;

                    }
                FindObjectOfType<spaceInvaderGameManager>().enemyMoveSpeed += .5f;
                    DestroyEnemyGlobally();
                    
                }
            }
            else if (collision.gameObject.CompareTag("Player"))
            {
                if (photonView.IsMine)
                {
                    collision.gameObject.GetComponent<PhotonView>().RPC("DestroySpaceship", RpcTarget.All);

                    DestroyEnemyGlobally();
                }
            }
        }

        #endregion

        private void DestroyEnemyGlobally()
        {
            isDestroyed = true;
            PhotonNetwork.Destroy(gameObject);
        }

        private void DestroyAsteroidLocally()
        {
            isDestroyed = true;

            GetComponent<Renderer>().enabled = false;
        }
    }
