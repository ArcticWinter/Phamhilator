﻿/*
 * Phamhilator. A .Net based bot network catching spam/low quality posts for Stack Exchange.
 * Copyright © 2015, ArcticEcho.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */





using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Phamhilator.Pham.UI
{
    public class ModelGenerator
    {
        private const RegexOptions regOpts = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        private readonly Regex inlineCode = new Regex("(?is)<code>.*?</code>", regOpts);
        private readonly Regex blockQuote = new Regex("(?is)<blockquote.*?></blockquote>", regOpts);
        private readonly Regex link = new Regex("(?is)<a.*?</a>", regOpts);
        private readonly Regex pic = new Regex("(?is)<img.*?>", regOpts);
        private readonly Regex htmlTags = new Regex("(?is)<.*?>", regOpts);
        private readonly Regex modelTags = new Regex(@"\•[A-Z-]*?\•", regOpts);
        private string[] stopwords;

        internal static readonly Regex CodeBlock = new Regex("(?is)<pre.*?><code>.*?</code></pre>", regOpts);



        public ModelGenerator()
        {
            stopwords = Properties.Resources.SOStopwordCorpus.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }



        public string[] GenerateModel(string body)
        {
            var prepared = PrepareBody(body);
            var words = prepared.Split(new[]
            {
                '.', ',', ':', ';', '(', ')', '{', '}', '[', ']', '?', '!', '/', '\\', ' ', '\n'
            }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < words.Length; i++)
            {
                if (modelTags.IsMatch(words[i])) { continue; }
                if (words[i].All(char.IsDigit))
                {
                    words[i] = "";
                }
                else
                {
                    words[i] = new string(words[i].Where(char.IsLetterOrDigit).ToArray());
                }
            }

            words = words.Where(w => !string.IsNullOrWhiteSpace(w) && (w.Length > 1 || w == "i" || w == "a")).ToArray();
            words = KeepStopwords(words, 400);

          return words;
        }



        private string PrepareBody(string body)
        {
            var clean = body.ToLowerInvariant();
            clean = TagChunks(clean);
            clean = htmlTags.Replace(clean, " ");
            clean = ExpandContractions(clean);

            return clean.Trim();
        }

        private string TagChunks(string body)
        {
            var tagged = TagCodeBlocks(body);
            tagged = TagInlineCode(tagged);
            tagged = TagBlockQuotes(tagged);
            tagged = TagPictures(tagged);
            tagged = TagLinks(tagged);

            return tagged;
        }

        private string TagCodeBlocks(string body)
        {
            var tagged = body;
            var m = CodeBlock.Match(tagged);

            while (m.Success)
            {
                var code = tagged.Substring(m.Index, m.Length);
                var lines = code.Split('\n');

                tagged = tagged.Remove(m.Index, m.Length);

                if (lines.Length < 4)
                {
                    tagged = tagged.Insert(m.Index, " •CB-S• ");
                }
                else if (lines.Length < 26)
                {
                    tagged = tagged.Insert(m.Index, " •CB-M• ");
                }
                else
                {
                    tagged = tagged.Insert(m.Index, " •CB-L• ");
                }

                m = CodeBlock.Match(tagged);
            }

            return tagged;
        }

        private string TagInlineCode(string body)
        {
            var tagged = body;
            var m = inlineCode.Matches(tagged);
            var matches = new List<Match>();

            foreach (Match match in m)
            {
                if (matches.Count == 0 || match.Index < matches[0].Index)
                {
                    matches.Add(match);
                }
                else
                {
                    matches.Insert(0, match);
                }
            }

            foreach (var match in matches)
            {
                var code = tagged.Substring(match.Index, match.Length);

                tagged = tagged.Remove(match.Index, match.Length);

                if (code.Length < 6)
                {
                    tagged = tagged.Insert(match.Index, " •IC-S• ");
                }
                else if (code.Length < 26)
                {
                    tagged = tagged.Insert(match.Index, " •IC-M• ");
                }
                else
                {
                    tagged = tagged.Insert(match.Index, " •IC-L• ");
                }
            }

            return tagged;
        }

        private string TagBlockQuotes(string body)
        {
            var tagged = body;
            var m = blockQuote.Match(tagged);

            while (m.Success)
            {
                var quote = tagged.Substring(m.Index, m.Length);
                var lines = quote.Split('\n');

                tagged = tagged.Remove(m.Index, m.Length);

                if (lines.Length < 4)
                {
                    tagged = tagged.Insert(m.Index, " •BQ-S• ");
                }
                else if (lines.Length < 11)
                {
                    tagged = tagged.Insert(m.Index, " •BQ-M• ");
                }
                else
                {
                    tagged = tagged.Insert(m.Index, " •BQ-L• ");
                }

                m = blockQuote.Match(tagged);
            }

            return tagged;
        }

        private string TagLinks(string body)
        {
            var tagged = body;
            var m = link.Match(tagged);

            while (m.Success)
            {
                tagged = tagged.Remove(m.Index, m.Length);
                tagged = tagged.Insert(m.Index, " •L• ");

                m = link.Match(tagged);
            }

            return tagged;
        }

        private string TagPictures(string body)
        {
            var tagged = body;
            var m = pic.Match(tagged);

            while (m.Success)
            {
                tagged = tagged.Remove(m.Index, m.Length);
                tagged = tagged.Insert(m.Index, " •P• ");

                m = pic.Match(tagged);
            }

            return tagged;
        }

        private string ExpandContractions(string text)
        {
            var expanded = text;

            expanded = expanded.Replace("isn't", "is not");
            expanded = expanded.Replace("aren't", "are not");
            expanded = expanded.Replace("wasn't", "was not");
            expanded = expanded.Replace("weren't", "were not");
            expanded = expanded.Replace("haven't", "have not");
            expanded = expanded.Replace("hasn't", "has not");
            expanded = expanded.Replace("hadn't", "hadn't");
            expanded = expanded.Replace("won't", "will not");
            expanded = expanded.Replace("wouldn't", "would not");
            expanded = expanded.Replace("don't", "do not");
            expanded = expanded.Replace("doesn't", "does not");
            expanded = expanded.Replace("didn't", "did not");
            expanded = expanded.Replace("can't", "can not");
            expanded = expanded.Replace("couldn't", "could not");
            expanded = expanded.Replace("shouldn't", "should not");
            expanded = expanded.Replace("mightn't", "might not");
            expanded = expanded.Replace("mustn't", "must not");
            expanded = expanded.Replace("shan't", "shall not");

            expanded = expanded.Replace("'ll", " will");
            expanded = expanded.Replace("'ve", " have");
            expanded = expanded.Replace("'m", " am");
            expanded = expanded.Replace("'d", " would");
            expanded = expanded.Replace("'re", " are");
            expanded = expanded.Replace("n't", " not");

            // I'm not even going to attempt to parse "'s" contractions.
            // So for now, let's just remove them.
            expanded = expanded.Replace("'s", "");

            return expanded;
        }

        private string[] KeepStopwords(string[] words, int stopwordCount)
        {
            var stopwords = new List<string>();

            foreach (var w in words)
            {
                if (IsStopword(w, stopwordCount) || modelTags.IsMatch(w) || w == "i" || w == "a")
                {
                    stopwords.Add(w);
                }
            }

            return stopwords.ToArray();
        }

        private bool IsStopword(string word, int stopwordCount)
        {
            for (var i = 0; i < stopwordCount; i++)
            {
                if (word == stopwords[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
