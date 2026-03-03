using System;
using TK_Shared._3DPlayerMovement;
using UnityEngine;

namespace PlayerShootingSystem
{
    public class PlayerShootingController: MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        public Gun currentGun;

        void Awake()
        {
            PlayerActionsController.OnPickedUp += HandlePickup;
        }

        public void Shoot()
        {
            Debug.Log("meep");
            if (currentGun)
            {
                currentGun.PerformShoot();
                if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit))
                {
                    if (hit.transform.TryGetComponent(out Enemy enemy))
                    {
                            enemy.GetDamage(currentGun.gunInfo.flatDamage);
                    }
                }
            }
        }

        void HandlePickup(Transform pickedObject)
        {
            if (pickedObject.TryGetComponent(out Gun gun))
            {
                currentGun = gun;
            }
        }
    }
}
