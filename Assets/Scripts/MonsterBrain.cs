using UnityEngine;
using UnityEngine.AI;

namespace ForestCleanup {
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterBrain : MonoBehaviour {
    public enum Mode { Wander, Chase }
    public Mode State { get; private set; }
    public Transform visualRoot;
    [Min(0.1f)] public float sightRange = 12f;
    [Range(1f, 360f)] public float fieldOfView = 120f;
    [Min(1f)] public float wanderRadius = 14f;
    public LayerMask sightBlockers = 1 << 0;

    NavMeshAgent agent;
    NavMeshPath wanderPath;
    float wanderRetry, footstep;

    void Awake() { agent = GetComponent<NavMeshAgent>(); wanderPath = new NavMeshPath(); }
    void Start() { BeginWandering(); }

    public bool ClearSight(Vector3 target) {
        Vector3 eye = transform.position + Vector3.up * 1.45f;
        return !Physics.Linecast(eye, target, sightBlockers, QueryTriggerInteraction.Ignore);
    }

    public bool SeesPlayer() {
        var game = GameSession.Current;
        if (game == null || game.player == null) return false;
        Vector3 direction = game.player.transform.position - transform.position;
        if (direction.sqrMagnitude > sightRange * sightRange) return false;
        // Horizontal cone: looking up/down at the player's camera must not shrink the cone.
        direction.y = 0;
        return Vector3.Angle(transform.forward, direction) <= fieldOfView * .5f
            && ClearSight(game.player.view.transform.position);
    }

    void BeginWandering() {
        State = Mode.Wander;
        agent.speed = 2f;
        if (!agent.isOnNavMesh) return;
        agent.ResetPath(); // Discard the player's last location immediately.
        PickWanderDestination();
    }

    void PickWanderDestination() {
        wanderRetry = 1f;
        for (int i = 0; i < 24; i++) {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0, offset.y);
            if (!NavMesh.SamplePosition(candidate, out var hit, 2f, agent.areaMask)) continue;
            if ((hit.position - transform.position).sqrMagnitude < 9f) continue;
            if (!agent.CalculatePath(hit.position, wanderPath)
                || wanderPath.status != NavMeshPathStatus.PathComplete) continue;
            if (agent.SetPath(wanderPath)) return;
        }
        // If this area has no valid destination, stop and retry rather than following a stale path.
        agent.ResetPath();
    }

    void Update() {
        var game = GameSession.Current;
        if (game == null || !agent.isOnNavMesh) return;
        agent.isStopped = !game.Playing || game.Paused;
        if (agent.isStopped) return;

        Vector3 target = game.player.transform.position;
        if (Vector3.Distance(transform.position, target) < 1.2f
            && ClearSight(game.player.view.transform.position)) {
            game.Finish(false);
            return;
        }

        if (SeesPlayer()) {
            State = Mode.Chase;
            agent.speed = 4f;
            agent.SetDestination(target);
        } else if (State == Mode.Chase) {
            BeginWandering();
        } else {
            wanderRetry -= Time.deltaTime;
            if (!agent.pathPending && wanderRetry <= 0f
                && (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete
                    || agent.remainingDistance <= agent.stoppingDistance + .3f)) {
                PickWanderDestination();
            }
        }

        footstep -= Time.deltaTime;
        if (agent.velocity.sqrMagnitude > .2f && footstep <= 0f) {
            SoundBank.Play(transform.position, 65, .65f, .18f);
            footstep = State == Mode.Chase ? .35f : .65f;
        }
    }
}
}

