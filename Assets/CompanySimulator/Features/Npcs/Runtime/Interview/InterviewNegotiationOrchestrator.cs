using CompanySimulator.Shared.Runtime.Economy;
using UnityEngine;

namespace CompanySimulator.Features.Npcs.Runtime.Interview
{
    public sealed class InterviewNegotiationOrchestrator
    {
        private readonly InterviewOpeningOfferService openingOfferService = new InterviewOpeningOfferService();
        private readonly InterviewOfferAcceptanceService acceptanceService = new InterviewOfferAcceptanceService();
        private readonly InterviewCounterOfferService counterOfferService = new InterviewCounterOfferService();
        private readonly InterviewDialogueGenerator dialogueGenerator = new InterviewDialogueGenerator();

        public void BeginNegotiation(InterviewSessionRuntimeData session, InterviewNegotiationSettings settings)
        {
            if (session == null)
            {
                return;
            }

            if (openingOfferService.ShouldNpcOpenWithOffer(settings))
            {
                var openingOffer = openingOfferService.CreateNpcOpeningOffer(session.BaseExpectation, settings);
                session.SetDebugReason("NPC açılışı kendi maaş talebiyle yaptı.");
                session.SetNpcOpeningOffer(openingOffer);
                Transition(session, InterviewNegotiationState.WaitingForPlayerDecisionOnNpcOffer, InterviewNegotiationTurn.Player, InterviewDialogueIntent.NpcOpeningOffer, openingOffer, false);
                return;
            }

            session.SetDebugReason("NPC açılışı oyuncudan teklif isteyerek başladı.");
            Transition(session, InterviewNegotiationState.WaitingForPlayerOpeningOffer, InterviewNegotiationTurn.Player, InterviewDialogueIntent.NpcRequestsPlayerOffer, Money.Zero, false);
        }

        public bool TryAcceptCurrentOffer(InterviewSessionRuntimeData session, out Money agreedSalary)
        {
            agreedSalary = Money.Zero;
            if (session == null)
            {
                return false;
            }

            agreedSalary = session.CurrentSalaryOffer;
            session.SetDebugReason(session.IsFinalDecisionStage
                ? "Oyuncu NPC son karşı teklifini kabul etti."
                : "Oyuncu aktif NPC teklifini kabul etti.");
            Transition(session, InterviewNegotiationState.Accepted, InterviewNegotiationTurn.System, InterviewDialogueIntent.PlayerAcceptedNpcOffer, agreedSalary, session.IsFinalDecisionStage);
            return true;
        }

        public void RejectByPlayer(InterviewSessionRuntimeData session)
        {
            if (session == null)
            {
                return;
            }

            session.SetDebugReason("Oyuncu görüşmeyi kendi isteğiyle reddetti.");
            Transition(session, InterviewNegotiationState.RejectedByPlayer, InterviewNegotiationTurn.System, InterviewDialogueIntent.PlayerRejectedNpcOffer, session.CurrentSalaryOffer, true);
        }

