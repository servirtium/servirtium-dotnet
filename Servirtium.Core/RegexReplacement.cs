using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Servirtium.Core
{

    [Flags]
    public enum ReplacementContext
    {
        None = 0,
        RequestHeader = 1,
        RequestBody = 2,
        ResponseHeader = 4,
        ResponseBody = 8
    }

    public readonly struct RegexReplacement
    {
        public RegexReplacement(Regex matcher, string replacement, ReplacementContext context = (ReplacementContext.RequestBody | ReplacementContext.RequestHeader | ReplacementContext.ResponseHeader | ReplacementContext.ResponseBody))
        {
            Matcher = matcher;
            Replacement = replacement;
            Context = context;
        }

        public Regex Matcher { get; }
        public string Replacement { get; }
        public ReplacementContext Context { get; }


        public static string FixStringForRecording(string input, IEnumerable<RegexReplacement> regexReplacements, ReplacementContext context)
        {
            var output = input;
            foreach (var regexReplacement in regexReplacements.Where(rr=>(rr.Context & context) == context))
            {
                output = regexReplacement.Matcher.Replace(output, regexReplacement.Replacement);
            }
            return output;
        }

        public static (string, string) FixHeaderForRecording((string, string) input, IEnumerable<RegexReplacement> regexReplacements, ReplacementContext context)
        {
            (string name, string val) = input;
            var fixedHeaderText = FixStringForRecording($"{name}: {val}", regexReplacements, context);
            var headerBits = fixedHeaderText.Split(": ", 2);
            return (headerBits[0], headerBits[1]);
        }
    }
}
