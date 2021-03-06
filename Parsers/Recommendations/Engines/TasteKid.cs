﻿namespace RoliSoft.TVShowTracker.Parsers.Recommendations.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Provides support for TasteKid's TV show recommendation service.
    /// </summary>
    [TestFixture]
    public class TasteKid : RecommendationEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TasteKid";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://www.tastekid.com/";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-07-12 2:46 PM");
            }
        }

        /// <summary>
        /// Gets the list of recommended TV show from the engine.
        /// </summary>
        /// <param name="shows">The currently watched shows.</param>
        /// <returns>Recommended shows list.</returns>
        public override IEnumerable<RecommendedShow> GetList(IEnumerable<string> shows)
        {
            var kid = Utils.GetXML("http://www.tastekid.com/ask/ws?verbose=1&q=" + shows.Aggregate(String.Empty, (current, r) => current + (Utils.EncodeURL(Parser.CleanTitleWithEp(r).Replace(",", String.Empty)) + ",")).TrimEnd(','));

            return kid.Descendants("results").Descendants("resource")
                   .Where(item => !shows.Contains(item.Descendants("name").First().Value, new ShowEqualityComparer()))
                   .Select(item => new RecommendedShow
                   {
                       Name      = item.Descendants("name").First().Value,
                       Wikipedia = item.Descendants("wUrl").First().Value,
                       Epguides  = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Utils.EncodeURL(item.Descendants("name").First().Value + " intitle:\"Titles & Air Dates Guide\" site:epguides.com"),
                       Imdb      = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Utils.EncodeURL(item.Descendants("name").First().Value + " intitle:\"TV Series\" site:imdb.com"),
                       Official  = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Utils.EncodeURL(item.Descendants("name").First().Value + " official site"),
                       TVRage    = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Utils.EncodeURL(item.Descendants("name").First().Value + " intitle:\"TV Show\" site:tvrage.com"),
                       TVDB      = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Utils.EncodeURL(item.Descendants("name").First().Value + " intitle:\"Series Info\" site:thetvdb.com"),
                       TVcom     = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Utils.EncodeURL(item.Descendants("name").First().Value + " intitle:\"on TV.com\" inurl:summary.html site:tv.com"),
                       TVTropes  = "http://www.google.com/search?btnI=I'm+Feeling+Lucky&hl=en&q=" + Utils.EncodeURL(item.Descendants("name").First().Value + " site:tvtropes.org")
                   });
        }
    }
}
