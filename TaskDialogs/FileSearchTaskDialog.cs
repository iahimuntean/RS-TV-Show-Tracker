﻿namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;

    using Microsoft.WindowsAPICodePack.Taskbar;

    using RoliSoft.TVShowTracker.FileNames;

    using VistaControls.TaskDialog;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>FileSearch</c> class.
    /// </summary>
    public class FileSearchTaskDialog
    {
        private TaskDialog _td;
        private Result _res;
        private FileSearch _fs;
        private string _show, _episode;
        private volatile bool _active;

        /// <summary>
        /// Searches for the specified show and its episode.
        /// </summary>
        /// <param name="show">The show.</param>
        /// <param name="episode">The episode.</param>
        public void Search(string show, string episode)
        {
            _show    = show;
            _episode = episode;

            var path = Settings.Get("Download Path");

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                new TaskDialog
                    {
                        CommonIcon  = TaskDialogIcon.Stop,
                        Title       = "Search path not configured",
                        Instruction = "Search path not configured",
                        Content     = "To use this feature you must set your download path." + Environment.NewLine + Environment.NewLine + "To do so, click on the logo on the upper left corner of the application, then select 'Configure Software'. On the new window click the 'Browse' button under 'Download Path'."
                    }.Show();
                return;
            }

            _td = new TaskDialog
                {
                    Title           = "Searching...",
                    Instruction     = show + " " + episode,
                    Content         = "Searching for the episode...",
                    CommonButtons   = TaskDialogButton.Cancel,
                    ShowProgressBar = true
                };

            _td.SetMarqueeProgressBar(true);
            _td.Destroyed   += TaskDialogDestroyed;
            _td.ButtonClick += TaskDialogDestroyed;

            _active = true;
            new Thread(() =>
                {
                    Thread.Sleep(500);

                    if (_active)
                    {
                        _res = _td.Show().CommonButton;
                    }
                }).Start();
            
            _fs = new FileSearch(path, show, episode);

            _fs.FileSearchDone += FileSearchDone;
            _fs.BeginSearch();

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the Destroyed event of the _td control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void TaskDialogDestroyed(object sender, EventArgs e)
        {
            if (_active && (_res == Result.Cancel || (e is ClickEventArgs && (e as ClickEventArgs).ButtonID == 2)))
            {
                Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

                _active = false;
                _res    = Result.Cancel;

                _fs.CancelSearch();
            }
        }

        /// <summary>
        /// Event handler for <c>FileSearch.FileSearchDone</c>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void FileSearchDone(object sender, EventArgs e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (_td != null && _td.IsShowing)
            {
                _td.SimulateButtonClick(-1);
            }

            if (_res == Result.Cancel)
            {
                return;
            }

            switch (_fs.Files.Count)
            {
                case 0:
                    new TaskDialog
                        {
                            CommonIcon    = TaskDialogIcon.Stop,
                            Title         = "No files found",
                            Instruction   = _show + " " + _episode,
                            Content       = "No files were found for this episode.",
                            CommonButtons = TaskDialogButton.OK
                        }.Show();
                    break;

                case 1:
                    Utils.Run(_fs.Files[0]);
                    break;

                default:
                    var mfftd = new TaskDialog
                        {
                            Title           = "Multiple files found",
                            Instruction     = _show + " " + _episode,
                            Content         = "Multiple files were found for this episode:",
                            CommonButtons   = TaskDialogButton.Cancel,
                            CustomButtons   = new CustomButton[_fs.Files.Count],
                            UseCommandLinks = true
                        };

                    mfftd.ButtonClick += (s, c) =>
                        {
                            if(c.ButtonID < _fs.Files.Count)
                            {
                                Utils.Run(_fs.Files[c.ButtonID]);
                            }
                        };

                    var i = 0;
                    foreach (var file in _fs.Files)
                    {
                        var fi      = new FileInfo(file);
                        var quality = Parser.ParseQuality(file);
                        var instr   = fi.Name + "\n";

                        if (quality != Parsers.Downloads.Qualities.Unknown)
                        {
                            instr += quality.GetAttribute<DescriptionAttribute>().Description + "   –   ";
                        }

                        mfftd.CustomButtons[i] = new CustomButton(i, instr + Utils.GetFileSize(fi.Length) + "\n" + fi.DirectoryName);
                        i++;
                    }

                    mfftd.Show();
                    break;
            }
        }
    }
}