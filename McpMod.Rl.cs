// RL additions (fork: github.com/Si1w/STS2MCP, branch rl). Actions that a headless
// training agent needs and that require no run in progress. Kept in a separate partial
// file so the upstream files stay close to verbatim.
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2_MCP;

public static partial class McpMod
{
    /// <summary>
    /// Reveal every obtained-but-unrevealed Timeline epoch without opening the Timeline
    /// UI. The main menu disables Singleplayer until new epochs are revealed, which a
    /// headless agent cannot do by clicking. Uses the game's own SaveManager.RevealEpoch.
    /// </summary>
    private static Dictionary<string, object?> ExecuteRevealEpochs()
    {
        var pending = GetProgressEpochIdsByState("Obtained", "ObtainedNoSlot");
        if (pending.Count == 0)
            return new Dictionary<string, object?> { ["status"] = "ok", ["message"] = "No epochs pending reveal", ["revealed"] = new List<string>() };

        var revealed = new List<string>();
        var errors = new List<string>();
        var save = SaveManager.Instance;
        foreach (var id in pending)
        {
            try { save.RevealEpoch(id, false); revealed.Add(id); }
            catch (Exception ex) { errors.Add($"{id}: {ex.GetType().Name}: {ex.Message}"); }
        }
        try { save.SaveProfile(); } catch (Exception ex) { errors.Add($"SaveProfile: {ex.Message}"); }

        try
        {
            var tree = Godot.Engine.GetMainLoop() as Godot.SceneTree;
            var mainMenu = tree != null ? FindFirst<NMainMenu>(tree.Root) : null;
            mainMenu?.RefreshButtons();
        }
        catch (Exception ex) { errors.Add($"RefreshButtons: {ex.Message}"); }

        var result = new Dictionary<string, object?>
        {
            ["status"] = errors.Count == 0 ? "ok" : "error",
            ["message"] = $"Revealed {revealed.Count} epoch(s)",
            ["revealed"] = revealed,
        };
        if (errors.Count > 0) result["error"] = string.Join("; ", errors);
        return result;
    }

    /// <summary>
    /// Abandon the current run and return to the main menu, regardless of screen. This is
    /// the recovery path when the game soft-locks (e.g. a crashed draw task leaves
    /// PlayerActionsDisabled set forever).
    /// </summary>
    private static Dictionary<string, object?> ExecuteAbandonRun()
    {
        if (!RunManager.Instance.IsInProgress)
            return new Dictionary<string, object?> { ["status"] = "ok", ["message"] = "No run in progress" };
        try
        {
            RunManager.Instance.Abandon();
            var tree = Godot.Engine.GetMainLoop() as Godot.SceneTree;
            var game = tree != null ? FindFirst<NGame>(tree.Root) : null;
            if (game == null)
                return Error("NGame node not found; run abandoned but cannot return to menu");
            _ = game.ReturnToMainMenu();
            return new Dictionary<string, object?> { ["status"] = "ok", ["message"] = "Run abandoned; returning to main menu" };
        }
        catch (Exception ex)
        {
            return Error($"abandon_run failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
