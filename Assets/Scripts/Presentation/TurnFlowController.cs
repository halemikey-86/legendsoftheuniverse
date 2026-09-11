using System;
using LegendsOfTheUniverse.Presentation.EngineBridge;
using LegendsOfTheUniverse.Rules;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Solo turn loop: delegates to Engine B when <see cref="TableMatchBridge"/> is active,
    /// otherwise runs the legacy local phase machine.
    /// </summary>
    [DisallowMultipleComponent]
    public class TurnFlowController : MonoBehaviour
    {
        [SerializeField] HandView handView;
        [SerializeField] PlaymatZonesView playmatZones;
        [SerializeField] StoreView storeView;
        [SerializeField] TableMatchBridge matchBridge;

        int round = 1;
        int willPool;
        TurnStep step = TurnStep.Start;
        bool discardPending;
        Action<string, bool> setPrompt;

        bool UseEngine => matchBridge != null && matchBridge.IsActive;

        public int Round => UseEngine ? matchBridge.CurrentRound : round;
        public int WillPool => UseEngine ? matchBridge.CurrentWillPool : willPool;
        public TurnStep CurrentStep => UseEngine ? matchBridge.CurrentTurnStep : step;
        public bool IsDiscardPending => discardPending;

        public void Configure(
            HandView hand,
            PlaymatZonesView zones,
            StoreView store,
            Action<string, bool> promptCallback)
        {
            handView = hand;
            playmatZones = zones;
            storeView = store;
            setPrompt = promptCallback;
        }

        public void BindEngine(TableMatchBridge bridge)
        {
            if (matchBridge == bridge)
                return;

            if (matchBridge != null)
                matchBridge.PhaseStateChanged -= OnEnginePhaseChanged;

            matchBridge = bridge;

            if (matchBridge != null)
                matchBridge.PhaseStateChanged += OnEnginePhaseChanged;
        }

        void OnDestroy()
        {
            if (matchBridge != null)
                matchBridge.PhaseStateChanged -= OnEnginePhaseChanged;
        }

        public void BeginMatchAfterSetup()
        {
            discardPending = false;

            if (matchBridge != null)
            {
                matchBridge.BeginSoloMatch();
                UpdatePhaseState();
                return;
            }

            round = 1;
            BeginTurn();
        }

        void BeginTurn()
        {
            willPool = Mathf.Min(round, GameConstants.MaxRoundWill);
            step = TurnStep.WillSite;
            SyncHud();
            UpdatePhaseState();
        }

        void SyncHud()
        {
            playmatZones?.SetRound(round);
            playmatZones?.SetWillPool(willPool);
        }

        public void OnNextPhaseClicked()
        {
            if (discardPending)
                return;

            if (UseEngine)
            {
                if (!matchBridge.TryPassPriority(out var error))
                {
                    if (!string.IsNullOrEmpty(error))
                        setPrompt?.Invoke(error, true);
                    return;
                }

                UpdatePhaseState();
                return;
            }

            switch (step)
            {
                case TurnStep.WillSite:
                    step = TurnStep.Main;
                    break;
                case TurnStep.Main:
                    step = TurnStep.Clash;
                    break;
                default:
                    return;
            }

            UpdatePhaseState();
        }

        public void OnEndTurnClicked()
        {
            if (discardPending)
                return;

            if (UseEngine)
            {
                if (matchBridge.CurrentTurnStep != TurnStep.Clash)
                    return;

                var handCount = handView != null ? handView.HandCount : 0;
                if (handCount > GameConstants.MaxHandSize)
                {
                    discardPending = true;
                    handView.BeginDiscardUntilHandSize(GameConstants.MaxHandSize, OnDiscardComplete);
                    UpdatePhaseState();
                    return;
                }

                if (!matchBridge.TryPassPriority(out var error))
                {
                    if (!string.IsNullOrEmpty(error))
                        setPrompt?.Invoke(error, true);
                    return;
                }

                UpdatePhaseState();
                return;
            }

            if (step != TurnStep.Clash)
                return;

            var legacyHandCount = handView != null ? handView.HandCount : 0;
            if (legacyHandCount > GameConstants.MaxHandSize)
            {
                discardPending = true;
                handView.BeginDiscardUntilHandSize(GameConstants.MaxHandSize, OnDiscardComplete);
                UpdatePhaseState();
                return;
            }

            FinishTurn();
        }

        void OnDiscardComplete()
        {
            discardPending = false;

            if (UseEngine)
            {
                if (!matchBridge.TryPassPriority(out var error) && !string.IsNullOrEmpty(error))
                    setPrompt?.Invoke(error, true);
                UpdatePhaseState();
                return;
            }

            FinishTurn();
        }

        void FinishTurn()
        {
            if (round < PlaymatZones.MaxRound)
                round++;

            BeginTurn();
        }

        void OnEnginePhaseChanged(TurnStep _, bool __) => UpdatePhaseState();

        void UpdatePhaseState()
        {
            var currentStep = CurrentStep;
            var storeAllowed = UseEngine
                ? matchBridge.IsStoreAllowed && !discardPending
                : currentStep == TurnStep.Main && !discardPending;
            storeView?.SetTurnStoreEnabled(storeAllowed);

            var prompt = discardPending
                ? $"Discard down to {GameConstants.MaxHandSize} cards ({handView?.HandCount ?? 0} in hand)"
                : currentStep switch
                {
                    TurnStep.WillSite =>
                        $"Round {Round} — Will {WillPool}. Will site step (optional), then continue.",
                    TurnStep.Main =>
                        $"Round {Round} — Will {WillPool}. Main phase — play cards and use the store.",
                    TurnStep.Clash =>
                        $"Round {Round} — Will {WillPool}. Clash phase — Press or Hold, then End Turn.",
                    _ => null,
                };

            setPrompt?.Invoke(prompt, !string.IsNullOrEmpty(prompt));
            PhaseStateChanged?.Invoke(currentStep, discardPending);
        }

        public event Action<TurnStep, bool> PhaseStateChanged;

        public bool ShouldShowNextPhaseButton =>
            !discardPending && (UseEngine
                ? matchBridge.ShouldShowNextPhaseButton
                : step == TurnStep.WillSite || step == TurnStep.Main);

        public bool ShouldShowEndTurnButton =>
            !discardPending && (UseEngine
                ? matchBridge.ShouldShowEndTurnButton
                : step == TurnStep.Clash);
    }
}
