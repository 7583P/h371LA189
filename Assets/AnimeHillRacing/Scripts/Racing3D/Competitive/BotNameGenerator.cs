using System;
using System.Collections.Generic;
using System.Text;

namespace AnimeHillRacing.Racing3D
{
    public enum BotNameStyle : byte { Letter, Digits, ShortAlias, CleanWord, ShortTag, GamerHandle }

    // Original, safe, probabilistic league names. Higher hidden skill only
    // shifts style weights; every style is reachable at every skill level, so
    // a name never deterministically reveals skill. Names are generated once,
    // in index order, and then persisted in the league journal.
    public sealed class BotNameGenerator
    {
        public const int GeneratorVersion = 2;
        public const int MaximumLength = 18;

        static readonly string Letters = "ABCDEFGHJKLMNPRSTVXZ";
        static readonly string LowerLetters = "kmrvxznj";
        static readonly string[] Onsets = { "r", "k", "n", "m", "z", "v", "s", "t", "l", "y", "h", "d", "j", "ky", "ry", "sh", "" };
        static readonly string[] Vowels = { "a", "i", "o", "e", "u", "ai", "io", "ei" };
        static readonly string[] Codas = { "", "", "", "n", "x", "r", "k", "s" };
        static readonly string[] CleanWords = {
            "Zen", "Aki", "Nox", "Rai", "Mio", "Kairo", "Vanta", "Ember", "Lumen", "Onyx", "Halo", "Vega", "Orin", "Sora", "Kenji",
            "Rhea", "Tarn", "Juno", "Kova", "Lyra", "Nyx", "Pax", "Rune", "Seren", "Talon", "Vale", "Wren", "Yuki", "Zephyr", "Astra",
            "Brio", "Cielo", "Drav", "Echo", "Fable", "Glint", "Hikari", "Iris", "Jett", "Koa", "Lark", "Mako", "Nori", "Opal", "Quill",
            "Riven", "Sable", "Tempo", "Umbra", "Vireo", "Wisp", "Xeno", "Yara", "Zuri", "Akira", "Basil", "Coda", "Dune", "Elio", "Flint" };
        static readonly string[] GamerPrefix = { "dark", "pro", "super", "cool", "speed", "drift", "turbo", "mega", "ultra", "epic", "shadow",
            "night", "neon", "fast", "real", "the", "big", "lil", "mr", "king", "ice", "fire", "hyper", "nitro", "crazy", "lucky", "silent" };
        static readonly string[] GamerNoun = { "master", "gamer", "racer", "driver", "player", "legend", "king", "boss", "rider", "pilot",
            "wolf", "tiger", "dragon", "ninja", "hunter", "ghost", "storm", "rocket", "car", "kart", "wheel", "drifter", "champ", "star" };
        static readonly string[] GamerSuffix = { "pro", "x", "yt", "gg", "tv", "hd", "", "", "", "", "", "" };
        // Defensive filter for accidental syllable/number combinations. Generated
        // vocabulary is curated; this guards concatenations in EN/ES.
        static readonly string[] Blocked = { "fuck", "fuk", "shit", "cum", "sex", "porn", "nazi", "hitler", "rape", "puta", "puto", "mierd",
            "culo", "pene", "verga", "polla", "coño", "cono", "dick", "cock", "cunt", "bitch", "slut", "whore", "fag", "nigg", "negr", "kkk", "isis",
            "kill", "suicid", "anal", "anus", "tits", "boob", "pussy", "jizz", "marica", "pendej", "chinga", "joto", "perra", "nazi", "heil", "666" };

        readonly HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public int Collisions { get; private set; }

        public void Reserve(string name) { if (!string.IsNullOrEmpty(name)) used.Add(name); }

        public static double[] Weights(double skill)
        {
            double s = Math.Max(0, Math.Min(1, skill));
            return new[] {
                .015 + .11 * s * s * s,          // Letter
                .01 + .06 * s * s * s,           // Digits
                .10 + .34 * s * s,               // ShortAlias
                .08 + .16 * s,                   // CleanWord
                .15,                             // ShortTag
                Math.Max(.10, .64 - .46 * Math.Pow(s, 1.5)) // GamerHandle
            };
        }

        public string Generate(uint seed, double skill, out BotNameStyle style)
        {
            var rng = new LeagueRandom(seed);
            var weights = Weights(skill);
            double total = 0; foreach (var w in weights) total += w;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                double roll = rng.Unit() * total; int chosen = weights.Length - 1;
                for (int i = 0; i < weights.Length; i++) { roll -= weights[i]; if (roll < 0) { chosen = i; break; } }
                // Exhausted tiny namespaces fall through to aliases instead of duplicating.
                if (attempt >= 6 && chosen <= 1) chosen = 2;
                style = (BotNameStyle)chosen;
                string name = Build(style, ref rng);
                if (attempt >= 12) name += (10 + rng.Next(990)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (name.Length == 0 || name.Length > MaximumLength || !Safe(name)) continue;
                if (used.Add(name)) return name;
                Collisions++;
            }
            style = BotNameStyle.GamerHandle;
            string fallback;
            do { fallback = "racer" + rng.NextUInt().ToString("x6").Substring(0, 6); } while (!used.Add(fallback));
            return fallback;
        }

