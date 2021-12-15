// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Spaceship.cs" company="Exit Games GmbH">
//   Part of: Asteroid Demo,
// </copyright>
// <summary>
//  Spaceship
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;

using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;

    public class Spaceship : MonoBehaviour
    {
        public float RotationSpeed = 90.0f;
        public float MovementSpeed = 2.0f;
        public float MaxSpeed = 0.2f;

        public ParticleSystem Destruction;
        public GameObject EngineTrail;
        public GameObject BulletPrefab;

        private PhotonView photonView;

#pragma warning disable 0109
        private new Rigidbody rigidbody;
        private new Collider collider;
        private new Renderer renderer;
#pragma warning restore 0109

        private float horizontal = 0.0f;
        private float vertical = 0.0f;
        private float shootingTimer = 0.0f;

        private bool controllable = true;

        #region UNITY

        public void Awake()
        {
            photonView = GetComponent<PhotonView>();

            rigidbody = GetComponent<Rigidbody>();
            collider = GetComponent<Collider>();
            renderer = GetComponent<Renderer>();
        }

        public void Start()
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                r.material.color = spaceInvadersGame.GetColor(photonView.Owner.GetPlayerNumber());
            }
        }

        public void Update()
        {
            if (!photonView.AmOwner || !controllable)
            {
                return;
            }

            // we don't want the master client to apply input to remote ships while the remote player is inactive
            if (this.photonView.CreatorActorNr != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return;
            }

            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            if (vertical == 1f && shootingTimer <= 0.0)
            {
                shootingTimer = 0.2f;

                photonView.RPC("Fire", RpcTarget.AllViaServer, rigidbody.position, rigidbody.rotation);
            }

            if (shootingTimer > 0.0f)
            {
                shootingTimer -= Time.deltaTime;
            }
        }
        public float max;
        public void FixedUpdate()
        {
            if (!photonView.IsMine)
            {
                return;
            }

            if (!controllable)
            {
                return;
            }


            Vector3 force = Vector3.right * horizontal * 1000.0f * MovementSpeed * Time.fixedDeltaTime;

            if (rigidbody.velocity.magnitude > (MaxSpeed * 1000.0f))
            {
                rigidbody.velocity = rigidbody.velocity.normalized * MaxSpeed * 1000.0f;
            }
            if(rigidbody.transform.position.x <= -max && rigidbody.velocity.x < 0f)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.transform.position = new Vector3(-max,rigidbody.transform.position.y,rigidbody.transform.position.z);
            }
            else if (rigidbody.transform.position.x >= max && rigidbody.velocity.x > 0f)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.transform.position = new Vector3(max,rigidbody.transform.position.y,rigidbody.transform.position.z);
            }
            else
                rigidbody.AddForce(force);

            CheckExitScreen();
        }

        #endregion

        #region COROUTINES

        private IEnumerator WaitForRespawn()
        {
            yield return new WaitForSeconds(spaceInvadersGame.PLAYER_RESPAWN_TIME);

            photonView.RPC("RespawnSpaceship", RpcTarget.AllViaServer);
        }

        #endregion

        #region PUN CALLBACKS

        [PunRPC]
        public void DestroySpaceship()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            collider.enabled = false;
            renderer.enabled = false;

            controllable = false;

            EngineTrail.SetActive(false);
            Destruction.Play();

            if (photonView.IsMine)
            {
                object lives;
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(spaceInvadersGame.PLAYER_LIVES, out lives))
                {
                    PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable {{spaceInvadersGame.PLAYER_LIVES, ((int) lives <= 1) ? 0 : ((int) lives - 1)}});

                    if (((int) lives) > 1)
                    {
                        StartCoroutine("WaitForRespawn");
                    }
                }
            }
        }

        [PunRPC]
        public void Fire(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
        {
            float lag = (float) (PhotonNetwork.Time - info.SentServerTime);
            GameObject bullet;

            /** Use this if you want to fire one bullet at a time **/
            bullet = Instantiate(BulletPrefab, position, Quaternion.identity) as GameObject;
            bullet.GetComponent<Bullet>().InitializeBullet(photonView.Owner, (rotation * Vector3.forward), Mathf.Abs(lag));


            /** Use this if you want to fire two bullets at once **/
            //Vector3 baseX = rotation * Vector3.right;
            //Vector3 baseZ = rotation * Vector3.forward;

            //Vector3 offsetLeft = -1.5f * baseX - 0.5f * baseZ;
            //Vector3 offsetRight = 1.5f * baseX - 0.5f * baseZ;

            //bullet = Instantiate(BulletPrefab, rigidbody.position + offsetLeft, Quaternion.identity) as GameObject;
            //bullet.GetComponent<Bullet>().InitializeBullet(photonView.Owner, baseZ, Mathf.Abs(lag));
            //bullet = Instantiate(BulletPrefab, rigidbody.position + offsetRight, Quaternion.identity) as GameObject;
            //bullet.GetComponent<Bullet>().InitializeBullet(photonView.Owner, baseZ, Mathf.Abs(lag));
        }

        [PunRPC]
        public void RespawnSpaceship()
        {
            collider.enabled = true;
            renderer.enabled = true;

            controllable = true;

            EngineTrail.SetActive(true);
            Destruction.Stop();
        }
        
        #endregion

        private void CheckExitScreen()
        {
            if (Camera.main == null)
            {
                return;
            }
            
            if (Mathf.Abs(rigidbody.position.x) > (Camera.main.orthographicSize * Camera.main.aspect))
            {
                rigidbody.position = new Vector3(-Mathf.Sign(rigidbody.position.x) * Camera.main.orthographicSize * Camera.main.aspect, 0, rigidbody.position.z);
                rigidbody.position -= rigidbody.position.normalized * 0.1f; // offset a little bit to avoid looping back & forth between the 2 edges 
            }

            if (Mathf.Abs(rigidbody.position.z) > Camera.main.orthographicSize)
            {
                rigidbody.position = new Vector3(rigidbody.position.x, rigidbody.position.y, -Mathf.Sign(rigidbody.position.z) * Camera.main.orthographicSize);
                rigidbody.position -= rigidbody.position.normalized * 0.1f; // offset a little bit to avoid looping back & forth between the 2 edges 
            }
        }
    }
