using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace ValleyTalk;

public class ValleyTalkInterface : IValleyTalkInterface
{
    private string _modName = string.Empty;

    public ValleyTalkInterface()
    {
        _modName = Guid.NewGuid().ToString();
    }

    public void SetModName(string modName)
    {
        _modName = modName;
    }

    public bool IsEnabledForCharacter(NPC character)
    {
        return DialogueBuilder.Instance.PatchNpc(character);
    }

    public void RegisterPromptOverride(string characterName, string promptElement, string overrideText)
    {
        ModInteropManager.Instance.RegisterPromptOverride(_modName, characterName, promptElement, overrideText);
    }

    public void ClearPromptOverride(string characterName, string promptElement)
    {
        ModInteropManager.Instance.ClearPromptOverride(_modName, characterName, promptElement);
    }

    public void ClearPromptOverrides(string characterName = "")
    {
        ModInteropManager.Instance.ClearPromptOverrides(_modName, characterName);
    }

    public void AddPromptContributor(Func<NPC, string, string> contributor)
    {
        ModInteropManager.Instance.AddPromptContributor(_modName, contributor);
    }

    public void AddLineProcessor(Func<NPC, string, string> processor)
    {
        ModInteropManager.Instance.AddLineProcessor(_modName, processor);
    }

    public void AddPlayerLineListener(Action<NPC, string> listener)
    {
        ModInteropManager.Instance.AddPlayerLineListener(_modName, listener);
    }

    public void SetGameSummary(string summary)
    {
        ModInteropManager.Instance.GameSummaryOverride = string.IsNullOrWhiteSpace(summary) ? null : summary;
    }

    public bool SendPlayerLine(NPC npc, string text, bool continueConversation)
    {
        if (npc == null || string.IsNullOrWhiteSpace(text) || AsyncBuilder.Instance.AwaitingGeneration)
        {
            return false;
        }

        var history = new List<ConversationElement>();
        if (continueConversation && DialogueBuilder.Instance.LastContext != null)
        {
            history.AddRange(DialogueBuilder.Instance.LastContext.ChatHistory);
        }
        else
        {
            DialogueBuilder.Instance.ClearContext();
        }

        void AddNpcLine(string line)
        {
            if (!string.IsNullOrWhiteSpace(line) && line != "skip" && line != Util.GetString("outputRespond") && !history.Any(h => h.Text.Contains(line)))
            {
                history.Add(new ConversationElement(line, false));
            }
        }

        // What the NPC is saying right now is what the player is answering.
        if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox box && box.characterDialogue?.speaker == npc)
        {
            foreach (var line in box.characterDialogue.dialogues)
            {
                AddNpcLine(line.Text);
            }
            box.closeDialogue();
        }
        else if (continueConversation)
        {
            AddNpcLine(ModInteropManager.Instance.LastGeneratedLine(npc));
        }

        DialogueBuilder.Instance.AddConversation(npc, text, isPlayerLine: true);
        ModInteropManager.Instance.NotifyPlayerLine(npc, text);
        history.Add(new ConversationElement(text, true));
        npc.grantConversationFriendship(Game1.player);
        // Generation starts once no menu is open (the dialogue box closed above).
        AsyncBuilder.Instance.RequestNpcResponse(npc, history);
        return true;
    }
}
