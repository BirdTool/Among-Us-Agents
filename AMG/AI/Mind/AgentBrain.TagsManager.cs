using AMG.Enums;
using AMG.Models;
using System.Collections.Generic;
using System.Text;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        public List<AgentTag> tags = [];
        public bool CanSeeTheRedNameAsCrewmate = true;

        public void AddNameTag(string tag, string hexColour, IdentifierEnum identifier, string size = "80%", float? expiresAt = null)
        {
            tags.Add(new AgentTag { Tag = tag, ColourHex = hexColour, Size = size, Identifier = identifier, ExpiresAt = expiresAt });
            RefreshNameTag();
        }

        public void AddNameTag(AgentTag tag)
        {
            tags.Add(tag);
            RefreshNameTag();
        }

        public void RemoveNameTag(IdentifierEnum identifier)
        {
            tags.RemoveAll(t => t.Identifier == identifier);
            RefreshNameTag();
        }

        public void RemoveNameTag(AgentTag tag)
        {
            tags.RemoveAll(t => t == tag);
            RefreshNameTag();
        }

        public void ReplaceNameTag(IdentifierEnum identifier, string newTag, string newHexColour, string newSize = "80%", float? expiresAt = null)
        {
            var existing = tags.Find(t => t.Identifier == identifier);
            if (existing != null)
            {
                existing.Tag = newTag;
                existing.ColourHex = newHexColour;
                existing.Size = newSize;
                if (expiresAt != null)
                {
                    existing.ExpiresAt = expiresAt;
                }
                else
                {
                    if (existing.ExpiresAt != null)
                    {
                        existing.ExpiresAt = null;
                    }
                }
                RefreshNameTag();
            }
            else
            {
                AddNameTag(newTag, newHexColour, identifier, newSize);
            }
        }

        public void ReplaceNameTag(IdentifierEnum identifier, AgentTag tag)
        {
            var existing = tags.Find(t => t.Identifier == identifier);
            if (existing != null)
            {
                existing.Tag = tag.Tag;
                existing.ColourHex = tag.ColourHex;
                existing.Size = tag.Size;
                RefreshNameTag();
            }
            else
            {
                AddNameTag(tag);
            }
        }

        public void ReplaceNameTag(AgentTag tag)
        {
            var existing = tags.Find(t => t.Identifier == tag.Identifier);
            if (existing != null)
            {
                existing.Tag = tag.Tag;
                existing.ColourHex = tag.ColourHex;
                existing.Size = tag.Size;
                RefreshNameTag();
            }
            else
            {
                AddNameTag(tag);
            }
        }

        public void ReplaceNameTag(AgentTag tag, float expiresAt)
        {
            var existing = tags.Find(t => t.Identifier == tag.Identifier);
            if (existing != null)
            {
                existing.Tag = tag.Tag;
                existing.ColourHex = tag.ColourHex;
                existing.Size = tag.Size;
                existing.ExpiresAt = expiresAt;
                RefreshNameTag();
            }
            else
            {
                tag.ExpiresAt = expiresAt;
                AddNameTag(tag);
            }
        }

        public void SetTags(List<AgentTag> newTags)
        {
            tags = newTags;
            RefreshNameTag();
        }

        private void RefreshNameTag()
        {
            if (nameTextComp != null)
            {
                StringBuilder builder = new();

                var states = tags.FindAll(t => t.Identifier == IdentifierEnum.State);
                var emotions = tags.FindAll(t => t.Identifier == IdentifierEnum.Emotion);
                var thoughts = tags.FindAll(t => t.Identifier == IdentifierEnum.Think);

                StringBuilder append(AgentTag tag) => builder.Append($"<color={tag.ColourHex}><size={tag.Size}>{tag.Tag}</size></color>\n");

                foreach (AgentTag tag in thoughts) append(tag);
                foreach (AgentTag tag in emotions) append(tag);
                foreach (AgentTag tag in states) append(tag);

                string displayBaseName = baseName;
                if (IsImpostor && ((PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.Role != null && PlayerControl.LocalPlayer.Data.Role.IsImpostor) || CanSeeTheRedNameAsCrewmate))
                {
                    displayBaseName = $"<color=red>{baseName}</color>";
                }

                nameTextComp.text = $"{builder}{displayBaseName}\n";
            }
        }
    }
}
