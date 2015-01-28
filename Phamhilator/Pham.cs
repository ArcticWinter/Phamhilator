﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using CsQuery;



namespace Phamhilator
{
    public class Pham
    {
        public Pham()
        {
            Config.Log.EntriesRemovedEvent = items =>
            {
                var vaildEntries = items.Where(i => i.ReportType == PostType.Spam || i.ReportType == PostType.Offensive || i.ReportType == PostType.LowQuality).Take(30);

                var results = new Dictionary<LogItem, bool>();

                foreach (var item in vaildEntries)
                {
                    var data = GetDataFromLog(item);
                    var wasTPd = false;

                    switch (item.ReportType)
                    {
                        case PostType.Spam:
                        {
                            wasTPd = CheckForClass1Report(item, data);
                            break;
                        }

                        case PostType.Offensive:
                        {
                            wasTPd = CheckForClass1Report(item, data);
                            break;
                        }

                        case PostType.LowQuality:
                        {
                            wasTPd = CheckForClass2Report(item, data);
                            break;
                        }
                    }

                    results.Add(item, wasTPd);
                    Thread.Sleep(8000); // 8 secs. Try to avoid getting throttled.
                }

                if (results.Count == 0) { return; }

                Config.PrimaryRoom.PostMessage("Auto-review Results\n\nReports TPd:" + FormatTPdReports(results) + "\n\nReports FPd:" + FormatFPdReports(results));
            };
        }



        public void RegisterFP(Post post, PostAnalysis report)
        {
            Stats.TotalFPCount++;

            var newWhiteTermScore = report.BlackTermsFound.Select(t => t.Score).Max() / 2;

            foreach (var filter in report.FiltersUsed)
            {
                var filterConfig = new FilterConfig(filter.Key, filter.Value);
                var whiteFilterConfig = new FilterConfig(filter.Key, FilterType.White);

                if (filter.Value == FilterType.White)
                {
                    // Increment the score of each found white term.
                    foreach (var whiteTerm in report.WhiteTermsFound.Where(term => term.Site == post.Site))
                    {
                        Config.WhiteFilters[whiteFilterConfig].SetScore(whiteTerm, whiteTerm.Score + 1);
                    }
                }
                else
                {
                    // Add all black terms to white filter (if they don't already exist).
                    foreach (var blackTerm in report.BlackTermsFound)
                    {
                        blackTerm.FPCount++;

                        if (Config.WhiteFilters[whiteFilterConfig].Terms.All(term => term.Site != blackTerm.Site && term.Regex.ToString() != blackTerm.Regex.ToString()))
                        {
                            Config.WhiteFilters[whiteFilterConfig].AddTerm(new Term(filterConfig, blackTerm.Regex, newWhiteTermScore, post.Site));
                        }
                    }
                }
            }
        }

        public void RegisterTP(Post post, PostAnalysis report)
        {
            Stats.TotalTPCount++;

            foreach (var filter in report.FiltersUsed.Where(filter => filter.Value == FilterType.Black))
            {
                var filterConfig = new FilterConfig(filter.Key, filter.Value);
                var whiteFilter = new FilterConfig(filter.Key, FilterType.White);

                // Increment the score of each found black term.
                for (var i = 0; i < report.BlackTermsFound.Count; i++)
                {
                    var blackTerm = Config.BlackFilters[filterConfig].Terms.ElementAt(i);
                    Config.BlackFilters[filterConfig].SetScore(blackTerm, blackTerm.Score + 1);
                    blackTerm.TPCount++;

                    // Increase score of each white term (matching the found black term) not found on this site.
                    for (var ii = 0; i < Config.WhiteFilters[whiteFilter].Terms.Count; ii++)
                    {
                        var whiteTerm = Config.WhiteFilters[whiteFilter].Terms.ElementAt(i);

                        if (whiteTerm.Regex.ToString() != blackTerm.Regex.ToString() || whiteTerm.Site == post.Site) { continue; }

                        var x = whiteTerm.Score / blackTerm.Score;

                        Config.WhiteFilters[whiteFilter].SetScore(whiteTerm, x * (blackTerm.Score + 1));
                    }
                }
            }
        }



