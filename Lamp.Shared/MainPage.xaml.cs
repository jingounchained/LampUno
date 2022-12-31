using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using WinRT;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using System.Diagnostics;
using System.Drawing;
using Windows.Devices.Enumeration;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Dispatching;
using System.Xml.Linq;

namespace Lamp
{
    /// <summary>
    /// The Primary Page of the Application
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private DispatcherQueue uiDispatcherQueue;
        public string TitleText { get; } = "Lamp! For Genie!";

        public string DescriptionText { get; set; } = "Lamp is a custom installer for Genie 4 and can be used to update an existing Genie install (including from 3 to 4).";

        public string _actionText = "Processing Command Line Parameters.";
        public string ActionText
        {
            get
            {
                return _actionText;
            }
            set
            {
                if (_actionText != value)
                {
                    _actionText = value;
                    Console.WriteLine(value);
                    OnPropertyChanged("ActionText");
                }
            }
        }
        public string SelectedFolder
        {
            get
            {
                return ConfigHandler.Instance.GenieDirectory;
            }
            set
            {
                Console.Out.WriteLine(value);
                SelectFolder(value);
            }
        }

        public string MapDir
        {
            get
            {
                if (ConfigHandler.Instance.Configs.ContainsKey("mapdir"))
                {
                    return ConfigHandler.Instance.Configs["mapdir"];
                }
                else
                {
                    return "Maps";
                }
            }
            set
            {
                ConfigHandler.Instance.SetConfig("mapdir", value);
                OnPropertyChanged("MapDir");
            }
        }

        public string ScriptDir
        {
            get
            {
                if (ConfigHandler.Instance.Configs.ContainsKey("scriptdir"))
                {
                    return ConfigHandler.Instance.Configs["scriptdir"];
                }
                else
                {
                    return "Scripts";
                }
            }
            set
            {
                ConfigHandler.Instance.SetConfig("scriptdir", value);
                OnPropertyChanged("ScriptDir");
            }
        }

        public string PluginDir
        {
            get
            {
                if (ConfigHandler.Instance.Configs.ContainsKey("plugindir"))
                {
                    return ConfigHandler.Instance.Configs["plugindir"];
                }
                else
                {
                    return "Plugins";
                }
            }
            set
            {
                ConfigHandler.Instance.SetConfig("plugindir", value);
                OnPropertyChanged("PluginDir");
            }
        }

        public string MapRepo
        {
            get
            {
                if (ConfigHandler.Instance.Configs.ContainsKey("maprepo"))
                {
                    return ConfigHandler.Instance.Configs["maprepo"];
                }
                else
                {
                    return Paths.GitHub.MapRepositoryZip;
                }
            }
            set
            {
                ConfigHandler.Instance.SetConfig("maprepo", value);
                OnPropertyChanged("MapRepo");
            }
        }

        public string ScriptRepo
        {
            get
            {
                if (ConfigHandler.Instance.Configs.ContainsKey("scriptrepo"))
                {
                    return ConfigHandler.Instance.Configs["scriptrepo"];
                }
                else
                {
                    return "";
                }
            }
            set
            {
                ConfigHandler.Instance.SetConfig("scriptrepo", value);
                OnPropertyChanged("ScriptRepo");
            }
        }

        public string PluginRepo
        {
            get
            {
                if (ConfigHandler.Instance.Configs.ContainsKey("pluginrepo"))
                {
                    return ConfigHandler.Instance.Configs["pluginrepo"];
                }
                else
                {
                    return Paths.GitHub.PluginRepositoryZip;
                }
            }
            set
            {
                ConfigHandler.Instance.SetConfig("pluginrepo", value);
                OnPropertyChanged("PluginRepo");
            }
        }

        public bool DirectoryContainsGenie
        {
            get
            {
                return FileHandler.DirectoryContainsGenie(SelectedFolder);
            }
        }

        public string SelectedFolderTrimmed
        {
            get
            {
                if (SelectedFolder.Length < 39)
                {
                    return SelectedFolder;
                }
                else
                {
                    return SelectedFolder.Substring(0, 15) + "..." + SelectedFolder.Substring(SelectedFolder.Length - 20);
                }
            }
            set
            {
                OnPropertyChanged("SelectedFolderTrimmed");
            }
        }


        private List<Button> Tabs = new List<Button>();
        private List<StackPanel> Panels = new List<StackPanel>();
        public MainPage()
        {
            this.InitializeComponent();
            DataContext = this;
            Initialize();
        }

        private async void Initialize()
        {
            uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
            InitializeWindow();
            await Task.Run(async () =>
            {
                Console.Out.WriteLine("Processing Command Line Parameters");
                if (!ExecutionHandler.ContinueAfterCommandLineParameters())
                {
                    Environment.Exit(0);
                }
                else
                {
                    uiDispatcherQueue?.TryEnqueue(() => { OptionsButton.IsEnabled = true; });
                }
            });
            ActionText = "Loading Genie Settings.";
            SelectFolder(@"C:\OneDrive\Genie Clients\EnescuDashboard\test");//SelectFolder(FileHandler.LocalDirectory);
            InitializeTabs();
            ToggleDisplay(true);
        }

