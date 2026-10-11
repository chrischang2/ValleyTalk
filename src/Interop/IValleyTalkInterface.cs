using System;

namespace ValleyTalk;

public interface IValleyTalkInterface
{
    void SetModName(string modName);
    bool IsEnabledForCharacter(StardewValley.NPC character);
    void RegisterPromptOverride(string characterName, string promptElement, string overrideText);
    void ClearPromptOverride(string characterName, string promptElement);
    void ClearPromptOverrides(string characterName = "");

    /// <summary>
    /// Add text to every prompt, worked out fresh for each request. The contributor gets the NPC and a slot name and
    /// returns text to append there (or null): "System" (end of the system prompt), "Instructions" (end of the
    /// formatting instructions) or "End" (the very end of the prompt, which small models weigh most).
    /// </summary>
    void AddPromptContributor(Func<StardewValley.NPC, string, string> contributor);

    /// <summary>Rewrite each generated line (the dialogue and any response options) before it reaches the game. Return null or empty to drop it.</summary>
    void AddLineProcessor(Func<StardewValley.NPC, string, string> processor);

    /// <summary>Called with each line the player sends to an NPC (a chosen response or typed text).</summary>
    void AddPlayerLineListener(Action<StardewValley.NPC, string> listener);

    /// <summary>Replace the built-in game summary sent with every prompt (useful for small local models); null restores the default.</summary>
    void SetGameSummary(string summary);

    /// <summary>
    /// Say something to an NPC as if it had been typed (e.g. from speech recognition); the NPC's reply is generated as usual.
    /// If a dialogue box with this NPC is open, it is closed and its lines join the conversation. With
    /// <paramref name="continueConversation"/> false a new conversation starts. Returns false if a reply is already being generated.
    /// </summary>
    bool SendPlayerLine(StardewValley.NPC npc, string text, bool continueConversation);
}
