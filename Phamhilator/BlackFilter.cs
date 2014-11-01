﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;



namespace Phamhilator
{
	public class BlackFilter
	{
		public HashSet<Term> Terms { get; private set; }

		public float AverageScore
		{
			get
			{
				var tt = Terms.Count == 0 ? 10 : Terms.Select(t => t.Score).Average();

				return tt;
			}
		}

		public float HighestScore
		{
			get
			{
				return Terms.Count == 0 ? 10 : Terms.Select(t => t.Score).Max();
			}
		}

		public FilterType FilterType { get; private set; }



		public BlackFilter(FilterType filter)
		{
			if ((int)filter > 99) { throw new ArgumentException("Must be a black filter.", "filter"); }

			FilterType = filter;
			Terms = new HashSet<Term>();
			var data = File.ReadAllLines(DirectoryTools.GetFilterFile(filter));

			foreach (var termAndScore in data)
			{
				if (termAndScore.IndexOf("]", StringComparison.Ordinal) == -1) { continue; }

				var scoreAuto = termAndScore.Substring(0, termAndScore.IndexOf("]", StringComparison.Ordinal));

				var termScore = float.Parse(new String(scoreAuto.Where(c => Char.IsDigit(c) || c == '.' || c == ',').ToArray()), CultureInfo.InvariantCulture);
				var termIsAuto = scoreAuto[0] == 'A';
				var termRegex = new Regex(termAndScore.Substring(termAndScore.IndexOf("]", StringComparison.Ordinal) + 1), RegexOptions.Compiled);

				if (Terms.Contains(termRegex) || String.IsNullOrEmpty(termRegex.ToString())) { continue; }

				Terms.Add(new Term(termRegex, termScore, "", termIsAuto));
			}
		}



		public void AddTerm(Term term)
		{
			if (Terms.Contains(term.Regex)) { return; } // Gasp! Silent failure!

			Terms.WriteTerm(FilterType, new Regex(""), term.Regex, "", term.Score);
		}

		public void RemoveTerm(Regex term)
		{
			if (!Terms.Contains(term)) { return; }

			Terms.WriteTerm(FilterType, term, new Regex(""));
		}

		public void EditTerm(Regex oldTerm, Regex newTerm)
		{
			if (!Terms.Contains(oldTerm)) { return; }

			Terms.WriteTerm(FilterType, oldTerm, newTerm);
		}

		public void SetScore(Term term, float newScore)
		{
			if (!Terms.Contains(term.Regex)) { return; }

			Terms.WriteScore(FilterType, term.Regex, newScore);
		}

		public void SetAuto(Regex term, bool isAuto, bool persistence = false)
		{
			if (!Terms.Contains(term)) { return; }

			if (persistence)
			{
				Terms.WriteAuto(FilterType, term, isAuto);
			}
			else
			{
				var t = Terms.GetRealTerm(term);

				Terms.Remove(t);

				Terms.Add(new Term(t.Regex, t.Score, t.Site, isAuto));
			}
		}
	}
}
