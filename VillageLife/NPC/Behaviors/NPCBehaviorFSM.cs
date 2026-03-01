using UnityEngine;
using VillageLife.Util;

namespace VillageLife.NPC.Behaviors
{
    public enum BehaviorState
    {
        Idle,
        Wandering,
        Interacting,
        GoingHome,
        Sleeping,
        Fleeing,
        Working
    }

    /// <summary>
    /// Finite state machine controlling NPC behaviors.
    /// Handles wandering, day/night schedules, fleeing from danger, and role-specific work states.
    /// </summary>
    public class NPCBehaviorFSM : MonoBehaviour
    {
        private VillageNPC _npc;
        private BehaviorState _currentState = BehaviorState.Idle;
        private float _stateTimer;
        private float _wanderIdleTime;
        private Vector3 _wanderTarget;
        private float _interactionTimeout = 5f;

        public BehaviorState CurrentState => _currentState;

        private void Awake()
        {
            _npc = GetComponent<VillageNPC>();
        }

        private void Update()
        {
            if (_npc == null || _npc.ZNetView == null || _npc.ZNetView.GetZDO() == null)
                return;

            // Only run AI on the owner
            if (!_npc.ZNetView.IsOwner())
                return;

            _stateTimer += Time.deltaTime;

            // Check schedule
            float dayFraction = EnvMan.instance != null ? EnvMan.instance.GetDayFraction() : 0.5f;

            switch (_currentState)
            {
                case BehaviorState.Idle:
                    UpdateIdle(dayFraction);
                    break;
                case BehaviorState.Wandering:
                    UpdateWandering();
                    break;
                case BehaviorState.Interacting:
                    UpdateInteracting();
                    break;
                case BehaviorState.GoingHome:
                    UpdateGoingHome();
                    break;
                case BehaviorState.Sleeping:
                    UpdateSleeping(dayFraction);
                    break;
                case BehaviorState.Fleeing:
                    UpdateFleeing();
                    break;
                case BehaviorState.Working:
                    UpdateWorking(dayFraction);
                    break;
            }

            // Update role
            _npc.Role?.OnUpdate(_npc, Time.deltaTime);
        }

        public void SetState(BehaviorState newState)
        {
            if (_currentState == newState) return;

            OnExitState(_currentState);
            _currentState = newState;
            _stateTimer = 0f;
            OnEnterState(newState);

            // Sync state to ZDO
            var zdo = _npc.ZNetView?.GetZDO();
            zdo?.Set(ZDOHelper.Hash(ZDOHelper.KeyBehaviorState), (int)newState);
        }

        private void OnEnterState(BehaviorState state)
        {
            switch (state)
            {
                case BehaviorState.Wandering:
                    PickWanderTarget();
                    break;
                case BehaviorState.Sleeping:
                    // Could trigger sleep animation
                    break;
                case BehaviorState.Interacting:
                    _interactionTimeout = 5f;
                    break;
            }
        }

        private void OnExitState(BehaviorState state)
        {
            // Cleanup for exited state if needed
        }

        #region State Updates

        private void UpdateIdle(float dayFraction)
        {
            // Check if it's bedtime
            if (dayFraction >= Constants.DuskFraction)
            {
                SetState(BehaviorState.GoingHome);
                return;
            }

            // After idling for a random duration, start wandering
            if (_stateTimer >= _wanderIdleTime)
            {
                _wanderIdleTime = Random.Range(Constants.WanderIdleMinSeconds, Constants.WanderIdleMaxSeconds);
                SetState(BehaviorState.Wandering);
            }
        }

        private void UpdateWandering()
        {
            Vector3 pos = transform.position;
            Vector3 direction = (_wanderTarget - pos);
            direction.y = 0;

            if (direction.magnitude < 0.5f)
            {
                // Reached target, go back to idle
                SetState(BehaviorState.Idle);
                return;
            }

            // Move toward wander target
            Vector3 move = direction.normalized * 1.5f * Time.deltaTime;
            transform.position += move;
            transform.rotation = Quaternion.LookRotation(direction.normalized);

            // Timeout - don't wander forever
            if (_stateTimer > 30f)
                SetState(BehaviorState.Idle);
        }

        private void UpdateInteracting()
        {
            _interactionTimeout -= Time.deltaTime;
            if (_interactionTimeout <= 0f)
            {
                SetState(BehaviorState.Idle);
            }
        }

        private void UpdateGoingHome()
        {
            Vector3 home = _npc.GetHomePosition();
            Vector3 direction = (home - transform.position);
            direction.y = 0;

            if (direction.magnitude < 1f)
            {
                // Home reached
                float dayFraction = EnvMan.instance != null ? EnvMan.instance.GetDayFraction() : 0.5f;
                if (dayFraction >= Constants.SleepFraction || dayFraction < Constants.DawnFraction)
                    SetState(BehaviorState.Sleeping);
                else
                    SetState(BehaviorState.Idle);
                return;
            }

            // Walk home
            Vector3 move = direction.normalized * 2f * Time.deltaTime;
            transform.position += move;
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        private void UpdateSleeping(float dayFraction)
        {
            // Wake up at dawn
            if (dayFraction >= Constants.DawnFraction && dayFraction < Constants.DuskFraction)
            {
                SetState(BehaviorState.Idle);
            }
        }

        private void UpdateFleeing()
        {
            // Flee back toward home
            Vector3 home = _npc.GetHomePosition();
            Vector3 direction = (home - transform.position);
            direction.y = 0;

            if (direction.magnitude < 2f)
            {
                SetState(BehaviorState.Idle);
                return;
            }

            // Run home faster than normal walking
            Vector3 move = direction.normalized * 4f * Time.deltaTime;
            transform.position += move;
            transform.rotation = Quaternion.LookRotation(direction.normalized);

            if (_stateTimer > 20f)
                SetState(BehaviorState.Idle);
        }

        private void UpdateWorking(float dayFraction)
        {
            // Working is a role-specific idle animation state
            if (dayFraction >= Constants.DuskFraction)
            {
                SetState(BehaviorState.GoingHome);
                return;
            }

            // Return to idle after work period
            if (_stateTimer > 30f)
                SetState(BehaviorState.Idle);
        }

        #endregion

        /// <summary>
        /// Pick a random wander target within the configured radius of the home point.
        /// </summary>
        private void PickWanderTarget()
        {
            Vector3 home = _npc.GetHomePosition();
            float radius = Plugin.VillageLifePlugin.NPCWanderRadius.Value;

            Vector2 randomCircle = Random.insideUnitCircle * radius;
            _wanderTarget = home + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Snap to terrain height
            if (ZoneSystem.instance != null)
            {
                float height;
                if (ZoneSystem.instance.GetGroundHeight(_wanderTarget, out height))
                {
                    _wanderTarget.y = height;
                }
            }
        }

        /// <summary>
        /// Trigger flee behavior (e.g., from a raid event).
        /// </summary>
        public void TriggerFlee()
        {
            if (_currentState != BehaviorState.Sleeping) // Don't wake up sleeping NPCs for minor threats
                SetState(BehaviorState.Fleeing);
        }

        /// <summary>
        /// Check if this NPC is currently available for interaction.
        /// </summary>
        public bool IsAvailableForInteraction()
        {
            return _currentState != BehaviorState.Sleeping &&
                   _currentState != BehaviorState.Fleeing;
        }
    }
}
