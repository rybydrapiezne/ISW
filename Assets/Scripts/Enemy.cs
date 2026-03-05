using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float health;
    [SerializeField] GameObject enemyMesh;
    
    Coroutine _coroutine;
    
    public void GetDamage(float damage)
    {
        if (_coroutine != null) return;
        health -= damage;
        Debug.Log(health);
        if (health <= 0)
            _coroutine = StartCoroutine(Respawner());

    }

    IEnumerator Respawner()
    {
        enemyMesh.SetActive(false);
        yield return new WaitForSeconds(3);
        enemyMesh.SetActive(true);
        health = 100;
        _coroutine = null;
    }
}
