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
                var openingOfferRoll = openingOfferService.CreateNpcOpeningOffer(session.BaseExpectation, settings);
                var openingOffer = openingOfferRoll.Offer;
                session.SetLastNpcOfferRoll(openingOfferRoll);
                session.AddDebugLog(CreateNpcOfferLog("NPC açılış teklifi", openingOfferRoll));
                session.SetDebugReason("NPC açılışı kendi maaş talebiyle yaptı.");
                session.SetNpcOpeningOffer(openingOffer);
                Transition(session, InterviewNegotiationState.WaitingForPlayerDecisionOnNpcOffer, InterviewNegotiationTurn.Player, InterviewDialogueIntent.NpcOpeningOffer, openingOffer, false);
                return;
            }

            session.SetDebugReason("NPC açılışı oyuncudan teklif isteyerek başladı.");
            session.AddDebugLog("NPC açılışı: NPC ilk teklifi oyuncudan istedi.");
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
            session.AddDebugLog($"Oyuncu NPC teklifini kabul etti: {FormatMoney(agreedSalary)}.");
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
            session.AddDebugLog($"Oyuncu görüşmeyi reddetti. Aktif NPC teklifi: {FormatMoney(session.CurrentSalaryOffer)}.");
            Transition(session, InterviewNegotiationState.RejectedByPlayer, InterviewNegotiationTurn.System, InterviewDialogueIntent.PlayerRejectedNpcOffer, session.CurrentSalaryOffer, true);
        }

        public bool TrySubmitPlayerOffer(InterviewSessionRuntimeData session, Money playerOffer, InterviewNegotiationSettings settings)
        {
            if (session == null || !CanPlayerSubmitOffer(session))
            {
                return false;
            }

            session.SetPlayerOffer(playerOffer);
            session.AddDebugLog($"Oyuncu teklifi: {FormatMoney(playerOffer)}. En yüksek oyuncu teklifi: {FormatMoney(session.HighestPlayerOffer)}.");
            var accepted = acceptanceService.TryEvaluateOffer(playerOffer, session.BaseExpectation, settings, out var ratio, out var probability, out var roll);
            session.SetLatestOfferEvaluation(ratio, probability, roll, string.Empty);
            if (accepted)
            {
                session.UpdateSalaryOffer(playerOffer);
                session.AddDebugLog($"Kabul testi: kabul ihtimali {FormatPercent(probability)}, red ihtimali {FormatPercent(1f - probability)}, zar {roll:F2}. Sonuç: kabul.");
                session.SetDebugReason($"Oyuncu teklifi kabul edildi. Oran={ratio:F2}, olasılık={probability:P0}, zar={roll:F2}.");
                Transition(session, InterviewNegotiationState.Accepted, InterviewNegotiationTurn.System, InterviewDialogueIntent.NpcAcceptsOffer, playerOffer, session.IsFinalDecisionStage);
                return true;
            }

            session.AddDebugLog($"Kabul testi: kabul ihtimali {FormatPercent(probability)}, red ihtimali {FormatPercent(1f - probability)}, zar {roll:F2}. Sonuç: red.");
            var hardRejected = ratio <= settings.LowOfferHardRejectionMultiplier;
            var shouldEnd = hardRejected;
            if (hardRejected)
            {
                session.AddDebugLog($"Hard red eşiği: teklif oranı {ratio:F2}, eşik {settings.LowOfferHardRejectionMultiplier:F2}. Sonuç: görüşme kapanır.");
            }
            else
            {
                shouldEnd = ShouldNpcEndAfterReject(settings, out var endProbability, out var counterProbability, out var endRoll);
                session.AddDebugLog($"Red sonrası karar: bitirme ihtimali {FormatPercent(endProbability)}, karşı teklif ihtimali {FormatPercent(counterProbability)}, zar {endRoll:F2}. Sonuç: {(shouldEnd ? "görüşme kapanır" : "NPC teklif verir")}.");
            }

            if (shouldEnd)
            {
                session.SetDebugReason($"Teklif reddedildi ve görüşme kapandı. Oran={ratio:F2}, olasılık={probability:P0}, zar={roll:F2}.");
                Transition(session, InterviewNegotiationState.RejectedByNpc, InterviewNegotiationTurn.System, InterviewDialogueIntent.NpcHardRejectsOffer, playerOffer, true);
                return false;
            }

            var npcCounterOfferRoll = ResolveNpcResponseOffer(session, settings, ratio);
            var npcCounterOffer = npcCounterOfferRoll.Offer;
            session.SetLastNpcOfferRoll(npcCounterOfferRoll);
            if (!npcCounterOfferRoll.CanOffer)
            {
                session.UpdateSalaryOffer(playerOffer);
                session.AddDebugLog($"Geçerli NPC teklif aralığı yok: gerekli minimum {FormatMoney(npcCounterOfferRoll.MinimumOffer)}, tavan {FormatMoney(npcCounterOfferRoll.MaximumOffer)}. NPC oyuncudan düşük teklif vermedi ve oyuncu teklifini kabul etti.");
                session.SetDebugReason($"NPC oyuncu teklifinden yüksek ve tavanı aşmayan teklif üretemedi. Oyuncu teklifi kabul edildi. Oran={ratio:F2}, olasılık={probability:P0}, zar={roll:F2}.");
                Transition(session, InterviewNegotiationState.Accepted, InterviewNegotiationTurn.System, InterviewDialogueIntent.NpcAcceptsOffer, playerOffer, session.IsFinalDecisionStage);
                return true;
            }

            session.AddDebugLog(CreateNpcOfferLog("NPC karşı teklifi", npcCounterOfferRoll));
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

        private InterviewNpcOfferRollResult ResolveNpcResponseOffer(InterviewSessionRuntimeData session, InterviewNegotiationSettings settings, float ratio)
        {
            if (!session.WasNpcOpeningOfferBranch && ratio < settings.CounterOfferMinMultiplier)
            {
                return openingOfferService.CreateNpcOpeningOffer(session.BaseExpectation, settings, session.HighestPlayerOffer.Amount + 1);
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

        private static bool ShouldNpcEndAfterReject(InterviewNegotiationSettings settings, out float endProbability, out float counterProbability, out float roll)
        {
            var endWeight = Mathf.Clamp01(settings.RejectThenEndProbability);
            var counterWeight = Mathf.Clamp01(settings.RejectThenCounterProbability);
            var total = endWeight + counterWeight;
            if (total <= 0f)
            {
                endProbability = 1f;
                counterProbability = 0f;
                roll = 0f;
                return true;
            }

            endProbability = endWeight / total;
            counterProbability = counterWeight / total;
            roll = Random.value;
            return roll < endProbability;
        }

        private static string CreateNpcOfferLog(string label, InterviewNpcOfferRollResult offerRoll)
        {
            if (!offerRoll.CanOffer)
            {
                return $"{label}: geçerli aralık yok. Gerekli minimum {FormatMoney(offerRoll.MinimumOffer)}, tavan {FormatMoney(offerRoll.MaximumOffer)}.";
            }

            return $"{label}: {FormatMoney(offerRoll.Offer)} teklif etti. {FormatMoney(offerRoll.MinimumOffer)} - {FormatMoney(offerRoll.MaximumOffer)} aralığından seçildi.";
        }

        private static string FormatMoney(Money money)
        {
            return money.Amount.ToString("N0");
        }

        private static string FormatPercent(float value)
        {
            return (Mathf.Clamp01(value) * 100f).ToString("F1") + "%";
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
