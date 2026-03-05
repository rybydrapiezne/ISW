using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class AICore : MonoBehaviour
{
    // --- Enums ---
    public enum AIState { Patrol, Alert, Combat }
    public enum AlertLevel { None, Low, Medium, High, Extreme } // Empty, Grey, Yellow, Red
    public enum EnemyType { Glock, Shotgun }

    // --- Events ---
    public static event Action<AICore, float, AlertLevel> OnAlertChanged;
    public static event Action<AICore> OnEnemyDied;

    // --- Configurations ---
    [Header("General Settings")]
    [SerializeField] EnemyType enemyType = EnemyType.Glock;
    [Tooltip("The tag of the player object.")]
    [SerializeField] string playerTag = "Player";

    [Header("Movement & Speed")]
    [SerializeField] float basePlayerVelocity = 4f;
    [Tooltip("Percentage of player base speed.")]
    [SerializeField] float patrolSpeedMultiplier = 1f;
    [Tooltip("Percentage of player base speed.")]
    [SerializeField] float combatSpeedMultiplier = 1.3f;
    [Tooltip("Percentage of player base speed.")]
    [SerializeField] float retreatSpeedMultiplier = 0.3f;

    [Header("Vision Settings")]
    [Tooltip("Layers that block line of sight (e.g., Default, Environment, Player).")]
    [SerializeField] LayerMask sightObstaclesMask;
    [Tooltip("Height of eyes from ground level.")]
    [SerializeField] float eyeLevel = 1.7f;
    float playerEyeLevel => playerTransform.GetComponent<PlayerActionsController>().eyeLevel;
    [Tooltip("The total angle of the enemy's field of view in degrees (e.g., 120 means 60 degrees to each side).")]
    public float fieldOfViewAngle = 120f;
    [Tooltip("Maximum distance the enemy can see the player.")]
    public float maxSightDistance = 40f;

    [Header("Combat Settings")]
    [SerializeField] float maxHealth = 75f; // assume 75% of default player health
    [Tooltip("How far the enemy can shoot.")]
    [SerializeField] float weaponRange = 20f;
    [SerializeField] int maxAmmo = 15;
    [SerializeField] float reloadTime = 1.7f;
    [Tooltip("Optimal distance to maintain from player during combat (as a percentage of weapon range).")]
    [SerializeField] float optimalCombatDistancePct = 0.7f;

    [Header("Alert System")]
    [Tooltip("How fast the Trigger Multiplier (TM) grows per second of exposure. TM = exposureTime / distance. Adjust this to tune sensitivity.")]
    [SerializeField] float alertSensitivity = 1f;
    [Tooltip("Radius around the enemy where sounds are heard.")]
    [SerializeField] float hearingRadius = 30f;
    [SerializeField] float timeToLoseAlertLevel = 3f;

    [Header("Patrol Settings")]
    [Tooltip("Assign empty game objects as patrol points. Should be on the ground.")]
    [SerializeField] Transform[] patrolPoints;
    [Tooltip("Time to wait at each patrol point.")]
    [SerializeField] float waitTimeAtWaypoint = 2f;

    // --- State Variables (Read-only for debugging) ---
    [Header("Current State (Read Only)")]
    public AIState currentState = AIState.Patrol;
    public AlertLevel currentAlertLevel = AlertLevel.None;
    public float triggerMultiplier = 0f;
    public float currentHealth;
    public int currentAmmo;

    // --- Internal References ---
    NavMeshAgent agent;
    Transform playerTransform;
    Vector3 lastKnownPlayerPosition;

    float currentExposureTimer = 0f;
    float lastAlertTime = 0f;
    int currentPatrolIndex = 0;
    bool isWaiting = false;
    bool isReloading = false;

    // Static list to manage combat target positions to avoid overlap
    static List<Vector3> activeCombatTargets = new List<Vector3>();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Couldn't find player");
        }

        agent.speed = basePlayerVelocity * patrolSpeedMultiplier;
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (currentHealth <= 0) return;

        UpdateAlertSystem();

        OnAlertChanged?.Invoke(this, triggerMultiplier, currentAlertLevel);

        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrolState();
                break;
            case AIState.Alert:
                UpdateAlertState();
                break;
            case AIState.Combat:
                UpdateCombatState();
                break;
        }
    }

    private void UpdateAlertSystem()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (HasLineOfSight())
        {

            // currentExposureTimer += Time.deltaTime * alertSensitivity;

            // TM = exposure_time / distance [PH]
            triggerMultiplier += Time.deltaTime * alertSensitivity / distanceToPlayer;
            lastKnownPlayerPosition = playerTransform.position;
            lastAlertTime = Time.time;

            DetermineAlertLevel();
        }
        else
        {
            // TM decay
            if (triggerMultiplier > 0)
            {
                triggerMultiplier -= Time.deltaTime;
                OnAlertChanged?.Invoke(this, triggerMultiplier, currentAlertLevel);
            }
            else
            {
                triggerMultiplier = 0f;
            }
            // currentExposureTimer = 0f;

            if (currentState != AIState.Combat && Time.time - lastAlertTime > timeToLoseAlertLevel)
            {
                // degrade alert level //

                if (currentAlertLevel == AlertLevel.None)
                {
                    ChangeState(AIState.Patrol);
                }
            }
        }
    }

    private void DetermineAlertLevel()
    {
        if (triggerMultiplier <= 0) return;

        if (triggerMultiplier <= 0.25f)
        {
            SetAlertLevel(AlertLevel.Low); // Empty indicator, appears for 1s
        }
        else if (triggerMultiplier <= 0.75f)
        {
            SetAlertLevel(AlertLevel.Medium); // Grey indicator, stands still, looks around 3s
        }
        else if (triggerMultiplier <= 1.0f)
        {
            SetAlertLevel(AlertLevel.High); // Yellow indicator, goes to last known pos, looks 5s
        }
        else // TM > 1
        {
            SetAlertLevel(AlertLevel.Extreme); // Red indicator, Combat
        }
    }

    private void SetAlertLevel(AlertLevel newLevel)
    {
        if (currentAlertLevel == newLevel) return;
        currentAlertLevel = newLevel;

        StopAllCoroutines(); // Stop any pending wait/look behaviors

        switch (currentAlertLevel)
        {
            case AlertLevel.Low:
                // Do nothing to movement
                agent.speed = basePlayerVelocity * patrolSpeedMultiplier;
                agent.isStopped = false;
                currentAlertLevel = AlertLevel.None;
                break;
            case AlertLevel.Medium:
                StartCoroutine(LookAroundRoutine(3f));
                break;
            case AlertLevel.High:
                StartCoroutine(InvestigateRoutine(5f));
                break;
            case AlertLevel.Extreme:
                ChangeState(AIState.Combat);
                AlertNearbyEnemies();
                break;
        }
    }

    private void ChangeState(AIState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        isWaiting = false; // Reset waiting flag

        switch (newState)
        {
            case AIState.Patrol:
                agent.speed = basePlayerVelocity * patrolSpeedMultiplier;
                agent.isStopped = false;
                currentAlertLevel = AlertLevel.None;
                GoToNextPatrolPoint();
                break;
            case AIState.Alert:
                agent.speed = basePlayerVelocity * patrolSpeedMultiplier; // Normal speed
                break;
            case AIState.Combat:
                agent.isStopped = false;
                // Combat speed set in UpdateCombatState
                break;
        }
    }

    // --- State Behaviors ---

    private void UpdatePatrolState()
    {
        if (patrolPoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.5f && !isWaiting)
        {
            StartCoroutine(PatrolWaitRoutine());
        }
    }

    private void UpdateAlertState()
    {
        // Movement is handled by Coroutines (LookAroundRoutine, InvestigateRoutine) triggered by SetAlertLevel
    }

    private void UpdateCombatState()
    {
        if (playerTransform == null || isReloading) return;

        agent.speed = basePlayerVelocity * combatSpeedMultiplier;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float optimalDistance = weaponRange * optimalCombatDistancePct;

        // Face the player
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    lookRotation,
                    agent.angularSpeed * Time.deltaTime
                );

        if (distanceToPlayer <= weaponRange && HasLineOfSight())
        {
            if (distanceToPlayer <= optimalDistance)
            {
                // Player is too close, back away
                agent.speed = basePlayerVelocity * retreatSpeedMultiplier;

                Vector3 retreatDirection = transform.position - playerTransform.position;
                Vector3 retreatTarget = transform.position + retreatDirection.normalized * 2f;
                agent.SetDestination(retreatTarget);
            }
            else
            {
                // Optimal range, stop and shoot
                agent.SetDestination(transform.position);
            }

            if (currentAmmo > 0)
            {
                Shoot();
            }
            else if (!isReloading)
            {
                StartCoroutine(ReloadRoutine());
            }
        }
        else
        {
            // Player out of range, chase
            agent.SetDestination(playerTransform.position);
        }
    }

    // --- Helper Methods & Coroutines ---

    private bool HasLineOfSight()
    {
        if (playerTransform == null) return false;

        // 1. Distance Check
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > maxSightDistance) return false;

        Vector3 rayStartOrigin = transform.position + Vector3.up * eyeLevel;
        Vector3 targetPosition = playerTransform.position + Vector3.up * playerEyeLevel;
        Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;

        // 2. Field of View Angle Check
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > fieldOfViewAngle / 2f)
        {
            return false; // Player is outside the vision cone
        }

        // 3. Raycast Check from eye level
        if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, maxSightDistance, sightObstaclesMask))
        {
            //Debug.Log("hit: " + hit.collider.gameObject.name);
            if (hit.collider.CompareTag(playerTag))
            {
                return true;
            }
        }

        return false;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        Debug.Log("Current patrol node: " + currentPatrolIndex);
        agent.destination = patrolPoints[currentPatrolIndex].position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private IEnumerator PatrolWaitRoutine()
    {
        isWaiting = true;
        agent.isStopped = true;
        yield return StartCoroutine(SweepRotationRoutine(waitTimeAtWaypoint, false, 40));
        agent.isStopped = false;
        GoToNextPatrolPoint();
        isWaiting = false;
    }

    private IEnumerator LookAroundRoutine(float duration)
    {
        ChangeState(AIState.Alert);
        agent.isStopped = true;

        Debug.Log("looking around for " + duration);
        yield return StartCoroutine(SweepRotationRoutine(duration, true));

        agent.isStopped = false;
        ChangeState(AIState.Patrol); // Return to patrol
    }

    private IEnumerator InvestigateRoutine(float duration)
    {
        ChangeState(AIState.Alert);
        agent.isStopped = false;
        agent.SetDestination(lastKnownPlayerPosition);

        // Wait until we reach the destination
        while (agent.pathPending || agent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        // Reached location, look around
        Debug.Log("investigating for " + duration + " seconds");
        agent.isStopped = true;
        yield return StartCoroutine(SweepRotationRoutine(duration, false));

        agent.isStopped = false;
        ChangeState(AIState.Patrol);
    }

    private IEnumerator SweepRotationRoutine(float duration, bool trackLastKnownPosition, float lookAngle=70f)
    {
        float timer = 0f;
        bool lookingLeft = true;
        Quaternion centerRotation = transform.rotation;

        while (timer < duration)
        {
            if (trackLastKnownPosition)
            {
                Vector3 direction = (lastKnownPlayerPosition - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    centerRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                }

                if (HasLineOfSight())
                {
                    timer = 0f;
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        centerRotation,
                        agent.angularSpeed * Time.deltaTime
                    );
                    
                    yield return null;
                    continue;
                }
            }
            else
            {
                if (HasLineOfSight()) break;
            }

            timer += Time.deltaTime;

            Quaternion targetSweep = centerRotation * Quaternion.Euler(0, lookingLeft ? -lookAngle : lookAngle, 0);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetSweep,
                agent.angularSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(transform.rotation, targetSweep) < 1f)
            {
                lookingLeft = !lookingLeft;
            }

            yield return null;
        }
    }

    private void AlertNearbyEnemies()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hearingRadius);
        foreach (var hitCollider in hitColliders)
        {
            AICore nearbyEnemy = hitCollider.GetComponent<AICore>();
            if (nearbyEnemy != null && nearbyEnemy != this && nearbyEnemy.currentState != AIState.Combat)
            {
                // Force them into combat
                nearbyEnemy.triggerMultiplier = 2f; // Force above 1
                nearbyEnemy.DetermineAlertLevel();
            }
        }
    }

    private void Shoot()
    {
        // depends weapon system
        // currentAmmo--;
        Debug.Log($"{gameObject.name} is shooting!");
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        agent.isStopped = true; // Stop moving completely during reload
        Debug.Log($"{gameObject.name} is reloading...");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        agent.isStopped = false;
        Debug.Log($"{gameObject.name} finished reloading.");
    }

    // --- Damage Handling ---
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        // If hit from stealth, immediately alert
        if (currentState != AIState.Combat)
        {
            triggerMultiplier = 2f;
            DetermineAlertLevel();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void StealthKill()
    {
        if (currentState != AIState.Combat && currentAlertLevel == AlertLevel.None)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        OnEnemyDied?.Invoke(this);
        activeCombatTargets.Remove(agent.destination);
        Destroy(gameObject);
    }

    // --- "First Come First Served" Destination Logic ---
    // not used becasue was doing some crazy stuff to pathing
    private Vector3 GetUniqueDestination(Vector3 desiredTarget)
    {
        Vector3 finalTarget = desiredTarget;
        float avoidanceRadius = 2f; // How far apart they should stay

        // Remove old destination for this specific agent
        if (activeCombatTargets.Contains(agent.destination))
        {
            activeCombatTargets.Remove(agent.destination);
        }

        foreach (Vector3 existingTarget in activeCombatTargets)
        {
            if (Vector3.Distance(finalTarget, existingTarget) < avoidanceRadius)
            {
                // Target is taken, find a slight offset
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * avoidanceRadius;
                finalTarget += new Vector3(randomOffset.x, 0, randomOffset.y);
            }
        }

        // Add new destination
        activeCombatTargets.Add(finalTarget);
        return finalTarget;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerTransform == null) return;

        Vector3 rayStartOrigin = transform.position + Vector3.up * eyeLevel;
        Vector3 targetPosition = playerTransform.position + Vector3.up * playerEyeLevel;
        Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        Gizmos.color = Color.black; // Default color: trying to see

        // Draw the Raycast
        if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, maxSightDistance, sightObstaclesMask))
        {
            DrawVisibilityRaycast(rayStartOrigin, hit, angleToPlayer);
        }
        else
        {
            // Didn't hit anything
            Gizmos.DrawLine(rayStartOrigin, rayStartOrigin + directionToPlayer * maxSightDistance);
        }
    }

    private void DrawVisibilityRaycast(Vector3 rayStartOrigin, RaycastHit hit, float angleToPlayer)
    {
        if (hit.collider.CompareTag(playerTag))
        {
            if (angleToPlayer > fieldOfViewAngle / 2f)
            {
                Gizmos.color = Color.blue; // Blue: Player behind field of view
            }
            else
            {
                Gizmos.color = Color.green; // Green: Sees Player
            }
            Gizmos.DrawLine(rayStartOrigin, hit.point);
            Gizmos.DrawSphere(hit.point, 0.1f);
        }
        else
        {
            Gizmos.color = Color.red; // Red: Hit a wall/obstacle
            Gizmos.DrawLine(rayStartOrigin, hit.point);
            Gizmos.DrawSphere(hit.point, 0.1f);
        }
    }
#endif
}
