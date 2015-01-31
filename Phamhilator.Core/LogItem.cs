﻿using System;
using System.Collections.Generic;



namespace Phamhilator.Core
{
    public class LogItem
    {
        public string ReportLink { get; set; }
        public string PostUrl { get; set; }
        public string Site { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime TimeStamp { get; set; }
        public PostType ReportType { get; set; }
        public List<LogTerm> BlackTerms { get; set; }
        public List<LogTerm> WhiteTerms { get; set; }
    }
}