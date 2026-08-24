# CTA + Luna Integration (Project-specific)

## Current integration points

- Session start: `GameController.OnLoadLevelDone()` -> `_ctaOrchestrator.StartSession()`
- Endgame trigger:
  - Win: `OnLevelWin()` -> `ShowEndGame(true, CtaReason.Win)`
  - Pre-lose: `OnPreLose()` -> `ShowEndGame(false, CtaReason.PreLose)`
  - Lose: `ShowEndGameLose()` -> `ShowEndGame(false, CtaReason.Lose)`
- Manual CTA trigger: `GameEventBus.OnWinClaimButtonClicked` -> `_ctaOrchestrator.OnManualCtaButtonClick()`
- Mid-game click threshold: `GameEventBus.OnSelectBucket` -> `_ctaOrchestrator.NotifySelectableClick()`

## Architecture layers

1. Trigger layer: `GameController` + optional `GameplayCtaTriggerSource`
2. Orchestrator: `CTAOrchestrator`
3. Presenter (UI): `CTAPresenter`
4. Platform adapter:
   - `LunaPlayableAdapter` (reflection-based, idempotent `GameStarted`/`GameEnded`)
   - `NoOpPlayableAdapter` (local/testing)

## Config

`CTAConfig` controls:

- Global policy (`enableCtaSystem`, `useLunaAdapter`, debounce, duplicate install guard)
- Win/Lose behavior (`autoEndGameStoreOnWin`, `autoEndGameStoreOnLose`, fallback mode)
- Mid-game CTA (`enableClickThresholdCta`, `clickThreshold`, continuous mode)
- Tutorial suppression (`suppressCtaInTutorial`, `suppressThresholdCTAInTutorial`)

## Scene setup checklist

1. Create `CTAConfig` asset from `Create -> Playable -> CTA Config`.
2. Assign `ctaConfig` in `GameController`.
3. Add `CTAPresenter` on CTA panel object (if using UI CTA) and assign it to `GameController.ctaPresenter`.
4. Hook CTA button in presenter (`ctaButton`) and texts (`titleText`, `messageText`, `buttonText`) as needed.
5. If no presenter is assigned and mode is `ShowUIThenInstallOnClick`, orchestrator falls back to immediate install.
