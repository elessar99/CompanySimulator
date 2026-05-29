using CompanySimulator.Features.Employees.Runtime.Components;
using CompanySimulator.Features.Employees.Runtime.Models;
using CompanySimulator.Features.Furniture.Runtime.Components;
using CompanySimulator.Features.Npcs.Runtime.Actors;
using CompanySimulator.Features.Npcs.Runtime.Models;
using CompanySimulator.Features.Npcs.Runtime.Office;
using CompanySimulator.Features.Player.Runtime.Components;
using CompanySimulator.Presentation.UI.Runtime.Components;
using CompanySimulator.Shared.Runtime.Economy;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

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
        [SerializeField] private InterviewNegotiationSettings negotiationSettings = default;
        [SerializeField, Min(0f)] private float navMeshSpawnSampleRadius = 2f;

        private InterviewSessionRuntimeData currentSession;
        private string lastInterviewDebugSnapshot;
        private NpcActor currentActor;
        private int sessionSequence;
        private readonly InterviewNegotiationOrchestrator negotiationOrchestrator = new InterviewNegotiationOrchestrator();
        private readonly StringBuilder debugBuilder = new StringBuilder(1024);

        public event System.Action SessionChanged;
        public event Action<InterviewSessionRuntimeData> NegotiationUpdated;

        public InterviewSessionRuntimeData CurrentSession => currentSession;
        public NpcActor CurrentActor => currentActor;
        public bool HasActiveSession => currentSession != null;
        public string LastInterviewDebugSnapshot => lastInterviewDebugSnapshot;
        public string LastStartFailureReason { get; private set; }

        private void Awake()
        {
            employeeManager ??= FindObjectOfType<EmployeeManager>();
            rootCanvas ??= FindObjectOfType<Canvas>();
            EnsureActorRoot();
            EnsureOfficeWorkerManager();
        }

        public bool TryStartInterview(EmployeeRuntimeData applicant)
        {
            LastStartFailureReason = string.Empty;
            if (!CanStartInterview(applicant, out var desk, out var failureReason))
            {
                LogStartFailure(failureReason);
                return false;
            }

            var runtimeId = $"interview_npc_{++sessionSequence}";
            var interviewNpc = new InterviewNpcRuntimeData(runtimeId, applicant);
            currentSession = new InterviewSessionRuntimeData($"interview_session_{sessionSequence}", applicant, desk, interviewNpc, 0);
            lastInterviewDebugSnapshot = string.Empty;
            if (!SpawnInterviewActor(desk, interviewNpc))
            {
                currentSession = null;
                LogStartFailure("Interview NPC could not be created or seated.");
                return false;
            }

            currentSession.MarkCandidateSeated();
            currentSession.MarkNegotiationReady();
            BeginNegotiation();
            EnsureDialoguePanel();
            SessionChanged?.Invoke();
            NegotiationUpdated?.Invoke(currentSession);
            return true;
        }

        public bool TryStartQualityUpgradeInterview(EmployeeRuntimeData employee)
        {
            LastStartFailureReason = string.Empty;
            if (!CanStartQualityUpgradeInterview(employee, out var desk, out var failureReason))
            {
                LogStartFailure(failureReason);
                return false;
            }

            var requestedSalary = employee.GetQualityUpgradeRequestedSalary();
            var runtimeId = $"interview_npc_{++sessionSequence}";
            var interviewNpc = new InterviewNpcRuntimeData(runtimeId, employee, requestedSalary);
            currentSession = new InterviewSessionRuntimeData(
                $"interview_session_{sessionSequence}",
                employee,
                desk,
                interviewNpc,
                0,
                InterviewSessionPurpose.QualityUpgrade,
                requestedSalary);
            lastInterviewDebugSnapshot = string.Empty;
            if (!SpawnInterviewActor(desk, interviewNpc))
            {
                currentSession = null;
                LogStartFailure("Quality upgrade interview NPC could not be created or seated.");
                return false;
            }

            if (employeeManager == null || !employeeManager.TryBeginQualityUpgradeNegotiation(employee))
            {
                EndCurrentSession();
                return false;
            }

            currentSession.MarkCandidateSeated();
            currentSession.MarkNegotiationReady();
            BeginNegotiation();
            EnsureDialoguePanel();
            SessionChanged?.Invoke();
            NegotiationUpdated?.Invoke(currentSession);
            return true;
        }

        public bool TryHireCurrentApplicant(Money agreedDailySalary)
        {
            if (currentSession == null || employeeManager == null)
            {
                return false;
            }

            if (currentSession.Purpose == InterviewSessionPurpose.QualityUpgrade)
            {
                return TryCompleteCurrentQualityUpgrade(agreedDailySalary);
            }

            if (!employeeManager.TryHireApplicant(currentSession.Applicant, agreedDailySalary))
            {
                return false;
            }

            currentSession.MarkHired(agreedDailySalary);
            EndCurrentSession();
            return true;
        }

        private bool TryCompleteCurrentQualityUpgrade(Money agreedDailySalary)
        {
            if (currentSession == null || employeeManager == null)
            {
                return false;
            }

            if (!employeeManager.TryAcceptQualityUpgradeSalary(currentSession.Applicant, agreedDailySalary))
            {
                return false;
            }

            currentSession.MarkHired(agreedDailySalary);
            EndCurrentSession();
            return true;
        }

        public bool TryAcceptNegotiation()
        {
            if (currentSession == null)
            {
                return false;
            }

            if (!negotiationOrchestrator.TryAcceptCurrentOffer(currentSession, out var agreedSalary))
            {
                return false;
            }

            NegotiationUpdated?.Invoke(currentSession);
            return TryHireCurrentApplicant(agreedSalary);
        }

        public bool RejectCurrentApplicant()
        {
            if (currentSession == null)
            {
                return false;
            }

            if (currentSession.Purpose == InterviewSessionPurpose.QualityUpgrade
                && (employeeManager == null || !employeeManager.TryRejectQualityUpgradeNegotiation(currentSession.Applicant)))
            {
                return false;
            }

            negotiationOrchestrator.RejectByPlayer(currentSession);
            NegotiationUpdated?.Invoke(currentSession);
            if (currentSession.Purpose == InterviewSessionPurpose.Hiring)
            {
                employeeManager?.TryRejectApplicant(currentSession.Applicant);
            }
            currentSession.MarkRejectedByPlayer();
            EndCurrentSession();
            return true;
        }

        public bool TrySubmitPlayerOffer(Money playerOffer)
        {
            if (currentSession == null)
            {
                return false;
            }

            var settings = negotiationSettings.NpcOpeningOfferMinMultiplier > 0f
                ? negotiationSettings
                : InterviewNegotiationSettings.Default;
            var accepted = negotiationOrchestrator.TrySubmitPlayerOffer(currentSession, playerOffer, settings);
            if (accepted)
            {
                NegotiationUpdated?.Invoke(currentSession);
                return TryHireCurrentApplicant(playerOffer);
            }

            if (currentSession.NegotiationState == InterviewNegotiationState.RejectedByNpc)
            {
                NegotiationUpdated?.Invoke(currentSession);
                if (currentSession.Purpose == InterviewSessionPurpose.QualityUpgrade
                    && (employeeManager == null || !employeeManager.TryRejectQualityUpgradeNegotiation(currentSession.Applicant)))
                {
                    currentSession.AddDebugLog("Tazminat ödenemediği için çalışan ayrılışı tamamlanamadı.");
                    return false;
                }

                currentSession.MarkRejected();
                EndCurrentSession();
                return false;
            }

            SessionChanged?.Invoke();
            NegotiationUpdated?.Invoke(currentSession);
            return true;
        }

        public void CancelCurrentSession()
        {
            if (currentSession == null)
            {
                return;
            }

            if (currentSession.Purpose == InterviewSessionPurpose.QualityUpgrade
                && (employeeManager == null || !employeeManager.TryRejectQualityUpgradeNegotiation(currentSession.Applicant)))
            {
                currentSession.AddDebugLog("Görüşme iptal edildi ancak tazminat ödenemediği için çalışan ayrılışı tamamlanamadı.");
                return;
            }

            currentSession.Cancel();
            EndCurrentSession();
        }

        private bool CanStartInterview(EmployeeRuntimeData applicant, out CeoDeskController desk, out string failureReason)
        {
            failureReason = string.Empty;
            desk = ResolveDesk();
            if (employeeManager == null)
            {
                failureReason = "EmployeeManager not found.";
                return false;
            }

            if (applicant == null)
            {
                failureReason = "Applicant data is empty.";
                return false;
            }

            if (currentSession != null)
            {
                failureReason = "An interview session is already active.";
                return false;
            }

            if (desk == null)
            {
                failureReason = "No CEO desk was found for the interview.";
                return false;
            }

            if (desk.InterviewSeat == null)
            {
                failureReason = "The CEO desk has no InterviewNpc seat.";
                return false;
            }

            if (desk.InterviewSeat.IsOccupied)
            {
                failureReason = "The InterviewNpc seat is already occupied.";
                return false;
            }

            if (!ContainsApplicant(applicant))
            {
                failureReason = "The applicant is no longer in the applicant list.";
                return false;
            }

            return true;
        }

        private bool CanStartQualityUpgradeInterview(EmployeeRuntimeData employee, out CeoDeskController desk, out string failureReason)
        {
            failureReason = string.Empty;
            desk = ResolveDesk();
            if (employeeManager == null)
            {
                failureReason = "EmployeeManager not found.";
                return false;
            }

            if (employee == null)
            {
                failureReason = "Employee data is empty.";
                return false;
            }

            if (currentSession != null)
            {
                failureReason = "An interview session is already active.";
                return false;
            }

            if (desk == null)
            {
                failureReason = "No CEO desk was found for the salary interview.";
                return false;
            }

            if (desk.InterviewSeat == null)
            {
                failureReason = "The CEO desk has no InterviewNpc seat.";
                return false;
            }

            if (desk.InterviewSeat.IsOccupied)
            {
                failureReason = "The InterviewNpc seat is already occupied.";
                return false;
            }

            if (!employee.HasPendingQualityUpgrade)
            {
                failureReason = "The employee has no pending quality upgrade interview request.";
                return false;
            }

            return true;
        }

        private bool SpawnInterviewActor(CeoDeskController desk, InterviewNpcRuntimeData interviewNpc)
        {
            EnsureActorRoot();
            var seat = desk.InterviewSeat;
            var seatPosition = seat.GetSeatPosition(SeatOccupantType.InterviewNpc);
            var seatRotation = seat.GetSeatRotation(SeatOccupantType.InterviewNpc);
            var actor = CreateActorInstance(ResolveSpawnPosition(seatPosition), seatRotation);
            if (actor == null)
            {
                return false;
            }

            if (!seat.TryOccupy(actor, SeatOccupantType.InterviewNpc))
            {
                Destroy(actor.gameObject);
                LogStartFailure("InterviewNpc seat cannot be occupied by this NPC. " + FormatSeatInfo(seat));
                return false;
            }

            interviewNpc.SetPose(seatPosition, seatRotation);
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
            if (currentSession != null)
            {
                lastInterviewDebugSnapshot = BuildDebugSnapshot(currentSession);
            }

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
            NegotiationUpdated?.Invoke(null);
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

        private NpcActor CreateActorInstance(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            EnsureActorRoot();

            if (interviewNpcPrefab != null)
            {
                var actor = Instantiate(interviewNpcPrefab, spawnPosition, spawnRotation);
                if (interviewActorRoot != null)
                {
                    actor.transform.SetParent(interviewActorRoot, true);
                }

                return actor;
            }

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            primitive.name = "InterviewNpcActor";
            primitive.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            primitive.transform.SetParent(interviewActorRoot, true);
            primitive.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
            var primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                primitiveCollider.enabled = false;
            }

            return primitive.AddComponent<NpcActor>();
        }

        private Vector3 ResolveSpawnPosition(Vector3 desiredPosition)
        {
            if (navMeshSpawnSampleRadius > 0f &&
                NavMesh.SamplePosition(desiredPosition, out var hit, navMeshSpawnSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return desiredPosition;
        }

        private void EnsureActorRoot()
        {
            if (IsSceneTransform(interviewActorRoot))
            {
                return;
            }

            var existing = transform.Find("InterviewActorRoot");
            if (IsSceneTransform(existing))
            {
                interviewActorRoot = existing;
                return;
            }

            interviewActorRoot = new GameObject("InterviewActorRoot").transform;
            interviewActorRoot.SetParent(transform, false);
        }

        private static bool IsSceneTransform(Transform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            var scene = candidate.gameObject.scene;
            return scene.IsValid() && scene.isLoaded;
        }

        private void EnsureDialoguePanel()
        {
            var existingPanel = FindObjectOfType<InterviewDialoguePanelUI>(true);
            if (existingPanel != null)
            {
                if (!existingPanel.gameObject.activeSelf)
                {
                    existingPanel.gameObject.SetActive(true);
                }

                existingPanel.BindSessionManager(this);
                return;
            }

            var panel = new GameObject("InterviewDialoguePanelUI", typeof(InterviewDialoguePanelUI)).GetComponent<InterviewDialoguePanelUI>();
            panel.BindSessionManager(this);
        }

        private void EnsureOfficeWorkerManager()
        {
            if (FindObjectOfType<OfficeWorkerManager>() != null)
            {
                return;
            }

            new GameObject("OfficeWorkerManager", typeof(OfficeWorkerManager));
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

        private void BeginNegotiation()
        {
            if (currentSession == null)
            {
                return;
            }

            var settings = negotiationSettings.NpcOpeningOfferMinMultiplier > 0f
                ? negotiationSettings
                : InterviewNegotiationSettings.Default;
            negotiationOrchestrator.BeginNegotiation(currentSession, settings);
        }

        private void LogStartFailure(string reason)
        {
            LastStartFailureReason = string.IsNullOrWhiteSpace(reason) ? "Unknown interview start failure." : reason;
            lastInterviewDebugSnapshot = LastStartFailureReason;
            UnityEngine.Debug.LogWarning($"Interview start failed: {LastStartFailureReason}", this);
        }

        private static string FormatSeatInfo(SeatController seat)
        {
            if (seat == null)
            {
                return "Seat is null.";
            }

            var allowedType = seat.SeatPoint != null ? seat.SeatPoint.AllowedOccupantType.ToString() : "No SeatPoint";
            return $"Seat: {seat.name}, Allowed Occupant Type: {allowedType}.";
        }

        public string GetInterviewDebugSnapshot()
        {
            return currentSession != null ? BuildDebugSnapshot(currentSession) : lastInterviewDebugSnapshot;
        }

        private string BuildDebugSnapshot(InterviewSessionRuntimeData session)
        {
            if (session == null)
            {
                return "Henüz aktif veya sonlanmış bir interview verisi yok.";
            }

            var settings = negotiationSettings.NpcOpeningOfferMinMultiplier > 0f
                ? negotiationSettings
                : InterviewNegotiationSettings.Default;

            debugBuilder.Clear();
            debugBuilder.Append("<b>Interview Debug</b>");
            debugBuilder.Append("\nGörüşme Tipi: ");
            debugBuilder.Append(session.Purpose == InterviewSessionPurpose.QualityUpgrade ? "Maaş Düzenleme" : "İşe Alım");
            debugBuilder.Append("\n");
            debugBuilder.Append(session.Purpose == InterviewSessionPurpose.QualityUpgrade ? "Çalışan: " : "Aday: ");
            debugBuilder.Append(session.Applicant != null ? session.Applicant.DisplayName : "-");
            debugBuilder.Append("\nRol: ");
            debugBuilder.Append(session.Applicant != null && session.Applicant.Role != null ? session.Applicant.Role.DisplayName : "-");
            debugBuilder.Append("\nDurum: ");
            debugBuilder.Append(session.State);
            debugBuilder.Append(" | Neg.: ");
            debugBuilder.Append(session.NegotiationState);
            debugBuilder.Append(" | Sonuç: ");
            debugBuilder.Append(session.NegotiationOutcome);
            debugBuilder.Append("\nBeklenti: ");
            debugBuilder.Append(session.BaseExpectation.Amount.ToString("N0"));
            debugBuilder.Append(" | Çalışana Yazılan: ");
            debugBuilder.Append(session.Applicant != null ? session.Applicant.EffectiveDailySalary.Amount.ToString("N0") : "0");
            debugBuilder.Append(" | Açılış: ");
            debugBuilder.Append(session.NpcOpeningOffer.Amount.ToString("N0"));
            debugBuilder.Append(" | Son NPC: ");
            debugBuilder.Append(session.NpcLastOffer.Amount.ToString("N0"));
            debugBuilder.Append("\nSon Oyuncu Teklifi: ");
            debugBuilder.Append(session.LastPlayerOffer.Amount.ToString("N0"));
            debugBuilder.Append(" | En Yüksek Oyuncu: ");
            debugBuilder.Append(session.HighestPlayerOffer.Amount.ToString("N0"));
            debugBuilder.Append(" | Anlaşılan: ");
            debugBuilder.Append(session.CurrentSalaryOffer.Amount.ToString("N0"));
            if (session.LastNpcOfferRoll.HasRange)
            {
                debugBuilder.Append("\nNPC Teklif Aralığı: ");
                debugBuilder.Append(FormatNpcOfferRollType(session.LastNpcOfferRoll.RollType));
                debugBuilder.Append(" | ");
                debugBuilder.Append(session.LastNpcOfferRoll.MinimumOffer.Amount.ToString("N0"));
                debugBuilder.Append(" - ");
                debugBuilder.Append(session.LastNpcOfferRoll.MaximumOffer.Amount.ToString("N0"));
                if (session.LastNpcOfferRoll.CanOffer)
                {
                    debugBuilder.Append(" | Seçilen: ");
                    debugBuilder.Append(session.LastNpcOfferRoll.Offer.Amount.ToString("N0"));
                }
                else
                {
                    debugBuilder.Append(" | Geçerli teklif yok");
                }
            }
            debugBuilder.Append("\nTeklif Oranı: ");
            debugBuilder.Append(session.LastPlayerOfferRatio.ToString("F2"));
            debugBuilder.Append(" | Açılış/ Beklenti: ");
            debugBuilder.Append(session.BaseExpectation.Amount > 0 ? ((float)session.NpcOpeningOffer.Amount / session.BaseExpectation.Amount).ToString("F2") : "0.00");
            debugBuilder.Append(" | Kabul Olasılığı: ");
            debugBuilder.Append((session.LastAcceptanceProbability * 100f).ToString("F1"));
            debugBuilder.Append("% | Zar: ");
            debugBuilder.Append(session.LastAcceptanceRoll.ToString("F2"));
            debugBuilder.Append("\nSon Diyalog: ");
            debugBuilder.Append(session.LatestDialogue.Intent);
            debugBuilder.Append(" | LineKey: ");
            debugBuilder.Append(string.IsNullOrWhiteSpace(session.LatestDialogue.LineKey) ? "-" : session.LatestDialogue.LineKey);
            debugBuilder.Append(" | Turn: ");
            debugBuilder.Append(session.CurrentTurn);
            debugBuilder.Append("\nDal: ");
            debugBuilder.Append(session.WasNpcOpeningOfferBranch ? "NPC açılış teklifi" : "Oyuncu açılış teklifi");
            debugBuilder.Append(" | Final Aşama: ");
            debugBuilder.Append(session.IsFinalDecisionStage ? "Evet" : "Hayır");
            debugBuilder.Append("\nEşikler: hard<=");
            debugBuilder.Append(settings.LowOfferHardRejectionMultiplier.ToString("F2"));
            debugBuilder.Append(" | garanti>=");
            debugBuilder.Append(settings.GuaranteedAcceptanceMultiplier.ToString("F2"));
            debugBuilder.Append(" | counter>=");
            debugBuilder.Append(settings.CounterOfferMinMultiplier.ToString("F2"));
            debugBuilder.Append(" | npc open chance=");
            debugBuilder.Append(settings.NpcOpensWithOfferProbability.ToString("F2"));
            debugBuilder.Append("\nCounter Tavanı: ");
            debugBuilder.Append(settings.CounterOfferMaxMultiplier.ToString("F2"));
            debugBuilder.Append(" | End Prob: ");
            debugBuilder.Append(settings.RejectThenEndProbability.ToString("F2"));
            debugBuilder.Append(" | Counter Prob: ");
            debugBuilder.Append(settings.RejectThenCounterProbability.ToString("F2"));
            debugBuilder.Append("\nNeden: ");
            debugBuilder.Append(string.IsNullOrWhiteSpace(session.DebugReason) ? "-" : session.DebugReason);

            if (session.DebugLogEntries != null && session.DebugLogEntries.Count > 0)
            {
                debugBuilder.Append("\n\n<b>Karar Logları</b>");
                for (var i = 0; i < session.DebugLogEntries.Count; i++)
                {
                    debugBuilder.Append("\n");
                    debugBuilder.Append(i + 1);
                    debugBuilder.Append(") ");
                    debugBuilder.Append(session.DebugLogEntries[i]);
                }
            }

            if (session.NegotiationHistory != null && session.NegotiationHistory.Count > 0)
            {
                debugBuilder.Append("\n\n<b>Geçmiş</b>");
                for (var i = 0; i < session.NegotiationHistory.Count; i++)
                {
                    var entry = session.NegotiationHistory[i];
                    debugBuilder.Append("\n");
                    debugBuilder.Append(i + 1);
                    debugBuilder.Append(") ");
                    debugBuilder.Append(entry.Intent);
                    debugBuilder.Append(" | ");
                    debugBuilder.Append(entry.Amount.Amount.ToString("N0"));
                    debugBuilder.Append(" | ");
                    debugBuilder.Append(entry.PreviousState);
                    debugBuilder.Append(" → ");
                    debugBuilder.Append(entry.NextState);
                }
            }

            return debugBuilder.ToString();
        }

        private static string FormatNpcOfferRollType(InterviewNpcOfferRollType rollType)
        {
            switch (rollType)
            {
                case InterviewNpcOfferRollType.OpeningOffer:
                    return "Açılış";
                case InterviewNpcOfferRollType.CounterOffer:
                    return "Counter";
                default:
                    return "-";
            }
        }
    }
}
