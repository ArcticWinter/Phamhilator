﻿using System.Collections.Generic;
using System.Linq;



namespace Phamhilator.Analysers
{
	public static class QuestionTitle
	{
		public static bool IsSpam(Question post, ref QuestionAnalysis info)
		{
			var filtersUsed = 0;

			// Loop over blacklist.

			foreach (var blackTerm in GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackSpam].Terms)
			{
				if (blackTerm.Regex.IsMatch(post.Title))
				{
					info.Accuracy += blackTerm.Score;
					info.BlackTermsFound.Add(blackTerm);

					filtersUsed++;
				}
			}

			// Otherwise, if no blacklist terms were found, assume the post is clean.

			if (filtersUsed == 0) { return false; }

			// Loop over whitelist.

			foreach (var whiteTerm in GlobalInfo.WhiteFilters[FilterType.QuestionTitleWhiteSpam].Terms.Where(t => t.Site == post.Site))
			{
				if (whiteTerm.Regex.IsMatch(post.Title))
				{
					info.Accuracy -= whiteTerm.Score;
					info.WhiteTermsFound.Add(whiteTerm);
					info.FiltersUsed.Add(FilterType.QuestionTitleWhiteSpam);
					filtersUsed++;
				}
			}

			info.FiltersUsed.Add(FilterType.QuestionTitleBlackSpam);
			info.Accuracy /= filtersUsed;
			info.Accuracy /= GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackSpam].HighestScore;
			info.Accuracy *= 100;
			info.Type = PostType.Spam;

			return true;
		}

		public static bool IsLowQuality(Question post, ref QuestionAnalysis info)
		{
			var filtersUsed = 0;

			// Loop over blacklist.

			foreach (var blackTerm in GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackLQ].Terms)
			{
				if (blackTerm.Regex.IsMatch(post.Title))
				{
					info.Accuracy += blackTerm.Score;
					info.BlackTermsFound.Add(blackTerm);

					filtersUsed++;
				}
			}

			// Otherwise, if no blacklist terms were found, assume the post is clean.

			if (filtersUsed == 0) { return false; }

			// Loop over whitelist.

			foreach (var whiteTerm in GlobalInfo.WhiteFilters[FilterType.QuestionTitleWhiteLQ].Terms.Where(t => t.Site == post.Site))
			{
				if (whiteTerm.Regex.IsMatch(post.Title))
				{
					info.Accuracy -= whiteTerm.Score;
					info.WhiteTermsFound.Add(whiteTerm);
					info.FiltersUsed.Add(FilterType.QuestionTitleWhiteLQ);
					filtersUsed++;
				}
			}

			info.FiltersUsed.Add(FilterType.QuestionTitleBlackLQ);
			info.Accuracy /= filtersUsed;
			info.Accuracy /= GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackLQ].HighestScore;
			info.Accuracy *= 100;
			info.Type = PostType.LowQuality;

			return true;
		}

		public static bool IsOffensive(Question post, ref QuestionAnalysis info)
		{
			var filtersUsed = 0;

			// Loop over blacklist.

			foreach (var blackTerm in GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackOff].Terms)
			{
				if (blackTerm.Regex.IsMatch(post.Title))
				{
					info.Accuracy += blackTerm.Score;
					info.BlackTermsFound.Add(blackTerm);

					filtersUsed++;
				}
			}

			// Otherwise, if no blacklist terms were found, assume the post is clean.

			if (filtersUsed == 0) { return false; }

			// Loop over whitelist.

			foreach (var whiteTerm in GlobalInfo.WhiteFilters[FilterType.QuestionTitleWhiteOff].Terms.Where(t => t.Site == post.Site))
			{
				if (whiteTerm.Regex.IsMatch(post.Title))
				{
					info.Accuracy -= whiteTerm.Score;
					info.WhiteTermsFound.Add(whiteTerm);
					info.FiltersUsed.Add(FilterType.QuestionTitleWhiteOff);
					filtersUsed++;
				}
			}

			info.FiltersUsed.Add(FilterType.QuestionTitleBlackOff);
			info.Accuracy /= filtersUsed;
			info.Accuracy /= GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackOff].HighestScore;
			info.Accuracy *= 100;
			info.Type = PostType.Offensive;

			return true;
		}

		public static bool IsBadUsername(Question post, ref QuestionAnalysis info)
		{
			var filtersUsed = 0;

			// Loop over blacklist.

			foreach (var blackTerm in GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackName].Terms)
			{
				if (blackTerm.Regex.IsMatch(post.AuthorName))
				{
					info.Accuracy += blackTerm.Score;
					info.BlackTermsFound.Add(blackTerm);

					filtersUsed++;
				}
			}

			// Otherwise, if no blacklist terms were found, assume the post is clean.

			if (filtersUsed == 0) { return false; }

			// Loop over whitelist.

			foreach (var whiteTerm in GlobalInfo.WhiteFilters[FilterType.QuestionTitleWhiteName].Terms.Where(t => t.Site == post.Site))
			{
				if (whiteTerm.Regex.IsMatch(post.AuthorName))
				{
					info.Accuracy -= whiteTerm.Score;
					info.WhiteTermsFound.Add(whiteTerm); 
					info.FiltersUsed.Add(FilterType.QuestionTitleWhiteName);
					filtersUsed++;
				}
			}

			info.FiltersUsed.Add(FilterType.QuestionTitleBlackName);
			info.Accuracy /= filtersUsed;
			info.Accuracy /= GlobalInfo.BlackFilters[FilterType.QuestionTitleBlackName].HighestScore;
			info.Accuracy *= 100;
			info.Type = PostType.BadUsername;

			return true;
		}

		public static Dictionary<string, string> IsBadTagUsed(Question post, ref QuestionAnalysis info)
		{
			var tags = new Dictionary<string, string>();

			if (!BadTagDefinitions.BadTags.Keys.Contains(post.Site)) { return tags; }

			foreach (var tag in post.Tags)
			{
				if (BadTagDefinitions.BadTags[post.Site].ContainsKey(tag.ToLowerInvariant()))
				{
					tags.Add(tag, BadTagDefinitions.BadTags[post.Site][tag]);
				}
			}

			if (tags.Count != 0)
			{
				info.Accuracy = 100;
				info.Type = PostType.BadTagUsed;
			}

			return tags;
		}
	}
}
