using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Npcs.Runtime.Actors;
using CompanySimulator.Features.Npcs.Runtime.Models;
using CompanySimulator.Features.Player.Runtime.Components;
using CompanySimulator.Presentation.UI.Runtime.Components;
using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    [DisallowMultipleComponent]
    public sealed class InterviewSessionManager : MonoBehaviour
    {
        [SerializeField] private EmployeeManager employeeManager;
        [SerializeField] private NpcActor interviewNpcPrefab;
        [SerializeField] private Transform interviewActorRoot;
        [SerializeField] private CeoDeskController preferredDesk;
        [SerializeField] private Canvas rootCanvas;

        private InterviewSessionRuntimeData currentSession;
        private NpcActor currentActor;
        private int sessionSequence;

        public event System.Action SessionChanged;

        public InterviewSessionRuntimeData CurrentSession => currentSession;
        public NpcActor CurrentActor => currentActor;
        public bool HasActiveSession => currentSession != null;

        private void Awake()
        {
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            rootCanvas ??= FindObjectOfType<Canvas>();
            EnsureActorRoot();
        }

        public bool TryStartInterview(EmployeeRuntimeData applicant)
        {
            if (!CanStartInterview(applicant, out var desk))
            {
                return false;
            }

            var runtimeId = $"interview_npc_{++sessionSequence}";
            var interviewNpc = new InterviewNpcRuntimeData(runtimeId, applicant);
            currentSession = new InterviewSessionRuntimeData($"interview_session_{sessionSequence}", applicant, desk, interviewNpc, 0);
            if (!SpawnInterviewActor(desk, interviewNpc))
            {
                currentSession = null;
                return false;
            }

            currentSession.MarkCandidateSeated();
            currentSession.MarkNegotiationReady();
            EnsureDialoguePanel();
            SessionChanged?.Invoke();
            return true;
        }

        public bool TryHireCurrentApplicant(Money agreedDailySalary)
        {
            if (currentSession == null || employeeManager == null)
            {
                return false;
            }

            if (!employeeManager.TryHireApplicant(currentSession.Applicant))
            {
                return false;
            }

            currentSession.MarkHired(agreedDailySalary);
            EndCurrentSession();
            return true;
        }

        public bool RejectCurrentApplicant()
        {
            if (currentSession == null)
            {
                return false;
            }

            employeeManager?.TryRejectApplicant(currentSession.Applicant);
            currentSession.MarkRejected();
            EndCurrentSession();
            return true;
        }

        public void CancelCurrentSession()
        {
            if (currentSession == null)
            {
                return;
            }

            currentSession.Cancel();
            EndCurrentSession();
        }

        private bool CanStartInterview(EmployeeRuntimeData applicant, out CeoDeskController desk)
        {
            desk = ResolveDesk();
            if (employeeManager == null || applicant == null || currentSession != null || desk == null || desk.InterviewSeat == null)
            {
                return false;
            }

            if (desk.InterviewSeat.IsOccupied)
            {
                return false;
            }

            return ContainsApplicant(applicant);
        }

        private bool SpawnInterviewActor(CeoDeskController desk, InterviewNpcRuntimeData interviewNpc)
        {
            EnsureActorRoot();
            var actor = CreateActorInstance();
            if (actor == null)
            {
                return false;
            }

            var seat = desk.InterviewSeat;
            if (!seat.TryOccupy(actor, SeatOccupantType.InterviewNpc))
            {
                Destroy(actor.gameObject);
                return false;
            }

            interviewNpc.SetPose(seat.GetSeatPosition(), seat.GetSeatRotation());
            interviewNpc.SetLifecycleState(NpcLifecycleState.Seated);
            actor.Bind(interviewNpc);
            actor.SetSeatedPresentation(true, false, true);
            currentActor = actor;

            desk.SetRootCanvas(rootCanvas != null ? rootCanvas : FindObjectOfType<Canvas>());
            FocusPlayerOnInterviewNpc(desk, interviewNpc);
            return true;
        }

        private void EndCurrentSession()
        {
            if (currentSession?.Desk != null && currentActor != null && currentSession.Desk.InterviewSeat != null)
            {
                currentSession.Desk.InterviewSeat.Vacate(currentActor);
            }

            if (currentActor != null)
            {
                Destroy(currentActor.gameObject);
                currentActor = null;
            }

            currentSession = null;
            SessionChanged?.Invoke();
        }

        private CeoDeskController ResolveDesk()
        {
            var playerDesk = ResolvePlayerDesk();
            if (playerDesk != null)
            {
                return playerDesk;
            }

            if (preferredDesk != null && preferredDesk.InterviewSeat != null)
            {
                return preferredDesk;
            }

            var desks = FindObjectsOfType<CeoDeskController>(true);
            for (var i = 0; i < desks.Length; i++)
            {
                if (desks[i] != null && desks[i].InterviewSeat != null)
                {
                    return desks[i];
                }
            }

            return null;
        }

        private CeoDeskController ResolvePlayerDesk()
        {
            var playerInteractor = FindObjectOfType<PlayerInteractor>();
            if (playerInteractor == null || !playerInteractor.IsSeated)
            {
                return null;
            }

            var desks = FindObjectsOfType<CeoDeskController>(true);
            for (var i = 0; i < desks.Length; i++)
            {
                var desk = desks[i];
                if (desk != null && desk.PlayerSeat == playerInteractor.CurrentSeat)
                {
                    return desk;
                }
            }

            return null;
        }

        private bool ContainsApplicant(EmployeeRuntimeData applicant)
        {
            var applicants = employeeManager.Applicants;
            for (var i = 0; i < applicants.Count; i++)
            {
                if (applicants[i] == applicant)
                {
                    return true;
                }
            }

            return false;
        }

        private NpcActor CreateActorInstance()
        {
            if (interviewNpcPrefab != null)
            {
                return Instantiate(interviewNpcPrefab, interviewActorRoot);
            }

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            primitive.name = "InterviewNpcActor";
            primitive.transform.SetParent(interviewActorRoot, false);
            primitive.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
            var primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                primitiveCollider.enabled = false;
            }

            return primitive.AddComponent<NpcActor>();
        }

        private void EnsureActorRoot()
        {
            if (interviewActorRoot != null)
            {
                return;
            }

            var existing = transform.Find("InterviewActorRoot");
            if (existing != null)
            {
                interviewActorRoot = existing;
                return;
            }

            interviewActorRoot = new GameObject("InterviewActorRoot").transform;
            interviewActorRoot.SetParent(transform, false);
        }

        private void EnsureDialoguePanel()
        {
            if (FindObjectOfType<InterviewDialoguePanelUI>() != null)
            {
                return;
            }

            new GameObject("InterviewDialoguePanelUI", typeof(InterviewDialoguePanelUI));
        }

        private void FocusPlayerOnInterviewNpc(CeoDeskController desk, InterviewNpcRuntimeData interviewNpc)
        {
            if (desk == null || interviewNpc == null)
            {
                return;
            }

            var playerInteractor = FindObjectOfType<PlayerInteractor>();
            if (playerInteractor == null || !playerInteractor.IsSeatedAt(desk.PlayerSeat))
            {
                return;
            }

            playerInteractor.MovementController?.FocusViewAt(interviewNpc.WorldPosition + Vector3.up * 1.5f);
        }
    }
}
