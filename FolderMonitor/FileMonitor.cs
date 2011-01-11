using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Windows;

namespace FolderMonitor
{
    public class FileMonitor
    {
        Timer timer;
        UIElement caller;
        Stack<string> directoriesToMove = new Stack<string>();
        public string SourceFolder, DestinationFolder;

        public FileMonitor(string _sourceFolder, string _destinationFolder, UIElement _caller)
        {
            caller = _caller;
            SourceFolder = _sourceFolder;
            DestinationFolder = _destinationFolder;

            if (!Directory.Exists(SourceFolder))
            {
                string parent = SourceFolder.Substring(0,SourceFolder.LastIndexOf('\\'));
                FileSystemWatcher w = new FileSystemWatcher(parent);
                w.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName;
                w.IncludeSubdirectories = false;
                w.Changed += (UpdateSource);
                w.Created += (UpdateSource);
                w.Renamed += (UpdateSource);
                w.EnableRaisingEvents = true;
            }
            else
            {
                FileSystemWatcher w = new FileSystemWatcher(SourceFolder);
                w.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName;
                w.IncludeSubdirectories = true;
                w.Changed += (CheckContent);
                w.Created += (CheckContent);
                w.EnableRaisingEvents = true;
                timer = new Timer(500);
                timer.Elapsed += MoveContent;
            }
        }

        void UpdateSource(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher s = sender as FileSystemWatcher;
            if(e.FullPath == SourceFolder)
            {
                FileSystemWatcher w = new FileSystemWatcher(SourceFolder);
                w.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName;
                w.IncludeSubdirectories = true;
                w.Changed += (CheckContent);
                w.Created += (CheckContent);
                w.EnableRaisingEvents = true;
                timer = new Timer(500);
                timer.Elapsed += MoveContent;

                //Unregister events...
                s.Changed -= UpdateSource;
                s.Created -= UpdateSource;
            }
        }

        void CheckContent(object sender, FileSystemEventArgs e)
        {
            if (timer.Enabled)
                timer.Enabled = false;
            timer.Stop();
            timer.Enabled = true;
            timer.Start();

            DirectoryInfo d = new DirectoryInfo(e.FullPath);
            if (d.Parent.FullName == SourceFolder)
                directoriesToMove.Push(e.FullPath);

        }

        void MoveContent(object sender, ElapsedEventArgs e)
        {
            timer.Enabled = false;
            timer.Stop();
            string d = "";
            caller.Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    FileMover fm = new FileMover();
                    foreach (string dir in directoriesToMove)
                    {
                        d = dir;
                        fm.Copy(dir, DestinationFolder, caller);
                    }
                    directoriesToMove.Clear();
                }
                catch (Exception ex)
                {
                    caller.Dispatcher.Invoke(new Action(() =>
                                                        Globals.Errors.Add(new Error
                                                                               {
                                                                                   Description = ex.Message,
                                                                                   Dest = DestinationFolder,
                                                                                   Source = d
                                                                               })));
                }
            }));
        }
    }
}
