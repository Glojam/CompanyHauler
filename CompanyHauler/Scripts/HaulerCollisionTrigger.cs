using CompanyHauler.Scripts;
using GameNetcodeStuff;
using System.ComponentModel;
using UnityEngine;

public class HaulerCollisionTrigger : MonoBehaviour
{
    public HaulerController mainScript = null!;
    public BoxCollider insideTruckNavMeshBounds = null!;
    public EnemyAI[] enemiesLastHit = null!;

    private float timeSinceHittingPlayer;
    private float timeSinceHittingEnemy;
    private int enemyIndex;

    public void Start()
    {
        enemiesLastHit = new EnemyAI[3];
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!mainScript.hasBeenSpawned || (mainScript.magnetedToShip && mainScript.magnetTime > 0.8f))
            return;

        if (other.CompareTag("Player"))
        {
            if (!other.TryGetComponent<PlayerControllerB>(out var playerController))
                return;

            if (Time.realtimeSinceStartup - timeSinceHittingPlayer < 0.25f)
                return;

            Transform physicsTransform = mainScript.physicsRegion.physicsTransform;
            if (playerController.overridePhysicsParent == physicsTransform ||
                playerController.physicsParent == physicsTransform)
            {
                return;
            }

            float velocityMagnitude = Mathf.Abs(mainScript.averageVelocity.magnitude);
            if (velocityMagnitude < 2f)
            {
                return;
            }

            Vector3 directionToPlayer = playerController.transform.position - mainScript.mainRigidbody.position;
            float angle = Vector3.Angle(Vector3.Normalize(mainScript.averageVelocity * 1000f), Vector3.Normalize(directionToPlayer * 1000f));
            if (angle > 70f)
                return;

            if (angle < 30f && mainScript.wheelRPM > 400f)
            {
                velocityMagnitude += 6f;
            }

            if ((playerController.gameplayCamera.transform.position - mainScript.mainRigidbody.position).y < -0.1f)
            {
                velocityMagnitude *= 2f;
            }

            timeSinceHittingPlayer = Time.realtimeSinceStartup;
            Vector3 impactForce = Vector3.ClampMagnitude(mainScript.averageVelocity, 40f);

            if (playerController == GameNetworkManager.Instance.localPlayerController)
            {
                if (physicsTransform == GameNetworkManager.Instance.localPlayerController.physicsParent)
                {
                    return;
                }
                if (velocityMagnitude > 20f)
                {
                    GameNetworkManager.Instance.localPlayerController.KillPlayer(impactForce, spawnBody: true, CauseOfDeath.Crushing, 0, default, false);
                }
                else
                {
                    int damage = 0;
                    if (velocityMagnitude > 15f) damage = 80;
                    else if (velocityMagnitude > 12f) damage = 60;
                    else if (velocityMagnitude > 8f) damage = 40;

                    if (damage > 0)
                    {
                        GameNetworkManager.Instance.localPlayerController.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing, 0, fallDamage: false, impactForce);
                    }
                }

                if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead &&
                    GameNetworkManager.Instance.localPlayerController.externalForceAutoFade.sqrMagnitude < mainScript.averageVelocity.sqrMagnitude)
                {
                    GameNetworkManager.Instance.localPlayerController.externalForceAutoFade = mainScript.averageVelocity;
                }
            }
            else if (mainScript.IsOwner && mainScript.averageVelocity.magnitude > 1.8f)
            {
                mainScript.CarReactToObstacle(mainScript.averageVelocity, playerController.transform.position, mainScript.averageVelocity, CarObstacleType.Player);
            }
        }
        else
        {
            if (!other.gameObject.CompareTag("Enemy"))
                return;

            if (Time.realtimeSinceStartup - timeSinceHittingEnemy < 0.25f)
                return;

            if (!other.TryGetComponent<EnemyAICollisionDetect>(out var enemyAIcollision))
                return;

            if (enemyAIcollision.mainScript == null)
                return;

            if (enemyAIcollision.mainScript.isEnemyDead)
                return;

            // allow worms to catapult the vehicle
            if (enemyAIcollision.mainScript is SandWormAI)
            {
                timeSinceHittingEnemy = Time.realtimeSinceStartup;
                mainScript.mainRigidbody.AddExplosionForce(mainScript.mainRigidbody.mass * 100f, mainScript.transform.position + mainScript.transform.forward + Vector3.up * 1.5f, 12f, 3f, ForceMode.Impulse);
                return;
            }

            // prevent tulip-snakes bouncing the vehicle if actively clinging to a player
            if (enemyAIcollision.mainScript is FlowerSnakeEnemy flowerSnake && flowerSnake.clingingToPlayer)
                return;

            if (Vector3.Angle(mainScript.averageVelocity, enemyAIcollision.mainScript.transform.position - transform.position) > 130f)
                return;

            if (insideTruckNavMeshBounds.ClosestPoint(enemyAIcollision.mainScript.transform.position) == enemyAIcollision.mainScript.transform.position ||
                insideTruckNavMeshBounds.ClosestPoint(enemyAIcollision.mainScript.agent.destination) == enemyAIcollision.mainScript.agent.destination)
            {
                return;
            }

            bool dealDamage = false;
            for (int i = 0; i < enemiesLastHit.Length; i++)
            {
                if (enemiesLastHit[i] == enemyAIcollision.mainScript)
                {
                    if (Time.realtimeSinceStartup - timeSinceHittingEnemy < 0.6f || 
                        mainScript.averageVelocity.magnitude < 4f)
                    {
                        dealDamage = true;
                    }
                }
            }

            if (mainScript.averageVelocity.magnitude < 6f && (!mainScript.ignitionStarted || mainScript.gear == CarGearShift.Park || Mathf.Abs(mainScript.wheelRPM) < 40f))
            {
                dealDamage = false;
            }

            timeSinceHittingEnemy = Time.realtimeSinceStartup;
            Vector3 position = enemyAIcollision.transform.position;
            bool enemyDamageByCar = false;

            switch (enemyAIcollision.mainScript.enemyType.EnemySize)
            {
                case EnemySize.Tiny:
                    enemyDamageByCar = mainScript.CarReactToObstacle(mainScript.averageVelocity, position, mainScript.averageVelocity, CarObstacleType.Enemy, 1f, enemyAIcollision.mainScript, dealDamage);
                    break;
                case EnemySize.Giant:
                    enemyDamageByCar = mainScript.CarReactToObstacle(mainScript.averageVelocity, position, mainScript.averageVelocity, CarObstacleType.Enemy, 3f, enemyAIcollision.mainScript, dealDamage);
                    break;
                case EnemySize.Medium:
                    enemyDamageByCar = mainScript.CarReactToObstacle(mainScript.averageVelocity, position, mainScript.averageVelocity, CarObstacleType.Enemy, 2f, enemyAIcollision.mainScript, dealDamage);
                    break;
            }

            if (enemyDamageByCar)
            {
                enemyIndex = (enemyIndex + 1) % 3;
                enemiesLastHit[enemyIndex] = enemyAIcollision.mainScript;
                return;
            }

            for (int j = 0; j < enemiesLastHit.Length; j++)
            {
                if (enemiesLastHit[j] == enemyAIcollision.mainScript)
                {
                    enemiesLastHit[j] = null!;
                }
            }
        }
    }
}
