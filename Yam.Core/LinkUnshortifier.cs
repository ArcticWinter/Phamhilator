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





using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System;

namespace Phamhilator.Yam.Core
{
    public static class LinkUnshortifier
    {
        private static readonly Regex shortLink = new Regex(@"(?is)^https?://(goo\.gl|bit\.ly|tinyurl\.com|ow\.ly|tiny\.cc|bit\.do|po\.st|bigly\.us|t\.co|r\.im|cli\.gs|short\.ie|kl\.am|idek\.net|i\.gd|hex\.io)/\w*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);



        public static bool IsShortLink(string url)
        {
            return !String.IsNullOrEmpty(url) && shortLink.IsMatch(url.Trim());
        }

        public static string UnshortifyLink(string url)
        {
            if (!IsShortLink(url)) { return url; }

            var trimmed = url.Trim();
            var res = new WebClient().DownloadString("http://urlex.org/json/" + trimmed);
            var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(res);

            return data.Values.First();
        }
    }
}