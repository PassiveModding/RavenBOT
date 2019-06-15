using System.Collections.Generic;

namespace RavenBOT.Modules.Translation.Models
{
    public class LanguageMap
    {
        public enum LanguageCode
        {
            _is,
            af,
            am,
            ar,
            az,
            be,
            bg,
            bn,
            bs,
            ca,
            ceb,
            co,
            cs,
            cy,
            da,
            de,
            el,
            en,
            eo,
            es,
            et,
            eu,
            fa,
            fi,
            fr,
            fy,
            ga,
            gd,
            gl,
            gu,
            ha,
            haw,
            hi,
            hmn,
            hr,
            ht,
            hu,
            hy,
            id,
            ig,
            it,
            iw,
            ja,
            jw,
            ka,
            kk,
            km,
            kn,
            ko,
            ku,
            ky,
            la,
            lb,
            lo,
            lt,
            lv,
            mg,
            mi,
            mk,
            ml,
            mn,
            mr,
            ms,
            mt,
            my,
            ne,
            nl,
            no,
            ny,
            pa,
            pl,
            ps,
            pt,
            ro,
            ru,
            sd,
            si,
            sk,
            sl,
            sm,
            sn,
            so,
            sq,
            sr,
            st,
            su,
            sv,
            sw,
            ta,
            te,
            tg,
            th,
            tl,
            tr,
            uk,
            ur,
            uz,
            vi,
            xh,
            yi,
            yo,
            zh_CN,
            zh_TW,
            zu
        }

        /// <summary>
        ///     Gets or sets the default map.
        /// </summary>
        public static List<TranslationSet> DefaultMap { get; set; } =
            new List<TranslationSet>
            {
                new TranslationSet { EmoteMatches = new List<string> { "🇦🇺", "🇺🇸", "🇪🇺", "🇳🇿" }, Language = LanguageCode.en },
                new TranslationSet { EmoteMatches = new List<string> { "🇭🇺" }, Language = LanguageCode.hu },
                new TranslationSet { EmoteMatches = new List<string> { "🇫🇷" }, Language = LanguageCode.fr },
                new TranslationSet { EmoteMatches = new List<string> { "🇫🇮" }, Language = LanguageCode.fi },
                new TranslationSet { EmoteMatches = new List<string> { "🇲🇽", "🇪🇸", "🇨🇴", "🇦🇷" }, Language = LanguageCode.es },
                new TranslationSet { EmoteMatches = new List<string> { "🇧🇷", "🇵🇹", "🇲🇿", "🇦🇴" }, Language = LanguageCode.pt },
                new TranslationSet { EmoteMatches = new List<string> { "🇩🇪", "🇦🇹", "🇨🇭", "🇧🇪", "🇱🇺", "🇱🇮" }, Language = LanguageCode.de },
                new TranslationSet { EmoteMatches = new List<string> { "🇮🇹", "🇨🇭", "🇸🇲", "🇻🇦" }, Language = LanguageCode.it },
                new TranslationSet { EmoteMatches = new List<string> { "🇨🇳", "🇸🇬", "🇹🇼" }, Language = LanguageCode.zh_CN },
                new TranslationSet { EmoteMatches = new List<string> { "🇯🇵" }, Language = LanguageCode.ja }
            };

        public class TranslationSet
        {
            public List<string> EmoteMatches { get; set; } = new List<string>();
            public LanguageCode Language { get; set; }
        }
    }
}