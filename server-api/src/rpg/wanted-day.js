/**
 * Guard for the daily Wanted Boss claim day (SaveData.wantedDay): the stored day never goes backwards, so a stale or old zone build
 * can never reopen a claim. It clamps instead of rejecting, because a rejected save would spool the whole character.
 * NOTE: it mutates `newState.wantedDay` in place (only that field), before the state is written.
 */
export function keepWantedDay(oldState, newState) {
  newState.wantedDay = Math.max(oldState?.wantedDay ?? 0, newState.wantedDay ?? 0);
}
