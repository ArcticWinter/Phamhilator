﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;



namespace Phamhilator
{
    public static class Stats
    {
        private static readonly HashSet<Spammer> spammers = new HashSet<Spammer>();

        public static DateTime UpTime { get; internal set; }

        public static List<Report> PostedReports { get; internal set; }

        public static int PostsCaught { get; internal set; }

        public static float TotalCheckedPosts
        {
            get
            {
                return int.Parse(File.ReadAllText(DirectoryTools.GetTotalCheckedPostsFile()), CultureInfo.InvariantCulture);
            }

            internal set
            {
                File.WriteAllText(DirectoryTools.GetTotalCheckedPostsFile(), value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public static float TotalTPCount
        {
            get
            {
                return int.Parse(File.ReadAllText(DirectoryTools.GetTotalTPCountFile()), CultureInfo.InvariantCulture);
            }

            internal set
            {
                File.WriteAllText(DirectoryTools.GetTotalTPCountFile(), value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public static float TotalFPCount
        {
            get
            {
                return int.Parse(File.ReadAllText(DirectoryTools.GetTotalFPCountFile()), CultureInfo.InvariantCulture);
            }

            internal set
            {
                File.WriteAllText(DirectoryTools.GetTotalFPCountFile(), value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public static int TermCount
        {
            get
            {
                var termCount = 0;

                foreach (var filter in Config.BlackFilters.Values)
                {
                    termCount += filter.Terms.Count;
                }

                foreach (var filter in Config.WhiteFilters.Values)
                {
                    termCount += filter.Terms.Count;
                }

                return termCount + Config.BadTags.Tags.Count;
            }
        }

        public static HashSet<Spammer> Spammers
        {
            get
            {
                return spammers;
            }
        }
    }
}
