﻿namespace RoliSoft.TVShowTracker.FileNames
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Parsers.Downloads;

    using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

    /// <summary>
    /// Provides unit tests for the file name matching methods.
    /// </summary>
    [TestFixture]
    public class ParsingTests
    {
        /// <summary>
        /// Tests several different scene release names.
        /// </summary>
        [Test]
        public void SceneReleaseNames()
        {
            var files = new Dictionary<string, string>
                {
                    {
                        "House.S07E13.720p.HDTV.X264-DIMENSION",
                        "House, M.D. S07E13 - Two Stories"
                    },
                    {
                        "V.2009.S02E07.720p.HDTV.X264-DIMENSION",
                        "V (2009) S02E07 - Birth Pangs"
                    },
                    {
                        "aaf-tosh.s03e07.720p.mkv",
                        "Tosh.0 S03E07 - Hurdle Fail Girls"
                    },
                    {
                        "nova.sciencenow.s05e06.720p.hdtv.x264-orenji.mkv",
                        "NOVA scienceNOW S05E06 - What’s the Next Big Thing?"
                    },
                    {
                        "top_gear.16x01.real.720p_hdtv_x264-fov.mkv",
                        "Top Gear S16E01 - Yeti Road Test"
                    }
                };

            foreach (var file in files)
            {
                var fi   = Parser.ParseFile(file.Key);
                var info = "{0} S{1:00}E{2:00} - {3}".FormatWith(fi.Show, fi.Season, fi.Episode, fi.Title);

                Console.WriteLine(fi.Name);
                Console.WriteLine(info + Environment.NewLine);

                Assert.AreEqual(file.Value, info);
            }
        }

        /// <summary>
        /// Tests if the quality descriptions match themselves.
        /// </summary>
        [Test]
        public void Qualities()
        {
            foreach (var quality in Enum.GetValues(typeof(Qualities)).Cast<Qualities>().Reverse())
            {
                var descr = quality.GetAttribute<DescriptionAttribute>().Description;
                var parse = Parser.ParseQuality(descr);

                Console.WriteLine(descr);
                Assert.AreEqual(quality, parse);
            }
        }
    }
}
