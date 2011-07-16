﻿namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Provides methods to monitor a given process for open file handles.
    /// </summary>
    public static class ProcessMonitor
    {
        /// <summary>
        /// Gets or sets the list of files that have been seen to be open by the specified process.
        /// </summary>
        /// <value>The open files.</value>
        public static List<string> OpenFiles { get; set; }

        /// <summary>
        /// Initializes the <see cref="ProcessMonitor"/> class.
        /// </summary>
        static ProcessMonitor()
        {
            OpenFiles = new List<string>();
        }

        /// <summary>
        /// Gets the list of open file handles for the given processes.
        /// </summary>
        /// <param name="processes">The processes' executable name.</param>
        /// <returns>List of the open files.</returns>
        public static List<FileInfo> GetHandleList(IEnumerable<string> processes)
        {
            var sb = new StringBuilder();

            foreach (var process in processes)
            {
                sb.AppendLine(Utils.RunAndRead(Path.Combine(Signature.FullPath, "handle.exe"), "-accepteula -p " + process, true));
            }

            return Regex.Matches(sb.ToString(), @"(?:D|\-)\)\s+(.+)(?:\r|$)")
                   .Cast<Match>()
                   .Select(m => m.Groups[1].Value.Trim())
                   .Distinct(StringComparer.CurrentCultureIgnoreCase)
                   .Select(f => new FileInfo(f))
                   .ToList();
        }

        /// <summary>
        /// Checks for open files on the specified processes and marks them if recognized.
        /// </summary>
        public static void CheckOpenFiles()
        {
            if (!File.Exists(Path.Combine(Signature.FullPath, "handle.exe")))
            {
                return;
            }

            var procs = new List<string>();
            procs.AddRange(Settings.GetList("Processes to Monitor"));
            procs.AddRange(Utils.GetDefaultVideoPlayers().Select(Path.GetFileName));

            if (procs.Count() == 0)
            {
                return;
            }

            var files = GetHandleList(procs.Distinct(StringComparer.CurrentCultureIgnoreCase));
            var shows = Database.Query("select showid, name, release from tvshows order by rowid asc");

            foreach (var show in shows)
            {
                var parts = string.IsNullOrWhiteSpace(show["release"])
                          ? Parser.GetRoot(show["name"])
                          : show["release"].Split(' ');

                foreach (var file in files)
                {
                    if (Parser.IsMatch(file.DirectoryName + @"\" + file.Name, parts))
                    {
                        var pf = FileNames.Parser.ParseFile(file.Name, file.DirectoryName.Split(Path.DirectorySeparatorChar), false);
                        if (pf.Success && parts.SequenceEqual(Parser.GetRoot(pf.Show))) // or the one extracted from the directory name?
                        {
                            if (!OpenFiles.Contains(file.ToString()))
                            {
                                // add to open files list
                                // 5 minutes later we'll check again, and if it's still open we'll mark it as seen
                                // the reason for this is that an episode will be marked as seen only if you're watching it for more than 10 minutes (5 minute checks twice)

                                OpenFiles.Add(file.ToString());
                            }
                            else
                            {
                                MarkAsSeen(int.Parse(show["showid"]), pf.Episode);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Marks the specified episode as seen.
        /// </summary>
        /// <param name="showid">The ID of the show.</param>
        /// <param name="ep">The episode.</param>
        public static void MarkAsSeen(int showid, ShowEpisode ep)
        {
            var eps = ep.SecondEpisode.HasValue
                      ? Enumerable.Range(ep.Episode, (ep.SecondEpisode.Value - ep.Episode + 1)).ToArray()
                      : new[] { ep.Episode };

            foreach (var epnr in eps)
            {
                var epid = Database.GetEpisodeID(showid, ep.Season, epnr);

                if (epid == int.MinValue)
                {
                    continue;
                }

                if (Database.Query("select * from tracking where showid = ? and episodeid = ?", showid, epid).Count == 0)
                {
                    Database.Execute("insert into tracking values (?, ?)", showid, epid);

                    Database.Trackings.Add(epid);
                    Database.Episodes.First(e => e.EpisodeID == epid).Watched = true;
                }
            }

            if (Synchronization.Status.Enabled)
            {
                Synchronization.Status.Engine.MarkEpisodes(showid.ToString(), eps.Select(x => x + (ep.Season * 1000)).ToList());
            }

            MainWindow.Active.DataChanged();
        }
    }
}
