using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerShootingSystem
{
    [RequireComponent(typeof(PlayerShootingController))]
    public class PlayerShootingInputController : MonoBehaviour
    {
        PlayerShootingController _shootingController;
        void Awake()
        {
            _shootingController = GetComponent<PlayerShootingController>();
        }

        void OnShoot(InputValue inputValue)
        {
            Debug.Log(inputValue);
            if (inputValue.isPressed)
                _shootingController.Shoot();
                
        }
    }
}