        private PostAnalysis GetDataFromLog(LogItem entry)
        {
            var data = new AnswerAnalysis();

            foreach (var term in entry.BlackTerms)
            {
                data.BlackTermsFound.Add(term.ToTerm(new FilterConfig(term.Type, FilterType.Black)));

                if (!data.FiltersUsed.ContainsKey(term.Type))
                {
                    data.FiltersUsed.Add(term.Type, FilterType.Black);
                }
            }

            foreach (var term in entry.WhiteTerms)
            {
                data.WhiteTermsFound.Add(term.ToTerm(new FilterConfig(term.Type, FilterType.White)));

                if (!data.FiltersUsed.ContainsKey(term.Type))
                {
                    data.FiltersUsed.Add(term.Type, FilterType.White);
                }
            }

            return data;
        }

        private bool IsAnswerClass1(string postUrl)
        {
            try
            {
                var host = PostFetcher.HostParser.Replace(postUrl, "");
                var id = PostFetcher.PostIDParser.Replace(postUrl, "");

                new WebClient().DownloadString("http://" + host + "/posts/ajax-load-realtime/" + id);
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    // The post has been deleted.
                    return true;
                }
            }

            return false;
        }

        private bool IsQuestionClass1(string postUrl)
        {
            try
            {
                new WebClient().DownloadString(postUrl);
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    // The post has been deleted.
                    return true;
                }
            } 

            return false;
        }

        private bool IsAnswerClass2(string postUrl)
        {
            var html = "";

            try
            {
                var host = PostFetcher.HostParser.Replace(postUrl, "");
                var id = PostFetcher.PostIDParser.Replace(postUrl, "");

                html = new WebClient().DownloadString("http://" + host + "/posts/ajax-load-realtime/" + id);
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    // The post has been deleted.
                    return true;
                }
            }

            var dom = CQ.Create(html);

            var score = dom[".vote-count-post"].Html();

            return int.Parse(score) <= -5;
        }

        private bool IsQuestionClass2(string postUrl)
        {
            var html = "";

            try
            {
                html = new WebClient().DownloadString(postUrl);
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    // The post has been deleted.
                    return true;

                    // Maybe (later) add a "TP factor" (the effects of the TP will be multiplied by this factor). 
                    // If post was deleted due to moderation, TPF = 3. If post was deleted by owner, TPF = 2. If post is closed + score < -1, TPF = 1/

                    //using (var stream = ex.Response.GetResponseStream())
                    //using (var reader = new StreamReader(stream, Encoding.UTF8))
                    //{
                    //    html = reader.ReadToEnd();
                    //}
                }
            }

            var dom = CQ.Create(html);

            //var deleteionReason = dom[".revision-comment"].Html();

            // Check if the question is deleted (and if so, by who).
            //if (string.IsNullOrEmpty(deleteionReason) || !deleteionReason.Contains("moderation"))
            //{
            //    
            //}

            var isClosed = false;
            var score = dom[".vote-count-post"].Html();

            foreach (var e in dom[".question-status b"])
            {
                if (e.InnerHTML == "closed" || e.InnerHTML == "put on hold")
                {
                    isClosed = true;
                }
            }

            return isClosed || int.Parse(score) <= -3;
        }

        private bool CheckForClass1Report(LogItem item, PostAnalysis data)
        {
            if (item.PostUrl.Contains(@"/questions/"))
            {
                if (IsQuestionClass1(item.PostUrl))
                {
                    RegisterTP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

                    return true;
                }

                RegisterFP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

                return false;
            }

            if (IsAnswerClass1(item.PostUrl))
            {
                RegisterTP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

                return true;
            }

            RegisterFP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

            return false;
        }

        private bool CheckForClass2Report(LogItem item, PostAnalysis data)
        {
            if (item.PostUrl.Contains(@"/questions/"))
            {
                if (IsQuestionClass2(item.PostUrl))
                {
                    RegisterTP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

                    return true;
                }

                RegisterFP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

                return false;
            }

            if (IsAnswerClass2(item.PostUrl))
            {
                RegisterTP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

                return true;
            }

            RegisterFP(new Answer("", "", "", item.Site, 0, "", "", 0), data);

            return false;
        }

        private string FormatTPdReports(Dictionary<LogItem, bool> results)
        {
            var message = new StringBuilder();

            foreach (var result in results.Where(r => r.Value))
            {
                message.Append("\n" + result.Key.ReportLink);
            }

            return message.ToString();
        }

        private string FormatFPdReports(Dictionary<LogItem, bool> results)
        {
            var message = new StringBuilder();

            foreach (var result in results.Where(r => !r.Value))
            {
                message.Append("\n" + result.Key.ReportLink);
            }

            return message.ToString();
        }
    }
}
