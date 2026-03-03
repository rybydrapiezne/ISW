using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float health;

    public void GetDamage(float damage)
    {
        health -=damage;
        Debug.Log(health);
    }
}
