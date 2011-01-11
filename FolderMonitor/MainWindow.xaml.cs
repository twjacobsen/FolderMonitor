using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using System.Windows.Input;
using System.Xml.Serialization;
using Hardcodet.Wpf.TaskbarNotification;
using Ionic.Utils;
using Microsoft.Win32;
using System.Configuration;
using System.Windows.Controls;
using XbmcJson;
using System.Reflection;
using System.ComponentModel;
using System.Timers;

namespace FolderMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> FilterDataTypeList = new List<string>();

        private ObservableCollection<SourceDest> folders = new ObservableCollection<SourceDest>(); 
        public ObservableCollection<SourceDest> Folders
        {
            get { return folders; }
        }

        private ObservableCollection<Error> errors = Globals.Errors;
        public ObservableCollection<Error> Errors
        {
            get { return errors; }
        }

        List<FileMonitor> FileMonitors = new List<FileMonitor>();

        private bool isAppClosing;
        private bool tooltipShown;

        RegistryKey startupRegistry = null;

        Timer reconnectTimer = null;

        public MainWindow()
        {
            InitializeComponent();

            startupRegistry = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        }

        private void AppLoaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                          "\\FolderMonitor");
                if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor\\Sources.xml"))
                    File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor\\Sources.xml");
                if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor\\FileTypes.xml"))
                    File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor\\FileTypes.xml");
            }
            else
            {
                LoadXML();   
            }

            foreach(SourceDest d in Folders)
            {
                FileMonitor fm = new FileMonitor(d.Source, d.Dest, this);
                FileMonitors.Add(fm);
            }

            #region Initialize settings

            if (Globals.settings.CloseToTray)
            {
                isAppClosing = false;
                NotifyIcon.TrayMouseDoubleClick += NotifyIcon_TrayMouseDoubleClick;
            }
            else
            {
                isAppClosing = true;
            }

            if (Globals.settings.RunOnStartup)
            {
                if (startupRegistry.GetValue("FolderMonitor") == null)
                    startupRegistry.SetValue("FolderMonitor", Assembly.GetExecutingAssembly().GetName().CodeBase);
            }
            else
            {
                startupRegistry.DeleteValue("FolderMonitor", false);
            }

            if (Globals.settings.IntegrateXBMC)
            {
                XBMCConnectionInfo.Visibility = Visibility.Visible;
                XBMCConnectionStatus.Visibility = Visibility.Visible;
                if (Globals.settings.XBMCPort.ToString() != String.Empty)
                {
                    ConnectXBMC();
                    reconnectTimer = new Timer(3600000);    //Set it to reconnect once an hour...
                    reconnectTimer.Elapsed += (s, f) => ConnectXBMC();
                }
            }
            else
            {
                XBMCConnectionInfo.Visibility = Visibility.Collapsed;
                XBMCConnectionStatus.Visibility = Visibility.Collapsed;
            }

            #endregion
        }

        private void AddSource(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(SourceBox.Text) && Directory.Exists(DestBox.Text))
            {
                folders.Add(new SourceDest
                                {
                                    Source = SourceBox.Text,
                                    Dest = DestBox.Text
                                });
                FileMonitors.Add(new FileMonitor(SourceBox.Text, DestBox.Text, this));
            }
            SaveXML();
        }

        private void DeleteSource(object sender, MouseButtonEventArgs e)
        {
            SourceDest d = FoldersList.SelectedItem as SourceDest;
            int i = folders.IndexOf(d);
            folders.Remove(d);
            FileMonitors.RemoveAt(i);
        }

        private void LoadXML()
        {
            XmlSerializer xml = new XmlSerializer(typeof(ObservableCollection<SourceDest>));
            using(StreamReader rd = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor\\Sources.xml"))
            {
                foreach (SourceDest d in xml.Deserialize(rd) as ObservableCollection<SourceDest>)
                {
                    folders.Add(d);
                }
            }

            xml = new XmlSerializer(typeof(List<string>));
            using (StreamReader rd = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor\\FileTypes.xml"))
            {
                string s = "";
                foreach (string d in xml.Deserialize(rd) as List<string>)
                {
                    FilterDataTypeList.Add(d);
                    s += d + ";";
                }
                FilterDataTypes.Text = s;
                SaveFilterDataTypes();
            }
        }

        private void SaveXML()
        {
            XmlSerializer xml = new XmlSerializer(typeof(ObservableCollection<SourceDest>));
            using(StreamWriter wr = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"\\FolderMonitor\\Sources.xml"))
            {
                xml.Serialize(wr, folders);
            }

            xml = new XmlSerializer(typeof(List<string>));
            using (StreamWriter wr = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FolderMonitor\\FileTypes.xml"))
            {
                xml.Serialize(wr, FilterDataTypeList);
            }
        }

        private void AppClosing(object sender, CancelEventArgs e)
        {
            SaveXML();
            if(!isAppClosing)
            {
                e.Cancel = true;
                if (!tooltipShown)
                {
                    const string title = "Folder Monitor";
                    const string text = "Folder Monitor is still running, click this icon to restore the window.";

                    //show balloon with built-in icon
                    NotifyIcon.ShowBalloonTip(title, text, BalloonIcon.None);
                    tooltipShown = true;
                }
                Hide();
            }
        }

        void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
           this.Show();
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            isAppClosing = true;
            this.Close();
        }

        private void OpenFileBrowser(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;

            TextBox tb = null;
            if (b.Name.Contains("Dest"))
                tb = DestBox;
            else if (b.Name.Contains("Source"))
                tb = SourceBox;

            var dlg = new FolderBrowserDialogEx
                          {
                              ShowNewFolderButton = true,
                              ShowEditBox = true,
                              RootFolder = Environment.SpecialFolder.MyComputer,
                              SelectedPath = tb.Text != String.Empty ? tb.Text : Environment.SpecialFolder.MyComputer.ToString(),
                          };

            var result = dlg.ShowDialog();

            tb.Text = result == System.Windows.Forms.DialogResult.OK ? dlg.SelectedPath : tb.Text;
        }

        static List<string> RemoveDuplicates(List<string> inputList)
        {
            Dictionary<string, int> uniqueStore = new Dictionary<string, int>();
            List<string> finalList = new List<string>();

            foreach (string currValue in inputList)
            {
                if (!uniqueStore.ContainsKey(currValue))
                {
                    uniqueStore.Add(currValue, 0);
                    finalList.Add(currValue);
                }
            }
            return finalList;
        }

        private void SaveFilterDataTypes(object sender, RoutedEventArgs e)
        {
            SaveFilterDataTypes();
        }

        private void SaveFilterDataTypes()
        {
            TextBox tb = FilterDataTypes;
            string[] types = tb.Text.Split(';');

            foreach (string type in types)
            {
                if (type.Length > 2 && type.Length < 5)
                {
                    FilterDataTypeList.Add(type);
                }
                else
                {
                    tb.Text.Replace(type + ";", "");
                }
            }
            FilterDataTypeList = RemoveDuplicates(FilterDataTypeList);

            string regEx = "";
            foreach (string s in FilterDataTypeList)
            {
                regEx += "(" + s + ")|";
            }

            Globals.fileTypeFilter = new Regex(@"^.+\.(" + regEx + ")$");
        }

        private void LoadSettings()
        {
            AppSettings s = Globals.settings;
            foreach (string name in s.SettingNames())
            {
                object o = FindName(name);     //Is there a valid control attached to the setting?
                if (o != null)
                {
                    string oType = o.GetType().ToString();                  //Get the control type
                    switch (oType.Substring(oType.LastIndexOf('.') + 1))      //Remove the System.Windows.Controls namespace
                    {
                        case "TextBox":
                            TextBox tb = o as TextBox;
                            tb.Text = s[name].ToString();
                            break;

                        case "CheckBox":
                            CheckBox cb = o as CheckBox;
                            cb.IsChecked = (bool)s[name];
                            break;

                        case "PasswordBox":
                            PasswordBox pb = o as PasswordBox;
                            pb.Password = s[name].ToString();
                            break;

                        case "ComboBox":
                            ComboBox combo = o as ComboBox;
                            combo.SelectedIndex = (int)s[name];
                            break;

                        default: break;
                    }
                }
            }
        }

        private void CloseToTray_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Globals.settings.CloseToTray = cb.IsChecked.Value;
            Globals.settings.Save();

            if (cb.IsChecked.Value)
            {
                isAppClosing = false;
                NotifyIcon.TrayMouseDoubleClick += NotifyIcon_TrayMouseDoubleClick;
            }
            else
            {
                isAppClosing = true;
            }
        }

        private void RunOnStartup_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Globals.settings.RunOnStartup = cb.IsChecked.Value;
            Globals.settings.Save();


            if(cb.IsChecked.Value)
            {
                if(startupRegistry.GetValue("FolderMonitor") == null)
                    startupRegistry.SetValue("FolderMonitor", System.Windows.Forms.Application.ExecutablePath.ToString());
            }
            else
            {
                startupRegistry.DeleteValue("FolderMonitor", false);
            }
        }

        private void IntegrateXBMC_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            Globals.settings.IntegrateXBMC = cb.IsChecked.Value;
            Globals.settings.Save();

            if (cb.IsChecked.Value)
            {
                XBMCConnectionInfo.Visibility = Visibility.Visible;
                XBMCConnectionStatus.Visibility = Visibility.Visible;
            }
            else
            {
                XBMCConnectionInfo.Visibility = Visibility.Collapsed;
                XBMCConnectionStatus.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveXBMCSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                Globals.settings.XBMCPort = Int32.Parse(XBMCPort.Text);
                Globals.settings.XBMCUser = XBMCUser.Text;
                Globals.settings.XBMCPassword = XBMCPassword.Text;

                Globals.settings.XBMCSettingsSaved = true;

                Globals.settings.Save();

                ConnectXBMC();
            }
            catch (Exception ex)
            {
                Globals.Errors.Add(new Error{Description = ex.Message, Dest = "", Source = ""});
            }
        }

        private void ConnectXBMC()
        {
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += (q, w) =>
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    XBMCConnectionStatus.Visibility = Visibility.Visible;
                    XBMCConnectionStatusText.Text = "Connecting...";
                }));
                string ip = "127.0.0.1";
                try
                {
                    Globals.XBMC = new XbmcConnection(ip, Globals.settings.XBMCPort, Globals.settings.XBMCUser, Globals.settings.XBMCPassword);
                    if (Globals.XBMC.Status.IsConnected)
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            XBMCConnectionStatus.Visibility = Visibility.Visible;
                            XBMCConnectionStatusText.Text = "Connected to " + ip;
                        }));
                    }
                    else
                    {
                        throw new Exception("Could not connect to XBMC");
                    }
                }
                catch (Exception e)
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        XBMCConnectionStatus.Visibility = Visibility.Visible;
                        XBMCConnectionStatusText.Text = "Connection failed";
                        XBMCConnectionStatusImage.ToolTip = e.Message + "\nIP: " + ip + "\n Stack Trace: " + e.StackTrace;
                    }));

                    if (!reconnectTimer.Enabled)
                        reconnectTimer.Start();
                }
            };
            bgw.RunWorkerAsync();
        }

        private void RetryAction(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    FileMover fm = new FileMover();
                    Error er = Errors[ErrorList.SelectedIndex];
                    fm.Copy(er.Source, er.Dest, this);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }

        private void IgnoreError(object sender, MouseButtonEventArgs e)
        {
            errors.RemoveAt(ErrorList.SelectedIndex);
        }
    }

    public class SourceDest
    {
        public string Source { get; set; }
        public string Dest { get; set; }
    }

    public class Error
    {
        public string Source { get; set; }
        public string Dest { get; set; }
        public string Description { get; set; }
        public Object ShowReload
        {
            get
            {
                if(Source == "" || Dest == "")
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }
    }

    /// <summary>
    /// Class for handling persisted settings
    /// </summary>
    public sealed class AppSettings : ApplicationSettingsBase
    {
        /// <summary>
        /// An IEnumerable containing all the names of the available settings
        /// </summary>
        /// <returns>IEnumerable[string] names</returns>
        public IEnumerable<string> SettingNames()
        {
            return from SettingsProperty p in Properties select p.Name;
        }

        [UserScopedSettingAttribute]
        [DefaultSettingValueAttribute("true")]
        public bool CloseToTray
        {
            get { return (bool)this["CloseToTray"]; }
            set { this["CloseToTray"] = value; }
        }

        [UserScopedSettingAttribute]
        [DefaultSettingValueAttribute("true")]
        public bool RunOnStartup
        {
            get { return (bool)this["RunOnStartup"]; }
            set { this["RunOnStartup"] = value; }
        }

        [UserScopedSettingAttribute]
        [DefaultSettingValueAttribute("false")]
        public bool IntegrateXBMC
        {
            get { return (bool)this["IntegrateXBMC"]; }
            set { this["IntegrateXBMC"] = value; }
        }

        [UserScopedSettingAttribute]
        [DefaultSettingValueAttribute("8000")]
        public int XBMCPort
        {
            get { return (int)this["XBMCPort"]; }
            set { this["XBMCPort"] = value; }
        }

        [UserScopedSettingAttribute]
        [DefaultSettingValueAttribute("")]
        public string XBMCUser
        {
            get { return (string)this["XBMCUser"]; }
            set { this["XBMCUser"] = value; }
        }

        [UserScopedSettingAttribute]
        [DefaultSettingValueAttribute("")]
        public string XBMCPassword
        {
            get { return (string)this["XBMCPassword"]; }
            set { this["XBMCPassword"] = value; }
        }

        [UserScopedSettingAttribute]
        [DefaultSettingValueAttribute("false")]
        public bool XBMCSettingsSaved
        {
            get { return (bool)this["XBMCSettingsSaved"]; }
            set { this["XBMCSettingsSaved"] = value; }
        }
    }

    public static class Globals
    {
        public static Regex fileTypeFilter = null;
        public static bool filter = true;
        public static AppSettings settings = new AppSettings();
        public static XbmcConnection XBMC = null;
        public static ObservableCollection<Error> Errors = new ObservableCollection<Error>();
    }
}