        private void InitializeWindow()
        {
            AppWindow app = (Application.Current as App).AppWindow;
            Windows.Graphics.SizeInt32 dimensions = new Windows.Graphics.SizeInt32();
            dimensions.Width = 950; //TODO: Make these size to fit content
            dimensions.Height = 700;
            app.Resize(dimensions);
            app.Title = TitleText;
            OptionsButton.IsEnabled = false;
            Console.SetOut(new TextBoxWriter(OutputTextbox, uiDispatcherQueue));
        }
        /// <summary>
        /// Populates the Tabs and Panels lists.
        /// To add a new Tab and Panel, the NAME of the Element should STARTWITH
        /// the same string - InstallTab and InstallPanel will be selected by SelectTab("Install")
        /// So beware not to use a duplicate that would trigger more than one
        /// </summary>
        private void InitializeTabs()
        {
            Tabs = new List<Button>() { InstallTab, UpdateTab, ConfigTab };
            Panels = new List<StackPanel> { InstallPanel, UpdatePanel, ConfigPanel };

            if (FileHandler.DirectoryContainsGenie(SelectedFolder)) SelectTab("Update");
            else SelectTab("Install");

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async void ConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleDisplay(false);
        }

        public async void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleDisplay(true);
        }
        public async void ChooseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.ViewMode = PickerViewMode.List;

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App).Window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
            if (storageFolder != null)
            {
                // Folder was picked you can now use it
                SelectedFolder = storageFolder.Path;
            }
            else
            {
                // No folder was picked or the dialog was cancelled.
            }
        }

        public async Task<bool> ChooseDirectory(string target)
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                folderPicker.FileTypeFilter.Add("*");
                folderPicker.ViewMode = PickerViewMode.Thumbnail;

                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App).Window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

                StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
                if (storageFolder != null)
                {
                    // Folder was picked you can now use it
                    if (ConfigHandler.Instance.Configs.ContainsKey(target))
                    {
                        ConfigHandler.Instance.Configs[target] = storageFolder.Path;
                    }
                    else
                    {
                        ConfigHandler.Instance.Configs.Add(target, storageFolder.Path);
                    }
                    return true;
                }
                else
                {
                    // No folder was picked or the dialog was cancelled.
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public void InstallTab_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Install");
        }
        public void UpdateTab_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Update");
        }
        public void ConfigTab_Click(object sender, RoutedEventArgs e)
        {
            SelectTab("Config");
        }

        private void SelectTab(string tab)
        {
            foreach (Button button in Tabs)
            {
                button.IsEnabled = !button.Name.StartsWith(tab);
            }
            foreach (StackPanel panel in Panels)
            {
                panel.Visibility = panel.Name.StartsWith(tab) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (rdoInstallClient.IsChecked == false
                && rdoInstallFull.IsChecked == false
                && rdoInstallTestFull.IsChecked == false
                && rdoInstallTest.IsChecked == false) return;

            bool test = rdoInstallTestFull.IsChecked == true || rdoInstallTest.IsChecked == true;
            bool full = rdoInstallFull.IsChecked == true || rdoInstallTestFull.IsChecked == true;
            OptionsButton.IsEnabled = false;
            ToggleDisplay(false);
            Task.Run(async () => {

                if (full) await ExecutionHandler.InstallFull(test);
                else await ExecutionHandler.UpdateClient(test);
                DetectGenie();
                uiDispatcherQueue?.TryEnqueue(() => { OptionsButton.IsEnabled = true; });
            });
        }
        public async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            string action = "fail";
            if (rdoUpdateClient.IsChecked == true) action = "client";
            else if (rdoUpdateTest.IsChecked == true) action = "test";
            else if (rdoUpdateMaps.IsChecked == true) action = "maps";
            else if (rdoUpdatePlugins.IsChecked == true) action = "plugins";
            else if (rdoUpdateScripts.IsChecked == true) action = "scripts";

            if (action == "fail") return;

            ToggleDisplay(false);
            OptionsButton.IsEnabled = false;

            await Task.Run(async () =>
            {

                switch (action)
                {
                    case "client":
                    case "test":
                        await ExecutionHandler.UpdateClient(action == "test");
                        break;

                    case "maps":
                        string dir, repo;
                        dir = ConfigHandler.Instance.GetConfig("mapdir");
                        if (dir.Length == 0) break;
                        else dir = FileHandler.GetAbsolutePath(dir);

                        repo = ConfigHandler.Instance.GetConfig("maprepo");
                        if (repo.Length == 0) repo = Paths.GitHub.MapRepositoryZip;

                        await ExecutionHandler.UpdateMaps(repo, dir);
                        break;

                    case "plugins":

                        dir = ConfigHandler.Instance.GetConfig("plugindir");
                        if (dir.Length == 0) break;
                        else dir = FileHandler.GetAbsolutePath(dir);

                        repo = ConfigHandler.Instance.GetConfig("pluginrepo");
                        if (repo.Length == 0) repo = Paths.GitHub.PluginRepositoryZip;

                        await ExecutionHandler.UpdatePlugins(repo, dir);
                        break;

                    case "scripts":
                        dir = ConfigHandler.Instance.GetConfig("scriptdir");
                        if (dir.Length > 0) dir = FileHandler.GetAbsolutePath(dir);

                        repo = ConfigHandler.Instance.GetConfig("scriptrepo");
                        if (repo.Length == 0) break;

                        break;
                }

                DetectGenie();

                uiDispatcherQueue?.TryEnqueue(() => { OptionsButton.IsEnabled = true; });
            });
        }

        public async void Save_Click(object sender, RoutedEventArgs e)
        {
            OptionsButton.IsEnabled = false;
            ToggleDisplay(false);
            await Task.Run(async () =>
            {
                await ConfigHandler.Instance.Save();

                uiDispatcherQueue?.TryEnqueue(() => { OptionsButton.IsEnabled = true; });
            });
        }

        public async void MapDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool updateSuccessful = await ChooseDirectory("mapdir");
                if (updateSuccessful)
                {
                    OnPropertyChanged("MapDir");
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        public async void PluginDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool updateSuccessful = await ChooseDirectory("plugintdir");
                if (updateSuccessful)
                {
                    OnPropertyChanged("PluginDir");
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
        public void MapDir_TextChanged(object sender, TextChangedEventArgs args)
        {
            MapDir = MapDirTextBox.Text;
        }
        public void PluginDir_TextChanged(object sender, TextChangedEventArgs args)
        {
            PluginDir = PluginDirTextBox.Text;
        }
        public void ScriptDir_TextChanged(object sender, TextChangedEventArgs args)
        {
            ScriptDir = ScriptDirTextBox.Text;
        }
        public void MapRepo_TextChanged(object sender, TextChangedEventArgs args)
        {
            MapRepo = MapRepoTextBox.Text;
        }
        public void PluginRepo_TextChanged(object sender, TextChangedEventArgs args)
        {
            PluginRepo = PluginRepoTextBox.Text;
        }
        public void ScriptRepo_TextChanged(object sender, TextChangedEventArgs args)
        {
            ScriptRepo = ScriptRepoTextBox.Text;
        }


        public async void ScriptDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool updateSuccessful = await ChooseDirectory("scriptdir");
                if (updateSuccessful)
                {
                    OnPropertyChanged("ScriptDir");
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        public void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void SelectFolder(string path)
        {
            ConfigHandler.Instance.GenieDirectory = path;
            SelectedFolderTrimmed = path;

            DetectGenie();
        }
        private void DetectGenie()
        {
            if (FileHandler.DirectoryContainsGenie(ConfigHandler.Instance.GenieDirectory))
            {
                ActionText = $"Genie Version {FileHandler.GetFileVersion(System.IO.Path.Combine(ConfigHandler.Instance.GenieDirectory, "Genie.exe"))} is detected in the target directory.";
            }
            else
            {
                ActionText = "Genie was not found in the target directory.";
            }
            TriggerPropertyUpdate();
        }

        public void TriggerPropertyUpdate()
        {
            OnPropertyChanged("SelectedFolder");
            OnPropertyChanged("ActionText");
            OnPropertyChanged("MapDir");
            OnPropertyChanged("ScriptDir");
            OnPropertyChanged("PluginDir");
            OnPropertyChanged("MapRepo");
            OnPropertyChanged("ScriptRepo");
            OnPropertyChanged("PluginRepo");
        }
        private void ToggleDisplay(bool mainGridVisilibity)
        {
            uiDispatcherQueue?.TryEnqueue(() => { InputPanel.Visibility = mainGridVisilibity ? Visibility.Visible : Visibility.Collapsed; });
            uiDispatcherQueue?.TryEnqueue(() => { OutputPanel.Visibility = mainGridVisilibity ? Visibility.Collapsed : Visibility.Visible; });
        }

        private async Task<Windows.Storage.StorageFolder> GetStorageFolder(string path)
        {
            Windows.Storage.StorageFolder targetFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(path);
            return targetFolder;
        }

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            uiDispatcherQueue?.TryEnqueue(() => SafeOnPropertyChanged(name));
            //SafeOnPropertyChangedDelegate threadSafeCall = SafeOnPropertyChanged;
            //Task.Run(() => threadSafeCall.Invoke(name));
        }
        private void SafeOnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
