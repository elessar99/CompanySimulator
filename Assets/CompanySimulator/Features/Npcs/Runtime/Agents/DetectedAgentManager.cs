using System.Collections.Generic;
using CompanySimulator.Features.Agents.Runtime.Components;
using CompanySimulator.Features.Agents.Runtime.Models;
using CompanySimulator.Features.Npcs.Runtime.Actors;
using CompanySimulator.Features.Npcs.Runtime.Models;
using CompanySimulator.Features.Npcs.Runtime.Office;
using CompanySimulator.Features.Player.Runtime.Components;
using CompanySimulator.Features.Rivals.Runtime.Components;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Agents
{
    [DisallowMultipleComponent]
    public sealed class DetectedAgentManager : MonoBehaviour
    {
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private NpcActor detectedAgentPrefab;
        [SerializeField] private Transform detectedAgentRoot;
        [SerializeField] private Transform fallbackSpawnOrigin;
        [SerializeField, Min(0.5f)] private float spawnRadius = 3f;
        [SerializeField] private OfficePointOfInterestService pointOfInterestService;
        [SerializeField] private RivalCompanyManager rivalCompanyManager;
        [SerializeField] private AgentNpcBehaviourSettings behaviourSettings = default;

        private readonly Dictionary<string, AgentNpcRuntimeData> runtimeById = new Dictionary<string, AgentNpcRuntimeData>(8);
        private readonly Dictionary<string, NpcActor> actorById = new Dictionary<string, NpcActor>(8);
        private readonly Dictionary<string, PlayerTargetedAgentRuntimeData> sourceById = new Dictionary<string, PlayerTargetedAgentRuntimeData>(8);
        private int runtimeSequence;

        private void Awake()
        {
            agentManager ??= FindObjectOfType<AgentManager>();
            pointOfInterestService ??= FindObjectOfType<OfficePointOfInterestService>();
            rivalCompanyManager ??= FindObjectOfType<RivalCompanyManager>();
            EnsureActorRoot();
        }

        private void OnEnable()
        {
            agentManager ??= FindObjectOfType<AgentManager>();
            if (agentManager != null)
            {
                agentManager.DataChanged -= SyncDetectedAgents;
                agentManager.DataChanged += SyncDetectedAgents;
            }

            SyncDetectedAgents();
        }

        private void Update()
        {
            UpdateDetectedAgents();
        }

        private void OnDisable()
        {
            if (agentManager != null)
            {
                agentManager.DataChanged -= SyncDetectedAgents;
            }
        }

        public void SyncDetectedAgents()
        {
            if (agentManager == null)
            {
                return;
            }

            var activeDetectedIds = new HashSet<string>();
            var targetedAgents = agentManager.PlayerTargetedAgents;
            for (var i = 0; i < targetedAgents.Count; i++)
            {
                var sourceAgent = targetedAgents[i];
                if (sourceAgent == null || !sourceAgent.IsActive || !sourceAgent.IsDetected)
                {
                    continue;
                }

                var sourceId = GetSourceId(sourceAgent);
                activeDetectedIds.Add(sourceId);
                if (!runtimeById.ContainsKey(sourceId))
                {
                    SpawnDetectedAgent(sourceId, sourceAgent);
                }
            }

            var existingIds = new List<string>(runtimeById.Keys);
            for (var i = 0; i < existingIds.Count; i++)
            {
                if (!activeDetectedIds.Contains(existingIds[i]))
                {
                    RemoveDetectedAgent(existingIds[i]);
                }
            }
        }

        public bool TryDismissAgent(string runtimeId)
        {
            if (string.IsNullOrWhiteSpace(runtimeId) || agentManager == null)
            {
                return false;
            }

            if (!sourceById.TryGetValue(runtimeId, out var sourceAgent) || sourceAgent == null || !sourceAgent.IsDetected || !sourceAgent.IsActive)
            {
                return false;
            }

            if (!agentManager.DismissDetectedAgent(sourceAgent))
            {
                return false;
            }

            SyncDetectedAgents();
            return true;
        }

        public void TriggerTestAgent()
        {
            agentManager ??= FindObjectOfType<AgentManager>();
            rivalCompanyManager ??= FindObjectOfType<RivalCompanyManager>();
            if (agentManager == null || rivalCompanyManager == null)
            {
                return;
            }

            if (!rivalCompanyManager.IsInitialized)
            {
                rivalCompanyManager.Initialize();
            }

            var rivals = rivalCompanyManager.Rivals;
            if (rivals == null || rivals.Count == 0)
            {
                return;
            }

            agentManager.ForceRivalSendAgent(rivals[0]);
        }

        private void SpawnDetectedAgent(string runtimeId, PlayerTargetedAgentRuntimeData sourceAgent)
        {
            var actor = CreateActorInstance();
            if (actor == null)
            {
                return;
            }

            var settings = behaviourSettings.MoveSpeed > 0f ? behaviourSettings : AgentNpcBehaviourSettings.Default;
            var runtime = new AgentNpcRuntimeData($"detected_agent_{++runtimeSequence}", sourceAgent, settings);
            var spawnPosition = ResolveSpawnPosition(runtimeSequence);
            runtime.SetPose(spawnPosition, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            runtime.SetLifecycleState(NpcLifecycleState.Spawned);
            actor.Bind(runtime);
            actor.SetMovingPresentation(0f);
            actor.transform.position = spawnPosition;

            var interactable = actor.gameObject.GetComponent<DetectedAgentInteractable>();
            if (interactable == null)
            {
                interactable = actor.gameObject.AddComponent<DetectedAgentInteractable>();
            }

            var actorCollider = actor.GetComponent<Collider>();
            if (actorCollider == null)
            {
                actorCollider = actor.gameObject.AddComponent<CapsuleCollider>();
            }

            interactable.Configure(this, runtimeId);

             runtimeById[runtimeId] = runtime;
             actorById[runtimeId] = actor;
             sourceById[runtimeId] = sourceAgent;
        }

        private void UpdateDetectedAgents()
        {
            foreach (var pair in runtimeById)
            {
                var runtimeId = pair.Key;
                var runtime = pair.Value;
                if (runtime == null || !actorById.TryGetValue(runtimeId, out var actor) || actor == null)
                {
                    continue;
                }

                switch (runtime.State)
                {
                    case AgentNpcState.WalkingToPoint:
                        UpdateWalkingAgent(runtime, actor);
                        break;
                    case AgentNpcState.VisitingPoint:
                        UpdateVisitingAgent(runtime, actor);
                        break;
                    case AgentNpcState.Wandering:
                        UpdateIdleAgent(runtime);
                        break;
                }
            }
        }

        private void UpdateIdleAgent(AgentNpcRuntimeData runtime)
        {
            runtime.Tick(UnityEngine.Time.deltaTime);
            if (runtime.RemainingStateTime > 0f)
            {
                return;
            }

            if (TryAssignPointOfInterest(runtime, out var visitDuration))
            {
                runtime.SetState(AgentNpcState.WalkingToPoint, visitDuration);
                runtime.SetLifecycleState(NpcLifecycleState.Walking);
                return;
            }

            runtime.SetCurrentTarget(ResolveFallbackTarget(runtime));
            runtime.SetState(AgentNpcState.WalkingToPoint, runtime.BehaviourSettings.MaxVisitDuration);
            runtime.SetLifecycleState(NpcLifecycleState.Walking);
        }

        private void UpdateWalkingAgent(AgentNpcRuntimeData runtime, NpcActor actor)
        {
            var reached = actor.MoveTowards(runtime.CurrentTarget, runtime.BehaviourSettings.MoveSpeed);
            runtime.SetPose(actor.transform.position, actor.transform.rotation);
            if (!reached)
            {
                return;
            }

            var visitDuration = runtime.CurrentPointOfInterest != null
                ? runtime.CurrentPointOfInterest.GetRandomVisitDuration()
                : Random.Range(runtime.BehaviourSettings.MinVisitDuration, runtime.BehaviourSettings.MaxVisitDuration);
            runtime.SetState(AgentNpcState.VisitingPoint, visitDuration);
            runtime.SetLifecycleState(NpcLifecycleState.Spawned);
            actor.SetMovingPresentation(0f);
        }

        private void UpdateVisitingAgent(AgentNpcRuntimeData runtime, NpcActor actor)
        {
            runtime.Tick(UnityEngine.Time.deltaTime);
            actor.SetMovingPresentation(0f);
            if (runtime.RemainingStateTime > 0f)
            {
                return;
            }

            ReleasePointOfInterest(runtime);
            runtime.SetState(AgentNpcState.Wandering, Random.Range(runtime.BehaviourSettings.MinVisitDuration, runtime.BehaviourSettings.MaxVisitDuration));
            runtime.SetLifecycleState(NpcLifecycleState.Spawned);
        }

        private void RemoveDetectedAgent(string runtimeId)
        {
            if (runtimeById.TryGetValue(runtimeId, out var runtime))
            {
                ReleasePointOfInterest(runtime);
            }

            if (actorById.TryGetValue(runtimeId, out var actor) && actor != null)
            {
                Destroy(actor.gameObject);
            }

            actorById.Remove(runtimeId);
            runtimeById.Remove(runtimeId);
            sourceById.Remove(runtimeId);
        }

        private NpcActor CreateActorInstance()
        {
            if (detectedAgentPrefab != null)
            {
                return Instantiate(detectedAgentPrefab, detectedAgentRoot);
            }

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            primitive.name = "DetectedAgentNpcActor";
            primitive.transform.SetParent(detectedAgentRoot, false);
            primitive.transform.localScale = new Vector3(0.7f, 1.85f, 0.7f);
            return primitive.AddComponent<NpcActor>();
        }

        private Vector3 ResolveSpawnPosition(int index)
        {
            var origin = fallbackSpawnOrigin != null ? fallbackSpawnOrigin.position : transform.position;
            var angle = index * 37f;
            var offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * spawnRadius);
            return origin + offset;
        }

        private void EnsureActorRoot()
        {
            if (detectedAgentRoot != null)
            {
                return;
            }

            var existing = transform.Find("DetectedAgentRoot");
            if (existing != null)
            {
                detectedAgentRoot = existing;
                return;
            }

            detectedAgentRoot = new GameObject("DetectedAgentRoot").transform;
            detectedAgentRoot.SetParent(transform, false);
        }

        private static string GetSourceId(PlayerTargetedAgentRuntimeData sourceAgent)
        {
            if (sourceAgent == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(sourceAgent.RuntimeId))
            {
                return sourceAgent.RuntimeId;
            }

            return $"{sourceAgent.DeployDay}_{sourceAgent.Definition?.Id}_{sourceAgent.SourceRival?.Definition?.Id}_{sourceAgent.TargetSector?.Id}";
        }

        private bool TryAssignPointOfInterest(AgentNpcRuntimeData runtime, out float visitDuration)
        {
            visitDuration = runtime != null ? runtime.BehaviourSettings.MaxVisitDuration : 0f;
            if (runtime == null)
            {
                return false;
            }

            runtime.ClearCurrentPointOfInterest();
            if (pointOfInterestService == null)
            {
                return false;
            }

            if (!pointOfInterestService.TrySelectPoint(runtime.RuntimeId, out var pointOfInterest) || pointOfInterest == null)
            {
                return false;
            }

            runtime.SetCurrentPointOfInterest(pointOfInterest);
            runtime.SetCurrentTarget(pointOfInterest.VisitPosition);
            visitDuration = pointOfInterest.GetRandomVisitDuration();
            return true;
        }

        private Vector3 ResolveFallbackTarget(AgentNpcRuntimeData runtime)
        {
            var origin = fallbackSpawnOrigin != null ? fallbackSpawnOrigin.position : transform.position;
            var randomOffset = Random.insideUnitSphere * runtime.BehaviourSettings.WanderRadius;
            randomOffset.y = 0f;
            return origin + randomOffset;
        }

        private static void ReleasePointOfInterest(AgentNpcRuntimeData runtime)
        {
            if (runtime?.CurrentPointOfInterest == null)
            {
                return;
            }

            runtime.CurrentPointOfInterest.Release(runtime.RuntimeId);
            runtime.ClearCurrentPointOfInterest();
        }
    }
}
