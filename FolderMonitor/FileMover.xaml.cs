using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.ComponentModel;
using System.Timers;

namespace FolderMonitor
{
    /// <summary>
    /// Interaction logic for FileMover.xaml
    /// </summary>
    public partial class FileMover : Window
    {
        private string source, dest;
        private int progress;
        private UIElement caller;

        #region Constructor

        public FileMover()
        {
            InitializeComponent();
            Top = SystemParameters.WorkArea.Height - Height - 2;
            Left = SystemParameters.PrimaryScreenWidth - Width - 2;
        }

        #endregion

        #region Public funcitons

        public void Copy(string _source, string _dest, UIElement _caller)
        {
            Show();
            
            source = _source;
            dest = _dest;
            caller = _caller;

            BackgroundWorker bgWorker = new BackgroundWorker{ WorkerReportsProgress = true };

            MovementProgress ctrl = new MovementProgress();
            StackContent.Children.Add(ctrl);
            UpdateLayout();

            ctrl.FileToProcess.Text = source;
            ctrl.MovementProgressBar.Value = 0;
            bgWorker.DoWork += (s, e) => MoveContent(false, bgWorker);

            bgWorker.ProgressChanged += (s, e) =>
            {
                ctrl.MovementProgressBar.Value += e.ProgressPercentage;
            };

            bgWorker.RunWorkerCompleted += (s, e) =>
            {
                ctrl.MovementProgressBar.Value = 100;

                StackContent.Children.Remove(ctrl);
                if (StackContent.Children.Count == 0)
                {
                    MovementProgress done = new MovementProgress();
                    done.FileToProcess.Text= "---- DONE! ----";
                    done.MovementProgressBar.Value = 100;
                    StackContent.Children.Add(done);
                    Timer t = new Timer(1000);
                    t.Start();
                    t.Elapsed += (xs, xe) => Dispatcher.Invoke(new Action(Close));
                }

                if (Globals.settings.IntegrateXBMC)
                {
                    if (Globals.XBMC != null && Globals.XBMC.Status.IsConnected)
                    {
                        if (!Globals.XBMC.Player.IsVideoPlayerActive())
                        {
                            try
                            {
                                Globals.XBMC.VideoLibrary.ScanForContent();
                            }
                            catch(Exception ex)
                            {
                                caller.Dispatcher.Invoke(new Action(() => Globals.Errors.Add(new Error
                                {
                                    Description = ex.Message,
                                    Dest = "",
                                    Source = ""
                                })));
                            }
                        }
                        else
                        {
                            DelayContentScan();
                        }
                    }
                }
            };

            bgWorker.RunWorkerAsync();
        }

        #endregion

        #region Private functions

        private void DelayContentScan()
        {
            Timer t = new Timer(1800000);
            t.Elapsed += (s, a) =>
            {
                if (!Globals.XBMC.Player.IsVideoPlayerActive())
                {
                    try
                    {
                        Globals.XBMC.VideoLibrary.ScanForContent();
                    }
                    catch (Exception ex)
                    {
                        caller.Dispatcher.Invoke(new Action(() => Globals.Errors.Add(new Error
                        {
                            Description = ex.Message,
                            Dest = "",
                            Source = ""
                        })));
                    }
                }
                else
                    DelayContentScan();
            };
        }

