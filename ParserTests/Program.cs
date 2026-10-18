using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AMG.Models.ChatService;
using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Corpus;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;

static class T
{
    static int fail, pass;
    static readonly LanguagePack Pack = LoadPack();
    static Dictionary<SystemTypes, string> Rooms => Pack.Rooms;

    static LanguagePack LoadPack()
    {
        var opt = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() },
        };
        var path = Path.Combine(AppContext.BaseDirectory, "PhraseBank.json");
        return JsonSerializer.Deserialize<PhraseBankData>(File.ReadAllText(path), opt).Languages["en"];
    }

    static readonly List<ChatPlayer> Players =
    [
        new(0, "Guilherme", "Cyan"), new(1, "James Smith", "Red"), new(2, "Luna Doe", "White"),
        new(3, "Impostor Santos", "Blue"), new(4, "Cristiano Silva", "Green"), new(5, "Mary Brown", "Brown"),
        new(6, "John Santos", "Lime"),
    ];

    static ParseResult P(byte sender, string text, bool register = true)
    {
        ClaimStore.NoteMessage();   // the game counts every message, understood or not
        var r = ChatParser.Parse(text, sender, ClaimStore.Context, Players, Rooms, Pack.Phrases);
        if (register && r.Intent != null) ClaimStore.Register(sender, r.Intent, false, r.Confidence);
        return r;
    }

    static void Check(string label, bool ok, ParseResult r = null)
    {
        if (ok) pass++; else fail++;
        string d = r == null ? "" : r.Intent == null ? $"  -> UNKNOWN ({r.Rationale})"
            : $"  -> {r.Intent.Type}/{r.Intent.PhraseId} room={r.Intent.Room} target={r.Intent.Target} reply={r.Intent.ReplyToClaim} conf={r.Confidence:0.00} ({r.Rationale})";
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {label}{d}");
    }

    static bool Is(ParseResult r, ChatIntentEnum type, string id, SystemTypes? room = null, byte? target = null) =>
        r.Intent != null && r.Intent.Type == type && r.Intent.PhraseId == id && r.Intent.Room == room && r.Intent.Target == target;

    static void NewMeeting(float t = 0) { UnityEngine.Time.time = t; ClaimStore.Reset(); }

    static void Main()
    {
        Console.WriteLine("== bare rooms, nothing asked, meeting just started (reporter says only the room)");
        NewMeeting(); UnityEngine.Time.time = 3;
        foreach (var (txt, room) in new[] { ("elec", SystemTypes.Electrical), ("upper", SystemTypes.UpperEngine), ("lower", SystemTypes.LowerEngine),
                 ("nav", SystemTypes.Nav), ("cafe", SystemTypes.Cafeteria), ("med", SystemTypes.MedBay), ("o2", SystemTypes.LifeSupp),
                 ("life support", SystemTypes.LifeSupp), ("it was in electrical tho", SystemTypes.Electrical), ("ue", SystemTypes.UpperEngine),
                 ("le", SystemTypes.LowerEngine), ("sec", SystemTypes.Security), ("Electrical!!", SystemTypes.Electrical), ("ELEC", SystemTypes.Electrical) })
        {
            NewMeeting(); UnityEngine.Time.time = 3;
            var r = P(1, txt, false);
            Check($"'{txt}'", Is(r, ChatIntentEnum.Report, "Body_Location", room), r);
        }

        Console.WriteLine("\n== things that must NOT become a room");
        NewMeeting(); UnityEngine.Time.time = 3;
        foreach (var txt in new[] { "wait one sec", "engine", "engines", "up to you", "red sus" })
        { var r = P(1, txt, false); Check($"'{txt}' unknown", r.Intent == null, r); }

        Console.WriteLine("\n== late in the meeting a bare room is NOT assumed to be the body");
        NewMeeting(); UnityEngine.Time.time = 40;
        { var r = P(1, "elec", false); Check("'elec' at 40s, nothing asked", r.Intent == null, r); }

        Console.WriteLine("\n== body location statements / questions");
        NewMeeting(); UnityEngine.Time.time = 3;
        { var r = P(1, "body in electrical", false); Check("'body in electrical'", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical), r); }
        { var r = P(1, "found it in elec", false); Check("'found it in elec'", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical), r); }
        { var r = P(1, "i found the body in upper", false); Check("'i found the body in upper'", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.UpperEngine), r); }
        foreach (var txt in new[] { "where?", "Where was the body?", "loc?", "wheres the body", "where tho" })
        { var r = P(1, txt, false); Check($"'{txt}' is a body question", Is(r, ChatIntentEnum.Question, "Body_Location"), r); }
        { var r = P(1, "body in elec?", false); Check("'body in elec?' not a statement", r.Intent == null, r); }

        Console.WriteLine("\n== someone asks 'where?', the human answers with a bare room");
        NewMeeting(); UnityEngine.Time.time = 2;
        var q = ClaimStore.Register(3, new ChatIntent { Type = ChatIntentEnum.Question, PhraseId = "Body_Location" }, true, 1f);
        { var r = P(0, "elec"); Check("'elec' answers agent's 'Where?'", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical) && r.Intent.ReplyToClaim == q.Id, r); }
        Check("question closed", q.Status == ClaimStatus.Answered);
        Check("location now stated", ClaimStore.Any("Body_Location", ChatIntentEnum.Report));
        { var r = P(0, "i was in nav", false); Check("'i was in nav' is an alibi, not the body", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.Nav), r); }

        Console.WriteLine("\n== white wya? -> nav   (the case you described)");
        NewMeeting(); UnityEngine.Time.time = 12;
        { var r = P(0, "white wya?"); Check("'white wya?' asks white (id 2)", Is(r, ChatIntentEnum.Question, "Whereabouts_Ask", null, 2), r); }
        var wq = ClaimStore.OpenQuestionFor(2);
        Check("open question exists for white", wq != null);
        { var r = P(4, "nav", false); Check("'nav' from someone else is NOT white's answer", r.Intent == null || r.Intent.PhraseId != "Alibi_Tasks", r); }
        { var r = P(2, "nav"); Check("'nav' from white answers it", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.Nav) && r.Intent.ReplyToClaim == wq.Id, r); }
        Check("whereabouts question closed", wq.Status == ClaimStatus.Answered);

        Console.WriteLine("\n== both questions open: the one addressed to the sender wins");
        NewMeeting(); UnityEngine.Time.time = 5;
        ClaimStore.Register(3, new ChatIntent { Type = ChatIntentEnum.Question, PhraseId = "Body_Location" }, true, 1f);
        P(0, "white where were you");
        { var r = P(2, "nav", false); Check("white's 'nav' = whereabouts, not body", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.Nav), r); }
        { var r = P(4, "elec", false); Check("other player's 'elec' = body location", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical), r); }

        Console.WriteLine("\n== names, colours, typos");
        NewMeeting(); UnityEngine.Time.time = 12;
        foreach (var (txt, id) in new[] { ("james wya", (byte)1), ("where were you red", (byte)1), ("cristiano wya?", (byte)4), ("cristian wya", (byte)4),
                 ("luna where are u", (byte)2), ("Impostor Santos wya", (byte)3), ("wya mary", (byte)5) })
        { var r = P(0, txt, false); Check($"'{txt}' -> id {id}", Is(r, ChatIntentEnum.Question, "Whereabouts_Ask", null, id), r); }
        { var r = P(0, "santos wya", false); Check("'santos wya' ambiguous (2 Santos) -> unknown", r.Intent == null, r); }
        { var r = P(0, "impostor wya", false); Check("'impostor wya' is the word, not a player -> unknown", r.Intent == null, r); }

        Console.WriteLine("\n== agent-sent claims close questions without a parser");
        NewMeeting(); UnityEngine.Time.time = 1;
        var aq = ClaimStore.Register(3, new ChatIntent { Type = ChatIntentEnum.Question, PhraseId = "Body_Location" }, true, 1f);
        ClaimStore.Register(1, new ChatIntent { Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Room = SystemTypes.Admin }, true, 1f);
        Check("agent report closes agent question", aq.Status == ClaimStatus.Answered);

        Console.WriteLine("\n== free sentences matched against the PhraseBank");
        void T(string txt, string id, ChatIntentEnum? type = null, byte? target = null, SystemTypes? room = null, float at = 20, byte sender = 0)
        {
            NewMeeting(); UnityEngine.Time.time = at;
            var r = P(sender, txt, false);
            bool ok = r.Intent != null && r.Intent.PhraseId == id && (type == null || r.Intent.Type == type)
                && (target == null || r.Intent.Target == target) && (room == null || r.Intent.Room == room);
            Check($"'{txt}'", ok, r);
        }
        void NotId(string txt, string id)
        {
            NewMeeting(); UnityEngine.Time.time = 20;
            var r = P(0, txt, false);
            Check($"'{txt}' must not be {id}", r.Intent == null || r.Intent.PhraseId != id, r);
        }
        T("i saw red vent in cafe", "Vent_Culprit", ChatIntentEnum.Accuse, 1, SystemTypes.Cafeteria);
        T("red vented", "Vent_Culprit", ChatIntentEnum.Accuse, 1, null, 3);
        T("saw james kill someone in admin", "Kill_Culprit", ChatIntentEnum.Accuse, 1, SystemTypes.Admin);
        T("vote james", "Vote_Request", null, 1);
        T("lets vote luna", "Vote_Request", null, 2);
        T("who saw luna in nav?", "Seen_In_Room", ChatIntentEnum.Question, 2, SystemTypes.Nav);
        T("i saw mary in electrical", "Seen_In_Room", ChatIntentEnum.Report, 5, SystemTypes.Electrical);
        T("yeah james was in admin", "Seen_In_Room", ChatIntentEnum.Confirm, 1, SystemTypes.Admin);
        T("i trust mary", "Vouch_Generic", ChatIntentEnum.Vouch, 5);
        T("did anyone see the kill?", "Kill_Witness", ChatIntentEnum.Question);
        T("why do you suspect james?", "Suspicion_Why", ChatIntentEnum.Question, 1);
        T("reds wires looked weird", "Fake_Wires_Order", null, 1);
        T("why was the meeting called?", "Meeting_Reason", ChatIntentEnum.Question, null, null, 3);
        T("does anyone have info?", "Info_Ask", ChatIntentEnum.Question);
        T("i didnt see anything", "Info_None", ChatIntentEnum.Report);
        T("i didnt see james in cafe", "Not_Seen_In_Room", ChatIntentEnum.Report, 1, SystemTypes.Cafeteria);
        T("ok", "Ack_Generic");
        T("skip this one", "Vote_Skip");
        T("i was never in admin", "Seen_In_Room", ChatIntentEnum.Deny, null, SystemTypes.Admin);
        NotId("red didnt vent", "Vent_Culprit");
        NotId("red never vented", "Vent_Culprit");
        NotId("james didnt kill anyone", "Kill_Culprit");

        Console.WriteLine("\n== body location said before / late");
        NewMeeting(); UnityEngine.Time.time = 25;
        { var r = P(0, "it was in electrical", false); Check("late 'it was in electrical' with nobody having said it", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical), r); }
        { var r = P(0, "elec", false); Check("late bare 'elec' stays unknown", r.Intent == null, r); }
        NewMeeting(); UnityEngine.Time.time = 3;
        ClaimStore.Register(1, new ChatIntent { Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Room = SystemTypes.Electrical }, true, 1f);
        UnityEngine.Time.time = 9;
        { var r = P(0, "yeah elec", false); Check("'yeah elec' confirms", Is(r, ChatIntentEnum.Confirm, "Body_Location", SystemTypes.Electrical), r); }
        { var r = P(0, "nav", false); Check("'nav' disputes (different room)", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Nav), r); }
        { var r = P(0, "body in electrical", false); Check("'body in electrical' agrees", Is(r, ChatIntentEnum.Confirm, "Body_Location", SystemTypes.Electrical), r); }
        NotId("saw red vent", "Vent_Witness");           // a statement must not become the question "Who saw {target} vent?"
        foreach (var vague in new[] { "i saw red", "saw red", "red?" })
        {
            NewMeeting(); UnityEngine.Time.time = 20;
            var r = P(0, vague, false);
            Check($"'{vague}' is too vague to become an accusation/claim", r.Intent == null, r);
        }
        NotId("red vented?", "Vent_Culprit");           // asking, not accusing

        Console.WriteLine("\n== round trip: every variant of the bank, placeholders filled, must come back as its own group");
        int total = 0, strict = 0, idOnly = 0;
        var fails = new List<string>();
        foreach (var g in Pack.Phrases)
        foreach (var v in g.Phrases)
        {
            total++;
            string text = v.Text.Replace("{target}", "James").Replace("{subject}", "Luna").Replace("{witness}", "Mary").Replace("{room}", "Cafeteria");
            NewMeeting(); UnityEngine.Time.time = g.Context == ChatContextEnum.MeetingStart ? 3 : 20;
            var r = P(0, text, false);
            string gid = g.Id == "Whereabouts_Player" ? "Whereabouts_Ask" : g.Id; // same question, roaming vs meeting
            bool idOk = r.Intent != null && r.Intent.PhraseId == gid;
            bool typeOk = idOk && r.Intent.Type == g.Intent;
            // identical text living in several groups cannot be told apart: accept any group that really has this text
            if (!typeOk && r.Intent != null)
            {
                string norm = AMG.AI.Services.ChatService.ChatLexicon.Normalize(text);
                bool same = Pack.Phrases.Any(o => o.Id == r.Intent.PhraseId && o.Intent == r.Intent.Type &&
                    o.Phrases.Any(p => AMG.AI.Services.ChatService.ChatLexicon.Normalize(p.Text.Replace("{target}", "James").Replace("{subject}", "Luna").Replace("{witness}", "Mary").Replace("{room}", "Cafeteria")) == norm));
                if (same) { typeOk = true; idOk = true; }
            }
            bool needsState = r.Rationale.StartsWith("room only") || (g.Id == "Body_Location" && g.Intent == ChatIntentEnum.Confirm);
            if (!typeOk && needsState) { total--; continue; } // answers that only make sense after somebody else spoke
            if (idOk) idOnly++;
            if (typeOk) strict++;
            else fails.Add($"{g.Id}/{g.Intent}: \"{text}\" -> {(r.Intent == null ? "UNKNOWN (" + r.Rationale + ")" : r.Intent.PhraseId + "/" + r.Intent.Type)}");
        }
        foreach (var f in fails.Take(60)) Console.WriteLine("   miss  " + f);
        Console.WriteLine($"round trip: {strict}/{total} exact (id+intent), {idOnly}/{total} id only, {fails.Count} misses");
        Check("round trip >= 90% exact", strict * 100 >= total * 90);

        Console.WriteLine("\n== real messages from the first corpus (UnknownMessages.jsonl)");
        var withColors = new List<ChatPlayer>(Players) { new(7, "Zed", "Banana"), new(8, "Kai", "Gray"), new(9, "Lia", "Purple", "Roxo") };
        ParseResult PC(byte sender, string text, float at = 20) { NewMeeting(); UnityEngine.Time.time = at; return ChatParser.Parse(text, sender, ClaimStore.Context, withColors, Rooms, Pack.Phrases); }
        void Mask2(string txt, string expected) { var m = ChatParser.MaskEntities(txt, withColors, Rooms); Check($"mask '{txt}' -> '{m}'", m == expected); }
        Mask2("nana imp", "{player} imp");                         // "nana" is banana, not a name
        Mask2("not nana or red", "not {player} or {player}");
        Mask2("purp was in cafe rn", "{player} was in {room} rn");
        Mask2("roxo safe", "{player} safe");                       // the colour as a translated game shows it
        Mask2("grey is dead", "{player} is dead");
        Mask2("maroonwya", "maroonwya");                           // maroon is not in this lobby: nothing to mask
        Mask2("redwya", "{player} wya");
        Mask2("ＺＥＤ is sus", "{player} is sus");                  // fullwidth letters fold to plain ones
        { var r = PC(0, "i was on cafeteria"); Check("'i was on cafeteria' is an alibi", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.Cafeteria), r); }
        { var r = PC(0, "i was locked in electrical"); Check("'i was locked in electrical' is an alibi", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.Electrical), r); }
        { var r = PC(0, "i just entered admin"); Check("'i just entered admin' is an alibi", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.Admin), r); }
        { var r = PC(0, "i was in medicine"); Check("'medicine' is medbay", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.MedBay), r); }
        { var r = PC(0, "i was in storages"); Check("'storages' is storage", Is(r, ChatIntentEnum.Defend, "Alibi_Tasks", SystemTypes.Storage), r); }
        { var r = PC(0, "maroonwya".Replace("maroon", "red")); Check("glued 'redwya' asks red", Is(r, ChatIntentEnum.Question, "Whereabouts_Ask", null, 1), r); }
        foreach (var typo in new[] { "wher", "wyere", "whre" })
        { var r = PC(1, typo + "?"); Check($"'{typo}?' is the body question", Is(r, ChatIntentEnum.Question, "Body_Location"), r); }
        { var r = PC(0, "skkip"); Check("'skkip' is skip", r.Intent != null && r.Intent.PhraseId == "Vote_Skip", r); }
        { var r = PC(0, "vot james"); Check("'vot james' is a vote request", r.Intent != null && r.Intent.PhraseId == "Vote_Request" && r.Intent.Target == 1, r); }
        { var r = PC(0, "okey"); Check("'okey' is ok", r.Intent != null && r.Intent.PhraseId == "Ack_Generic", r); }
        foreach (var qw in new[] { "who", "who?", "what", "why", "how" })
        { var r = PC(0, qw); Check($"'{qw}' alone is a reaction, not Kill_Witness", r.Intent == null || r.Intent.PhraseId != "Kill_Witness", r); }
        NewMeeting(); UnityEngine.Time.time = 3;
        ClaimStore.Register(0, new ChatIntent { Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Room = SystemTypes.Security }, true, 1f);
        UnityEngine.Time.time = 46;
        { var r = ChatParser.Parse("in electrical", 1, ClaimStore.Context, withColors, Rooms, Pack.Phrases); Check("'in electrical' 43s later does NOT dispute the body location", r.Intent == null || r.Intent.PhraseId != "Body_Location", r); }
        UnityEngine.Time.time = 12;
        { var r = ChatParser.Parse("electrical", 1, ClaimStore.Context, withColors, Rooms, Pack.Phrases); Check("'electrical' 9s later does dispute it", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical), r); }
        Check("colour ids follow the game's numbering", ChatLexicon.ColorName(14) == "banana" && ChatLexicon.ColorName(15) == "gray" && ChatLexicon.ColorName(12) == "maroon" && ChatLexicon.ColorName(99) == "");

        Console.WriteLine("\n== 'eng': the Engineer role vs 'upper eng' / 'lower eng' (the room)");
        Mask2("saw red kill in upper eng", "saw {player} kill in {room}");
        Mask2("go lower eng and upper eng", "go {room} and {room}");
        Mask2("im eng", "im eng");                                   // the role: nothing to mask
        Mask2("who is eng", "who is eng");
        Mask2("red cd?", "{player} cd?");                            // "blue cd?": asking someone who says they are engineer for their vent cooldown
        Mask2("im eng cd is 5 sec", "im eng cd is 5 sec");
        { var r = PC(0, "upper eng", 3); Check("'upper eng' is a bare room", r.Intent != null && r.Intent.Room == SystemTypes.UpperEngine, r); }
        { var r = PC(0, "eng"); Check("a lone 'eng' is not a room", r.Intent == null || r.Intent.Room == null, r); }

        Console.WriteLine("\n== stale questions (first corpus: a bare room absorbed by a question from long ago)");
        void Chatter(int n) { for (int i = 0; i < n; i++) P((byte)(4 + i % 3), "random chatter number " + i, false); }
        NewMeeting(); UnityEngine.Time.time = 2; P(3, "where"); Chatter(3); UnityEngine.Time.time = 8;
        { var r = P(0, "in electrical", false); Check("3 messages after 'where': still its answer", r.Intent != null && r.Intent.PhraseId == "Body_Location" && r.Intent.ReplyToClaim != null, r); }
        NewMeeting(); UnityEngine.Time.time = 2; P(3, "where"); Chatter(12); UnityEngine.Time.time = 21;
        { var r = P(0, "in electrical", false); Check("12 messages after 'where': NOT an answer to it", r.Intent == null || r.Intent.ReplyToClaim == null, r); }
        NewMeeting(); UnityEngine.Time.time = 2; P(3, "where"); Chatter(12);
        Check("a question 12 messages old is no longer listed as open", !ClaimStore.OpenQuestionPhraseIds().Contains("Body_Location"));
        NewMeeting(); UnityEngine.Time.time = 10; P(0, "white wya?"); Chatter(2); UnityEngine.Time.time = 14;
        { var r = P(2, "nav", false); Check("white answers 'wya' 2 messages later", r.Intent != null && r.Intent.PhraseId == "Alibi_Tasks" && r.Intent.ReplyToClaim != null, r); }
        NewMeeting(); UnityEngine.Time.time = 10; P(0, "white wya?"); Chatter(10); UnityEngine.Time.time = 25;
        { var r = P(2, "nav", false); Check("white's 'nav' 10 messages later is not the answer", r.Intent == null || r.Intent.ReplyToClaim == null, r); }

        Console.WriteLine("\n== 'first room said = the body' only at the very start of the chat");
        NewMeeting(); UnityEngine.Time.time = 3;
        { var r = P(1, "electrical", false); Check("first message of the chat", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical), r); }
        NewMeeting(); UnityEngine.Time.time = 5; Chatter(2);
        { var r = P(1, "electrical", false); Check("third message: still early", Is(r, ChatIntentEnum.Report, "Body_Location", SystemTypes.Electrical), r); }
        NewMeeting(); UnityEngine.Time.time = 14; P(1, "we go james first", false); P(2, "but i saw james last round", false); P(0, "vote james first do luna after", false);
        { var r = P(3, "in electrical", false); Check("after 3 messages about voting it is NOT the body location", r.Intent == null, r); }

        Console.WriteLine("\n== a template that says more than the sentence (first corpus: 'did anyone see X' became Scan_Clear)");
        { var r = PC(0, "did anyone see james"); Check("'did anyone see james' is not the scan question", r.Intent == null || r.Intent.PhraseId != "Scan_Clear", r); }
        { var r = PC(0, "did anyone see james scan"); Check("'did anyone see james scan' is", r.Intent != null && r.Intent.PhraseId == "Scan_Clear" && r.Intent.Target == 1, r); }
        { var r = PC(0, "who saw james scan in medbay?"); Check("'who saw james scan in medbay?' is", r.Intent != null && r.Intent.PhraseId == "Scan_Clear" && r.Intent.Target == 1, r); }

        Console.WriteLine("\n== corpus: safety filter (judges messages, never players)");
        var filter = ChatSafetyFilter.Parse(ChatSafetyFilter.DefaultFile.Split('\n'));
        void Safe(string txt, string expectedCategory, bool expectedDrop)
        {
            var v = filter.Check(txt);
            Check($"'{txt}' -> {(expectedCategory ?? "clean")}{(expectedDrop ? " (dropped)" : "")}", v.Category == expectedCategory && v.Drop == expectedDrop);
        }
        foreach (var clean in new[] { "red vented in cafe", "i was in essex", "assassin", "cocktail", "analysis", "scunthorpe", "class pass grass bass",
                 "dickens", "sus", "o2", "where was it", "vote james", "i saw him s", "x y" })
            Safe(clean, null, false);
        foreach (var bad in new[] { "sex", "SEX!!!", "s3x", "s e x", "s.e.x", "seeeex", "red is sexy", "h0rny", "porn" })
            Safe(bad, "sexual", true);
        Safe("kys", "harassment", true);
        Safe("k y s", "harassment", true);
        Safe("kill yourself red", "harassment", true);
        Safe("wtf shit", "profanity", false);
        Safe("holy shit red vented", "profanity", false);
        Safe("fuck sex", "sexual", true);                 // the worse category wins over a merely flagged one
        Safe("sexo", "sexual", true);
        var weakened = ChatSafetyFilter.Parse(ChatSafetyFilter.DefaultFile.Split('\n').Concat(new[] { "# user emptied everything else" }));
        Check("user file can only add: defaults still apply", weakened.Check("sex").Drop);
        var extended = ChatSafetyFilter.Parse(ChatSafetyFilter.DefaultFile.Split('\n').Concat(new[] { "[hate]", "examplebadword" }));
        Check("user terms are added", extended.Check("you examplebadword").Category == "hate");

        Console.WriteLine("\n== corpus: contact info drops the whole message");
        foreach (var c in new[] { "add me on discord", "https://foo.gg/x", "www.site.com", "foo.com", "mail me john@x.com", "user#1234", "call 11999998888", "@streamer on twitch", "snap me" })
            Check($"'{c}' is contact", ChatRedactor.HasContactInfo(c));
        foreach (var c in new[] { "red vented in cafe at 2", "o2", "sus", "meet at 12:30", "1 2 3", "i did 2 tasks", "where?" })
            Check($"'{c}' is not contact", !ChatRedactor.HasContactInfo(c));

        Console.WriteLine("\n== corpus: names never reach the stored text");
        void Mask(string txt, string expected)
        {
            var m = ChatParser.MaskEntities(txt, Players, Rooms);
            Check($"'{txt}' -> '{m}'", m == expected);
        }
        Mask("james vented in cafe", "{player} vented in {room}");
        Mask("I saw James's body in admin", "i saw {player} body in {room}");
        Mask("where?", "where?");
        Mask("red and blue were in nav", "{player} and {player} were in {room}");
        Mask("cristian is sus", "{player} is sus");
        Mask("luna doe poofed", "{player} poofed");
        Mask("red farted", "{player} farted");
        Mask("elec", "{room}");
        Mask("wait one sec", "wait one sec");
        Mask("does anyone have info?", "does anyone have info?");
        foreach (var name in new[] { "james", "smith", "luna", "cristiano", "mary" })
        {
            var m = ChatParser.MaskEntities($"{name} did something near {name}s task", Players, Rooms);
            Check($"'{name}' is masked", !m.Contains(name));
        }

        Console.WriteLine("\n== corpus: one JSON object per line");
        var msg = new CorpusMessage { Session = "ab12cd", Meeting = 2, At = 4.3f, Speaker = "S1", Text = "{player} poofed", Bucket = "unknown", Reason = "no rule matched",
            Prev = [new CorpusPrev { Speaker = "me", Text = "where?" }], Open = ["Body_Location"] };
        var line = CorpusJson.Line(msg);
        Console.WriteLine("   " + line);
        Check("single line", !line.Contains('\n'));
        Check("camelCase + kind", line.Contains("\"kind\":\"msg\"") && line.Contains("\"speaker\":\"S1\""));
        Check("{player} not escaped", line.Contains("{player} poofed"));
        var filtered = CorpusJson.Line(new CorpusMessage { Session = "ab12cd", Meeting = 2, At = 1f, Speaker = "S2", Bucket = "filtered", Category = "sexual" });
        Check("filtered record has no text field", !filtered.Contains("\"text\""));
        var meetingLine = CorpusJson.Line(new CorpusMeeting { Session = "ab12cd", Meeting = 2, Utc = "2026-10-15T12:00:00Z", Alive = 9,
            Events = [new CorpusEvent { Type = "phantom_vanish", SecBefore = 14.2f }] });
        Check("meeting record carries marked events", meetingLine.Contains("phantom_vanish") && meetingLine.Contains("\"secBefore\":14.2"));
        Check("lines parse back as JSON", System.Text.Json.JsonDocument.Parse(line).RootElement.GetProperty("bucket").GetString() == "unknown");

        Console.WriteLine("\n== corpus: raw text lives in its own local file, joined by id");
        var shareable = CorpusJson.Line(new CorpusMessage { Id = "ab12cd-3-5", Session = "ab12cd", Meeting = 3, At = 2f, Speaker = "S1", Text = "i was with {player}", Bucket = "unknown" });
        var local = CorpusJson.Line(new CorpusRaw { Id = "ab12cd-3-5", Raw = "i was with rose" });
        var roster = CorpusJson.Line(new CorpusRoster { Session = "ab12cd", Meeting = 3, Players = [new CorpusRosterPlayer { Id = 2, Name = "Someone", Color = "rose" }] });
        Console.WriteLine("   " + shareable); Console.WriteLine("   " + local); Console.WriteLine("   " + roster);
        Check("shareable line has the id and no raw field", shareable.Contains("\"id\":\"ab12cd-3-5\"") && !shareable.Contains("\"raw\""));
        Check("shareable line never carries the original text", !shareable.Contains("with rose"));
        Check("local line carries the original under the same id", local.Contains("\"raw\":\"i was with rose\"") && local.Contains("ab12cd-3-5"));
        Check("roster line has names (local file only)", roster.Contains("\"name\":\"Someone\"") && roster.Contains("\"kind\":\"roster\""));

        Console.WriteLine($"\n{pass} passed, {fail} failed");
        Environment.Exit(fail == 0 ? 0 : 1);
    }
}