        static string Build(BotNameStyle style, ref LeagueRandom rng)
        {
            switch (style)
            {
                case BotNameStyle.Letter:
                    return rng.Unit() < .7 ? Letters[rng.Next(Letters.Length)].ToString() : LowerLetters[rng.Next(LowerLetters.Length)].ToString();
                case BotNameStyle.Digits:
                {
                    double r = rng.Unit();
                    int value = r < .35 ? 1 + rng.Next(9) : r < .8 ? 10 + rng.Next(90) : 100 + rng.Next(900);
                    return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                case BotNameStyle.ShortAlias:
                {
                    var sb = new StringBuilder(8);
                    if (rng.Unit() < .12) sb.Append(Letters[rng.Next(Letters.Length)]);
                    int syllables = rng.Unit() < .55 ? 1 : 2;
                    for (int i = 0; i < syllables; i++) { sb.Append(Onsets[rng.Next(Onsets.Length)]); sb.Append(Vowels[rng.Next(Vowels.Length)]); }
                    sb.Append(Codas[rng.Next(Codas.Length)]);
                    string alias = sb.ToString();
                    if (alias.Length < 2) alias += Vowels[rng.Next(4)];
                    if (alias.Length > 6) alias = alias.Substring(0, 6);
                    return Case(alias, ref rng);
                }
                case BotNameStyle.CleanWord:
                {
                    string word = CleanWords[rng.Next(CleanWords.Length)];
                    double c = rng.Unit();
                    return c < .55 ? word : c < .9 ? word.ToLowerInvariant() : word.ToUpperInvariant();
                }
                case BotNameStyle.ShortTag:
                {
                    string head = rng.Unit() < .5 ? CleanWords[rng.Next(CleanWords.Length)].ToLowerInvariant() : Build(BotNameStyle.ShortAlias, ref rng).ToLowerInvariant();
                    double t = rng.Unit();
                    string digits = (rng.Unit() < .5 ? rng.Next(10) : rng.Next(100)).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return t < .45 ? head + digits : t < .7 ? head + "_" + digits : t < .85 ? head + "." + LowerLetters[rng.Next(LowerLetters.Length)] : head.ToUpperInvariant() + digits;
                }
                default:
                {
                    var sb = new StringBuilder(18);
                    bool wrap = rng.Unit() < .08;
                    if (wrap) sb.Append("xx");
                    sb.Append(GamerPrefix[rng.Next(GamerPrefix.Length)]);
                    if (rng.Unit() < .35) sb.Append(GamerPrefix[rng.Next(GamerPrefix.Length)]);
                    if (rng.Unit() < .1) sb.Append('_');
                    sb.Append(GamerNoun[rng.Next(GamerNoun.Length)]);
                    sb.Append(GamerSuffix[rng.Next(GamerSuffix.Length)]);
                    double d = rng.Unit();
                    int number = d < .3 ? 10 + rng.Next(90) : d < .65 ? 100 + rng.Next(900) : d < .9 ? 1000 + rng.Next(9000) : 1990 + rng.Next(40);
                    if (rng.Unit() < .88) sb.Append(number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    if (wrap) sb.Append("xx");
                    string handle = sb.ToString();
                    if (rng.Unit() < .12) handle = char.ToUpperInvariant(handle[0]) + handle.Substring(1);
                    return handle.Length > MaximumLength ? handle.Substring(0, MaximumLength) : handle;
                }
            }
        }

        static string Case(string alias, ref LeagueRandom rng)
        {
            double c = rng.Unit();
            if (c < .6) return alias.ToLowerInvariant();
            if (c < .95) return char.ToUpperInvariant(alias[0]) + alias.Substring(1).ToLowerInvariant();
            return alias.ToUpperInvariant();
        }

        public static bool Safe(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var sb = new StringBuilder(name.Length);
            foreach (char raw in name.ToLowerInvariant())
            {
                char ch = raw == '0' ? 'o' : raw == '1' ? 'i' : raw == '3' ? 'e' : raw == '4' ? 'a' : raw == '5' ? 's' : raw == '7' ? 't' : raw == '@' ? 'a' : raw;
                if (char.IsLetter(ch)) sb.Append(ch);
            }
            string folded = sb.ToString(), lower = name.ToLowerInvariant();
            foreach (string blocked in Blocked)
                if (folded.Contains(blocked) || lower.Contains(blocked)) return false;
            return true;
        }

        public static bool IsGamerStyle(string name)
        {
            int digits = 0, letters = 0; foreach (char ch in name) { if (char.IsDigit(ch)) digits++; else if (char.IsLetter(ch)) letters++; }
            return name.Length >= 10 && letters >= 6 && (digits >= 2 || name.StartsWith("xx", StringComparison.Ordinal));
        }
    }
}
