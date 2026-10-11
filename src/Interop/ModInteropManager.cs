using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace ValleyTalk;

public class ModInteropManager
{
    private ModInteropManager() { }
    private static ModInteropManager _instance;
    public static ModInteropManager Instance => _instance ??= new ModInteropManager();
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> _promptOverrides = new();

    public void RegisterPromptOverride(string modName, string characterName, string promptElement, string overrideText)
    {
        if (!_promptOverrides.ContainsKey(characterName))
        {
            _promptOverrides[characterName] = new Dictionary<string, Dictionary<string, string>>();
        }
        if (!_promptOverrides[characterName].ContainsKey(promptElement))
        {
            _promptOverrides[characterName][promptElement] = new Dictionary<string, string>();
        }
        _promptOverrides[characterName][promptElement][modName] = overrideText;
    }

    public void ClearPromptOverride(string modName, string characterName, string promptElement)
    {
        if (_promptOverrides.ContainsKey(characterName) && _promptOverrides[characterName].ContainsKey(promptElement))
        {
            _promptOverrides[characterName][promptElement].Remove(modName);
            if (_promptOverrides[characterName][promptElement].Count == 0)
            {
                _promptOverrides[characterName].Remove(promptElement);
                if (_promptOverrides[characterName].Count == 0)
                {
                    _promptOverrides.Remove(characterName);
                }
            }
        }
    }

    public void ClearPromptOverrides(string modName, string characterName = "")
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            foreach (var character in _promptOverrides.Keys.ToList())
            {
                foreach (var promptElement in _promptOverrides[character].Keys.ToList())
                {
                    _promptOverrides[character][promptElement].Remove(modName);
                    if (_promptOverrides[character][promptElement].Count == 0)
                    {
                        _promptOverrides[character].Remove(promptElement);
                    }
                }
                if (_promptOverrides[character].Count == 0)
                {
                    _promptOverrides.Remove(character);
                }
            }
        }
        else
        {
            if (_promptOverrides.ContainsKey(characterName))
            {
                foreach (var promptElement in _promptOverrides[characterName].Keys.ToList())
                {
                    _promptOverrides[characterName][promptElement].Remove(modName);
                    if (_promptOverrides[characterName][promptElement].Count == 0)
                    {
                        _promptOverrides[characterName].Remove(promptElement);
                    }
                }
                if (_promptOverrides[characterName].Count == 0)
                {
                    _promptOverrides.Remove(characterName);
                }
            }
        }
    }

    internal Dictionary<string, IEnumerable<string>> GetPromptOverrides(Character character)
    {
        if (_promptOverrides.TryGetValue(character.Name, out var overrides))
        {
            return overrides.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Values.AsEnumerable());
        }
        return [];
    }

    // ------------------------------------------------------------------ callbacks registered by other mods

    private readonly List<(string mod, Func<NPC, string, string> fn)> _promptContributors = new();
    private readonly List<(string mod, Func<NPC, string, string> fn)> _lineProcessors = new();
    private readonly List<(string mod, Action<NPC, string> fn)> _playerLineListeners = new();

    internal string GameSummaryOverride { get; set; }

    private readonly Dictionary<string, string> _lastGeneratedLines = new();

    /// <summary>The last line generated for this NPC (after processing), so a later spoken reply can continue from it.</summary>
    internal string LastGeneratedLine(NPC npc) => npc != null && _lastGeneratedLines.TryGetValue(npc.Name, out var line) ? line : null;

    internal void RecordGeneratedLine(NPC npc, string line)
    {
        if (npc != null && !string.IsNullOrWhiteSpace(line)) _lastGeneratedLines[npc.Name] = line;
    }

    public void AddPromptContributor(string modName, Func<NPC, string, string> contributor) => _promptContributors.Add((modName, contributor));

    public void AddLineProcessor(string modName, Func<NPC, string, string> processor) => _lineProcessors.Add((modName, processor));

    public void AddPlayerLineListener(string modName, Action<NPC, string> listener) => _playerLineListeners.Add((modName, listener));

    /// <summary>Text other mods want appended to the given prompt slot ("System", "Instructions" or "End") for this NPC.</summary>
    internal IEnumerable<string> GetPromptContributions(NPC npc, string slot)
    {
        foreach (var (mod, fn) in _promptContributors)
        {
            string text = null;
            try { text = fn(npc, slot); }
            catch (Exception ex) { Log.Error($"Prompt contributor from {mod} failed: {ex.Message}"); }
            if (!string.IsNullOrWhiteSpace(text)) yield return text;
        }
    }

    internal string ProcessLine(NPC npc, string line)
    {
        foreach (var (mod, fn) in _lineProcessors)
        {
            if (string.IsNullOrEmpty(line)) break;
            try { line = fn(npc, line); }
            catch (Exception ex) { Log.Error($"Line processor from {mod} failed: {ex.Message}"); }
        }
        return line;
    }

    internal void NotifyPlayerLine(NPC npc, string text)
    {
        if (npc == null || string.IsNullOrWhiteSpace(text)) return;
        foreach (var (mod, fn) in _playerLineListeners)
        {
            try { fn(npc, text); }
            catch (Exception ex) { Log.Error($"Player line listener from {mod} failed: {ex.Message}"); }
        }
    }
}
