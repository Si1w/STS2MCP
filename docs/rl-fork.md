# RL fork additions

Branch `rl` of [Si1w/STS2MCP](https://github.com/Si1w/STS2MCP). Upstream is
[Gennadiyev/STS2MCP](https://github.com/Gennadiyev/STS2MCP) (MIT); this branch is rebased on
upstream `main` when the game updates. All additions live in `McpMod.Rl.cs` plus small,
commented (`// RL:`) hunks in `McpMod.cs`, `McpMod.Actions.cs` and `McpMod.StateBuilder.cs`.

## Actions (POST `/api/v1/singleplayer`, no run required)

| action | effect |
|---|---|
| `reveal_epochs` | Reveal every obtained-but-unrevealed Timeline epoch. The main menu disables *Singleplayer* until new epochs are revealed, which a headless agent cannot do by clicking. Returns `revealed: [ids]`. |
| `abandon_run` | Abandon the current run and return to the main menu from any screen. Recovery path for soft-locks (a crashed game task can leave player actions disabled forever). |

## State additions (combat only)

- `battle.actions_enabled`: true only when the game would accept `play_card` / `end_turn`
  right now. `is_play_phase` alone is not enough: actions stay disabled while card and enemy
  effects resolve.
- `battle.flags`: the raw `CombatManager` gates behind that boolean
  (`player_actions_disabled`, `ending_turn_phase_one/two`, `is_enemy_turn_started`,
  `is_paused`, `is_executing_effect`, `hand_in_card_play`), for diagnosing stuck states.

## Behaviour changes

- `end_turn` is refused while a card or potion effect is still executing or the turn is
  already ending. Ending the turn in that window overlaps the game's own tasks and can
  crash them ("Hand size 11 is greater than 11!") and soft-lock the run.

## Building

```bash
export STS2_GAME_ROOT="/path/to/Slay the Spire 2"   # or -p:STS2GameDir=...
dotnet build -c Release
```

`STS2_MCP.csproj` picks whichever data directory exists under the game root, so building in
WSL against the Windows install works. Output: `bin/Release/net9.0/STS2_MCP.dll`; install it
with `mod_manifest.json` renamed to `STS2_MCP.json` as described in the README.
