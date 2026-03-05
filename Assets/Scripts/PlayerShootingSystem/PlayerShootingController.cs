using System;
using System.Collections;
using TK_Shared._3DPlayerMovement; // assuming this namespace exists
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerShootingSystem
{
    public class PlayerShootingController : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        public Gun currentGun;

        bool _isHoldingShoot;
        Coroutine _autoFireCoroutine;

        void Awake()
        {
            PlayerActionsController.OnPickedUp += HandlePickup;
        }

        void OnDestroy()
        {
            PlayerActionsController.OnPickedUp -= HandlePickup;
            if (_autoFireCoroutine != null) StopCoroutine(_autoFireCoroutine);
        }

        public void OnShootInput(InputValue value)
        {
            _isHoldingShoot = value.isPressed;

            if (currentGun == null) return;

            if (_isHoldingShoot)
            {
                TryShootOnce();

                if (currentGun.gunInfo.isAutomatic && _autoFireCoroutine == null)
                {
                    _autoFireCoroutine = StartCoroutine(AutomaticFireLoop());
                }
            }
            else
            {
                if (_autoFireCoroutine != null)
                {
                    StopCoroutine(_autoFireCoroutine);
                    _autoFireCoroutine = null;
                }
            }
        }

        void TryShootOnce()
        {
            if (!currentGun) return;
            currentGun.PerformShoot();

            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent(out AICore enemy))
                {
                    float distance = hit.distance;
                    float multiplier = currentGun.gunInfo.damageFalloff.Evaluate(distance/100f);
                    float finalDamage = currentGun.gunInfo.flatDamage * multiplier;
                    enemy.TakeDamage(finalDamage);
                }
            }
        }

        IEnumerator AutomaticFireLoop()
        {
            while (_isHoldingShoot && currentGun && currentGun.gunInfo.isAutomatic)
            {
                yield return new WaitForSeconds(currentGun.gunInfo.fireRate);
                TryShootOnce();
            }

            _autoFireCoroutine = null;
        }

        void HandlePickup(Transform pickedObject)
        {
            if (pickedObject.TryGetComponent(out Gun gun))
            {
                currentGun = gun;
                gun.PickedUp();

                if (_autoFireCoroutine != null)
                {
                    StopCoroutine(_autoFireCoroutine);
                    _autoFireCoroutine = null;
                }
            }
        }
    }
}