        public bool TrySubmitPlayerOffer(InterviewSessionRuntimeData session, Money playerOffer, InterviewNegotiationSettings settings)
        {
            if (session == null || !CanPlayerSubmitOffer(session))
            {
                return false;
            }

            session.SetPlayerOffer(playerOffer);
            var accepted = acceptanceService.TryEvaluateOffer(playerOffer, session.BaseExpectation, settings, out var ratio, out var probability, out var roll);
            session.SetLatestOfferEvaluation(ratio, probability, roll, string.Empty);
            if (accepted)
            {
                session.UpdateSalaryOffer(playerOffer);
                session.SetDebugReason($"Oyuncu teklifi kabul edildi. Oran={ratio:F2}, olasılık={probability:P0}, zar={roll:F2}.");
                Transition(session, InterviewNegotiationState.Accepted, InterviewNegotiationTurn.System, InterviewDialogueIntent.NpcAcceptsOffer, playerOffer, session.IsFinalDecisionStage);
                return true;
            }

            var shouldEnd = ratio <= settings.LowOfferHardRejectionMultiplier || ShouldNpcEndAfterReject(settings);
            if (shouldEnd)
            {
                session.SetDebugReason($"Teklif reddedildi ve görüşme kapandı. Oran={ratio:F2}, olasılık={probability:P0}, zar={roll:F2}.");
                Transition(session, InterviewNegotiationState.RejectedByNpc, InterviewNegotiationTurn.System, InterviewDialogueIntent.NpcHardRejectsOffer, playerOffer, true);
                return false;
            }

            var npcCounterOffer = ResolveNpcResponseOffer(session, playerOffer, settings, ratio);
            var entersFullNpcOfferBranch = !session.WasNpcOpeningOfferBranch && ratio < settings.CounterOfferMinMultiplier;
            if (entersFullNpcOfferBranch)
            {
                session.SetNpcOpeningOffer(npcCounterOffer);
            }
            else
            {
                session.SetNpcLastOffer(npcCounterOffer);
            }

            var isFinalStage = !entersFullNpcOfferBranch && (session.WasNpcOpeningOfferBranch || ratio >= settings.CounterOfferMinMultiplier);
            var nextState = isFinalStage
                ? InterviewNegotiationState.WaitingForPlayerDecisionOnCounterOffer
                : InterviewNegotiationState.WaitingForPlayerDecisionOnNpcOffer;
            session.SetDebugReason(isFinalStage
                ? $"Teklif reddedildi, NPC son karşı teklife geçti. Oran={ratio:F2}, olasılık={probability:P0}, zar={roll:F2}."
                : $"Teklif reddedildi, NPC yeni teklif dalına geçti. Oran={ratio:F2}, olasılık={probability:P0}, zar={roll:F2}.");
            session.SetFinalDecisionStage(isFinalStage);
            Transition(session, nextState, InterviewNegotiationTurn.Player, InterviewDialogueIntent.NpcCounterOffers, npcCounterOffer, isFinalStage);
            return false;
        }

        private Money ResolveNpcResponseOffer(InterviewSessionRuntimeData session, Money playerOffer, InterviewNegotiationSettings settings, float ratio)
        {
            if (!session.WasNpcOpeningOfferBranch && ratio < settings.CounterOfferMinMultiplier)
            {
                return openingOfferService.CreateNpcOpeningOffer(session.BaseExpectation, settings);
            }

            var npcReference = session.NpcLastOffer.Amount > 0 ? session.NpcLastOffer : session.BaseExpectation;
            return counterOfferService.CreateCounterOffer(npcReference, session.HighestPlayerOffer, session.BaseExpectation, settings);
        }

        private static bool CanPlayerSubmitOffer(InterviewSessionRuntimeData session)
        {
            if (session == null || session.IsFinalDecisionStage)
            {
                return false;
            }

            return session.NegotiationState == InterviewNegotiationState.WaitingForPlayerOpeningOffer
                || session.NegotiationState == InterviewNegotiationState.WaitingForPlayerDecisionOnNpcOffer;
        }

        private static bool ShouldNpcEndAfterReject(InterviewNegotiationSettings settings)
        {
            var endWeight = Mathf.Clamp01(settings.RejectThenEndProbability);
            var counterWeight = Mathf.Clamp01(settings.RejectThenCounterProbability);
            var total = endWeight + counterWeight;
            if (total <= 0f)
            {
                return true;
            }

            var roll = Random.value * total;
            return roll < endWeight;
        }

        private void Transition(
            InterviewSessionRuntimeData session,
            InterviewNegotiationState nextState,
            InterviewNegotiationTurn nextTurn,
            InterviewDialogueIntent intent,
            Money amount,
            bool isFinalDecisionStage)
        {
            var previousState = session.NegotiationState;
            session.SetNegotiationState(nextState);
            session.SetCurrentTurn(nextTurn);
            session.SetFinalDecisionStage(isFinalDecisionStage);
            var payload = dialogueGenerator.CreatePayload(intent, amount);
            session.SetLatestDialogue(payload);
            session.AddHistoryEntry(new InterviewNegotiationHistoryEntry(nextTurn, intent, amount, previousState, nextState));
            session.UpdateSalaryOffer(amount.Amount > 0 ? amount : session.CurrentSalaryOffer);
        }
    }
}