        private void MoveContent(bool move, BackgroundWorker bgWorker)
        {
            SubfolderTVShow();

            bool isFolder = !File.Exists(source);
            if (isFolder)
            {
                string folderName = source.Substring(source.LastIndexOf('\\') + 1);
                try
                {
                    DirectoryInfo _source = new DirectoryInfo(source);
                    DirectoryInfo _target = new DirectoryInfo(Path.Combine(dest, folderName));
                    progress = (int)Math.Round(100 / NumberOfFiles(_source), 0, MidpointRounding.ToEven);
                    MoveOrCopyFolder(_source, _target, move, bgWorker);
                }
                catch (Exception ex)
                {
                    bgWorker.ReportProgress(100);
                    caller.Dispatcher.Invoke(new Action(() => Globals.Errors.Add(new Error
                                                                                     {
                                                                                         Description = ex.Message,
                                                                                         Dest = dest,
                                                                                         Source = source
                                                                                     })));
                }
            }
            else
            {
                try
                {
                    FileInfo fi = new FileInfo(source);
                    if (Globals.filter && Globals.fileTypeFilter.Match(fi.Extension).Success)
                        return;
                    if (move)
                        File.Move(source, Path.Combine(dest, fi.Name));
                    else
                        File.Copy(source, Path.Combine(dest, fi.Name));

                    bgWorker.ReportProgress(100);
                }
                catch (Exception ex)
                {
                    bgWorker.ReportProgress(100);
                    caller.Dispatcher.Invoke(
                        new Action(
                            () => Globals.Errors.Add(new Error {Description = ex.Message, Dest = dest, Source = source})));
                }
            }
        }

        private double NumberOfFiles(DirectoryInfo dir)
        {
            double count = dir.GetFiles().Length;
            if (dir.GetDirectories().Length > 0)
            {
                count += dir.GetDirectories().Length;
                foreach (DirectoryInfo sDir in dir.GetDirectories())
                {
                    count += NumberOfFiles(sDir);
                }
            }
            return count;
        }

        private void MoveOrCopyFolder(DirectoryInfo source, DirectoryInfo target, bool move, BackgroundWorker bgWorker)
        {
            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                bgWorker.ReportProgress(progress);
                if (move)
                {
                    if(Globals.filter)
                    {
                        if (!Globals.fileTypeFilter.Match(fi.FullName).Success && !fi.FullName.Contains("sample"))
                            fi.MoveTo(Path.Combine(target.FullName, fi.Name));
                    }
                    else
                        fi.MoveTo(Path.Combine(target.FullName, fi.Name));
                }
                else
                {
                    if (Globals.filter)
                    {
                        if (!Globals.fileTypeFilter.Match(fi.FullName).Success && !fi.FullName.Contains("sample"))
                            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                    }
                    else
                        fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                if (!diSourceSubDir.Name.Contains("sample"))
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    MoveOrCopyFolder(diSourceSubDir, nextTargetSubDir, move, bgWorker);
                }
            }
        }

        private bool SubfolderTVShow()
        {
            string folderName;
            if (source.LastIndexOf('\\') > 0)
                folderName = source.Substring(source.LastIndexOf('\\')+1);
            else
                folderName = source.Substring(source.LastIndexOf('/')+1);

            //substring alt før SxxExx, evt med regexp
            //Dette giver TVShow navn
            Regex a = new Regex("[Ss][0-9][0-9][Ee][0-9][0-9]");
            //Check if file/folder is a TVShow
            if (a.Match(folderName).Index > 0)
            {
                string TVShowName;
                TVShowName = folderName.Substring(0, a.Match(folderName).Index).Replace('.', ' ').Trim();
                TextInfo tInfo = new CultureInfo("da-DK", true).TextInfo;
                TVShowName = tInfo.ToTitleCase(TVShowName);
                string season = "Season ";
                
                //Find Sxx i folderName
                string exSeason = folderName.Substring(a.Match(folderName).Index + 1, 2);
                if(exSeason[0] == '0')
                    season += exSeason[1];
                else
                    season += exSeason;

                DirectoryInfo dDest = new DirectoryInfo(dest);
                if(dDest.GetDirectories(TVShowName).Length > 0)
                {
                    dest += "\\"+TVShowName;
                    dDest = new DirectoryInfo(dest);
                    if (dDest.GetDirectories(season).Length > 0)
                        dest += "\\"+season;
                    else
                    {
                        dDest = dDest.CreateSubdirectory(season);
                        dest = dDest.FullName;
                    }
                    
                }
                else
                {
                    dDest = dDest.CreateSubdirectory(TVShowName);
                    dDest = dDest.CreateSubdirectory(season);
                    dest = dDest.FullName;
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Event handlers

        private void HideFileMover(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Hide();
        }

        #endregion
    }
}
