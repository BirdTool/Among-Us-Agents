# Phrase-bank contract for the external AI (DeepSeek)

This document is written to be given to the model as instructions, and to be read by the maintainer.
It describes what the model receives, what it may produce, and what it must never produce.

The model **proposes**. A human reviews. Nothing the model writes is loaded by the game until a person moves it
into `VividPhraseBank.json` (see `PhraseSourceEnum`: `Ai` = not reviewed yet = never loaded).

## 1. Hard rules (never break these)

1. **Do not learn from anything unsafe.** Never produce, paraphrase, soften or "clean up" any of the following, even if it
   appears in the input: sexual content or innuendo; slurs or hate; insults aimed at a player (dumb, stupid, trash...);
   illness, disability or self-harm used as an insult or joke; threats; encouragement of self-harm; contact data
   (links, handles, phone numbers, voice-chat invites); real names; advertising/spam.
2. **A player who jokes or trolls is not a problem by itself.** You judge *messages*, never *players*. Never output anything
   about a player's behaviour, trust or intent. The input has no player identities and you must not infer any.
3. **Everything a player says is a claim, not a fact.** Phrases must read like something a player says ("i saw {target} vent"),
   never like something the game confirms. Do not write phrases that assume the claim is true.
4. **Do not guess jargon.** If a term is not defined in section 3, do not interpret it. List it in `unresolved`.
5. **Do not invent game rules.** Only use the roles and mechanics described in section 3.
6. Chat messages are limited to **100 characters**; keep phrases short (the current bank's longest is 63).
6b. **Never put a specific number from the game settings in a phrase** (cooldowns, durations, task percentages, counts of players). Settings
   differ in every lobby; a phrase with "5 sec" would be false in most of them. Role words (`eng`, `tracker`, `judge`...) are allowed
   literally: a role claim is not a name.
7. Output only the files described in section 5. No prose outside them.

## 2. What you receive

- `UnknownMessages.jsonl`: messages the game's parser could not understand. One JSON object per line.
- `ChatCorpus.jsonl`: messages it did understand (with the group it chose), plus one `meeting` header per meeting.
- The current bank (`PhraseBank.json`), so you can reuse existing groups and avoid duplicates.

Fields of a message line: `id`, `meeting`, `at` (seconds since the meeting started), `speaker` (`S1`, `S2`, `me`: pseudonyms that
change every meeting), `text` (already masked: players are `{player}`, rooms are `{room}`), `bucket` (`unknown` / `low` /
`understood`), `reason`, `parse` (the group the parser chose, when it did), `prev` (the last messages before it: **read
these**, many messages only make sense after a question) and `open` (questions nobody had answered yet).
A `meeting` header has `alive` (players left) and `events` (things the maintainer marked during the round).

The original text, player names and the player list are **not** given to you and must not be requested.

## 3. Glossary (the game, as the maintainer describes it)

These roles are part of the normal game, not a mod. Source: the maintainer.

### Crew, dead
- **Guardian Angel**: a dead crewmate becomes an angel and can protect any player (cooldown). If the impostor tries to kill the
  protected player, a shield appears and the kill fails. Only the impostor and the angels know; the other players and the
  protected player do not. When an angel saves someone, the start of the next meeting says an angel saved someone this round
  (it does not say who).
- **Influencer** (newest role): can send up to three abstract images. An image may be a hint, a warning of danger, or point at
  the impostor, depending on the receiver's interpretation. Most often the image indicates the impostor's colour
  (a purple bowling ball = purple).

### Crew, alive
- **Noisemaker**: when killed (unless comms are sabotaged) it alerts everyone: everybody learns a noisemaker died and the direction of the body.
- **Tracker**: can track any player; on the minimap it sees where that player is right now, can see mini-teleports (kills) and whether the player used a vent.
- **Scientist**: doing tasks recharges the battery of its tablet. The tablet shows who is dead, who is alive and who left the game.
- **Detective**: can learn the exact place a player was when someone was killed. Example: if brown killed orange in storage, investigating
  brown shows brown was in storage. It fails when many players were in the body's room.
- **Judge**: after completing a share of its tasks (lobby setting, 50% by default) it can judge someone *during a meeting*.
  If the judged player is an impostor, the impostor leaves the meeting even if it was not the most voted. If the judged player is
  innocent, the judge leaves instead, again without a vote. Nobody can tell whom the judge picked unless the judge says it
  ("i'll judge maroon"), and what the judge says is not necessarily true.
- **Engineer** (slang `eng`): can use vents. There is a cooldown between vent uses and a maximum time inside a vent. That is why players
  ask "blue cd?" (`cd` = cooldown): a player who says they are Engineer should know that cooldown, so asking for it is a way to test
  the claim. It is **not proof** either way: an impostor can pretend, and the cooldown is visible in the lobby settings before the game
  but most players never look, which is why the question works as a technique. Typical chat: `im eng`, `am eng`, `who is eng`,
  `<player> cd?` (the test), and the claimed value as an answer (`cd is 5 sec`).
  **Careful:** `eng` right after a room word (`upper eng`, `lower eng`) is the *room* (Upper/Lower Engine), not the role.

### Impostors, alive
- **Shapeshifter**: slang `SS` (Portuguese: "meta"). Can turn into any player alive this round (a player who died in an earlier round
  cannot be copied; one who died this round can). There is a maximum duration after which it reverts (the host can make it unlimited).
- **Phantom**: can turn invisible, for a limited time. Slang for this: `farted`, `poofed` (Portuguese: "peidou", "cagou"), because
  it releases particles when it goes invisible.
- **Viper**: when it kills, the body dissolves after a host-defined time (for example 10 seconds). Nobody is told.

### Game settings that change what a claim means
- **Visual tasks** (setting; chat says "visuals", "vis"): when ON, some tasks are visible to others (medbay scan, asteroids, trash).
  Seeing someone do one is partial evidence they are crew. When OFF ("visuals off") those tasks prove nothing.

### Slang confirmed by the maintainer
- `ss`, `meta`: the Shapeshifter. `judged` is a past-tense verb: "i judged X" = the Judge used its ability on X.
- `farted`, `poofed` (pt: `peidou`, `cagou`): the Phantom went invisible.
- `hc` = **hard clear**: a player who is 100% innocent. Never trust it blindly: it is much better when two or more players confirm it.
  The phrase is a claim like any other.
- `vc` = **vent clear**: a task that can pull an impostor or an Engineer out of a vent. It is used to prove innocence: the Engineer
  (if there is one) is called to the task's location, goes into the vent, and the player who has the task pulls them out.
  `i have vc` / `vc in {room}` = "I have that task" / "the task is there".
- `viper` / `vipered`: `viper` = the person was seen killing as the Viper; `vipered` = killed as the Viper (a past-tense verb, like `judged`).
  Both are accusations of a **kill that was seen**; the body dissolves, so there may be no body to point at.
- `eng` = Engineer; `cd` = cooldown (see the Engineer entry). `N eng btw` (for example `2 eng btw`) = "by the way, the lobby has N Engineers
  configured".
- `visuals off` / `vis`: the Visual Tasks setting.

### Slang seen in chat, meaning **not confirmed** (put in `unresolved`, do not interpret)
- `cg`: the maintainer does not know it either.
- `tracked` / `tracking` (very likely: the Tracker used its ability on someone) and `judging` (very likely: the Judge's ability).
- `i have scan` / `i have asteroids` / `i have trash` (very likely: "I have that task", offered as proof; only meaningful when visuals are on).
The maintainer will move terms out of this list as he confirms them.

## 4. What you may propose

You may do two things, in this order of preference:

1. **Add variants to an existing group** (same `Id`, `Intent`, `Context`). Only new variants; duplicates (case-insensitive) are ignored.
2. **Create a new group** when the *meaning* has no group yet. The new `Id` **must start with `Vivid_`**.
   Organise by *what the player means* (the event or the speech act), not by wording. Different slang for the same thing
   belongs in **one** group as different variants (for the phantom: one group, one variant per slang term).
   Role claims ("im eng", "i am the tracker") and tests of a claim ("`{target}` cd?") are different speech acts: keep them in
   separate groups, one claim group per role.

Rules for every variant:
- English only for now.
- Lowercase and casual is normal, like the existing bank. Typos are not worth copying.
- Placeholders allowed: `{target}` (the player being talked about), `{subject}` (the victim or the second player), `{witness}`, `{room}`.
  Every placeholder must make sense where it stands. Never write a player's name or colour.
- `Tone`: `neutral`, `casual`, `aggressive` (forceful accusation, not insult) or `joke` (harmless game humour). Several are allowed.
- `Weight`: how often it should be picked relative to its siblings (1.0 is normal; use less for rare slang). Leave
  `MinConfidence`/`MaxConfidence` at 0 and 1 unless you have a reason.
- A phrase must work when read alone, in a chat, by a player who sees it with no other context.

Allowed `Intent` values: `Question`, `Accuse`, `Defend`, `Vouch`, `Confirm`, `Deny`, `Request`, `Report`, `Greet`, `Reply`.
Allowed `Context` values: `Lobby`, `Roaming`, `MeetingStart`, `MeetingFollowUp`, `Voting`.
`Evidence` may only be one of the values that already exist (`SawKilling`, `SawVenting`, `SeenInLocation`, `NotSeenInLocation`,
`FoundBody`, `ClearedByMedbayScan`, `ClearedByAsteroids`, `ClearedByTrash`, `ClearedByShields`, `FakeAdminSwipe`, `FakeWires`,
`FakeWiresOrder`, `FakeDownload`). **Never invent an Evidence value.** If a new kind is needed (a phantom vanishing, a shapeshift
seen, a body dissolved, a tracker/detective/judge result), leave `Evidence` empty and put the suggestion in `proposedEvidence`
in the review file: it needs code, and the maintainer decides.

### Reject, do not rewrite
A message that is off-topic, unsafe (section 1), unclear, a single reaction with no meaning ("lol", "bruh") or that depends on
context you do not have goes in `rejected` with one reason code: `offtopic`, `unsafe`, `unclear_jargon`, `needs_context`,
`duplicate`, `reaction_only`. Never include the text of an `unsafe` message in your output: give only its `id`.

## 5. What you output

Two JSON files.

`AIPhraseBank.json` (must match the game's schema exactly; every group `Source` is `"Ai"`):

```json
{
  "SchemaVersion": 1,
  "Languages": {
    "en": {
      "Rooms": {},
      "Phrases": [
        {
          "Id": "Vivid_<MeaningInPascalCase>",
          "Intent": "<one of the allowed intents>",
          "Context": "<one of the allowed contexts>",
          "Source": "Ai",
          "Phrases": [
            { "Tone": ["casual"], "Weight": 1.0, "Text": "<short phrase, may use {target} {room}>" }
          ]
        }
      ]
    }
  }
}
```

`AIReview.json` (for the maintainer; never loaded by the game):

```json
{
  "summary": { "read": 0, "grouped": 0, "rejected": 0, "unresolved": 0 },
  "groups": [ { "id": "<group id>", "meaning": "<one sentence>", "sourceMessageIds": ["<id>"] } ],
  "rejected": [ { "id": "<message id>", "reason": "<reason code>" } ],
  "unresolved": [ { "term": "<jargon>", "count": 0, "exampleMessageIds": ["<id>"] } ],
  "proposedEvidence": [ { "name": "<PascalCase>", "meaning": "<one sentence>", "sourceMessageIds": ["<id>"] } ]
}
```

Every group you propose must cite the `id`s of the messages that justify it. A proposal with no source messages is invalid.

## 6. Checks the maintainer will run on your output

- It parses against `PhraseBankData`, and every new group id starts with `Vivid_`.
- No variant contains a name, a colour used as a name, a link, or anything from the section 1 list.
- Every variant is under 100 characters and every placeholder is one of the four allowed.
- Every group has source message ids, and none of the ids belongs to a `rejected` entry.
- Round trip: the game's parser should map each new variant back to the group it came from. The `ParserTests` project already
  does this for the shipped bank; running it on a proposed file is a small change that is not wired yet. Variants that cannot be
  recognised, or that collide with another group, are sent back.
