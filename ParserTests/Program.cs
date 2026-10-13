using System;
using System.Collections.Generic;
using AMG.AI.Services.ChatService;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;

static class T
{
    static int fail, pass;
    static readonly Dictionary<SystemTypes, string> Rooms = new()
    {
        [SystemTypes.Hallway] = "the hallway", [SystemTypes.Storage] = "Storage", [SystemTypes.Cafeteria] = "Cafeteria",
        [SystemTypes.Reactor] = "Reactor", [SystemTypes.UpperEngine] = "Upper Engine", [SystemTypes.Nav] = "Navigation",
        [SystemTypes.Admin] = "Admin", [SystemTypes.Electrical] = "Electrical", [SystemTypes.LifeSupp] = "O2",
        [SystemTypes.Shields] = "Shields", [SystemTypes.MedBay] = "Medbay", [SystemTypes.Security] = "Security",
        [SystemTypes.Weapons] = "Weapons", [SystemTypes.LowerEngine] = "Lower Engine", [SystemTypes.Comms] = "Comms",
    };
    static readonly List<ChatPlayer> Players =
    [
        new(0, "Guilherme", "Cyan"), new(1, "James Smith", "Red"), new(2, "Luna Doe", "White"),
        new(3, "Impostor Santos", "Blue"), new(4, "Cristiano Silva", "Green"), new(5, "Mary Brown", "Brown"),
        new(6, "John Santos", "Lime"),
    ];

    static ParseResult P(byte sender, string text, bool register = true)
    {
        var r = ChatParser.Parse(text, sender, ClaimStore.Context, Players, Rooms);
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

        Console.WriteLine($"\n{pass} passed, {fail} failed");
        Environment.Exit(fail == 0 ? 0 : 1);
    }
}
