using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Lamp
{
    internal class ExecutionHandler
    {
        public static bool ContinueAfterCommandLineParameters()
        {
            return ProcessArgs().Result;
        }

        private static async Task<bool> ProcessArgs()
        {
            try
            {
                SafeProcessArgsDelegate threadSafeCall = SafeProcessArgs;
                var continueRunning = await Task.Run(() => threadSafeCall.Invoke());
                return continueRunning;
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        delegate Task<bool> SafeProcessArgsDelegate();

        private static async Task<bool> SafeProcessArgs()
        {
            bool continueAfterProcessing = true;
            try
            {
                if (ConfigHandler.Instance.LaunchParameters.Count() == 0) return continueAfterProcessing;
                bool auto = false;
                bool updateClient = false;
                bool forceUpdate = false;
                bool loadTest = false;
                bool updateMaps = false;
                bool updateConfig = false;
                bool updatePlugins = false;
                bool updateScripts = false;

                string mapdir = "Maps";
                string maprepo = Paths.GitHub.MapRepositoryZip;
                string scriptdir = "Scripts";
                string scriptrepo = "";
                string plugindir = "Plugins";
                string pluginrepo = Paths.GitHub.PluginRepositoryZip;

                ConfigHandler.Instance.GenieDirectory = @"C:\OneDrive\Genie Clients\EnescuDashboard\test"; //FileHandler.LocalDirectory;

                foreach (string arg in ConfigHandler.Instance.LaunchParameters)
                {

                    switch (arg.Split('|')[0].ToLower())
                    {
                        case "--full":
                            continueAfterProcessing = false;
                            auto = true;
                            updateClient = true;
                            forceUpdate = true;
                            updateMaps = true;
                            updateConfig = true;
                            updatePlugins = true;
                            updateScripts = true;
                            break;
                        case "--automated":
                        case "--auto":
                        case "--a":
                            auto = true;
                            updateClient = true;
                            break;

                        case "--background":
                        case "--bg":
                        case "--b":
                            auto = true;
                            break;

                        case "--force":
                        case "--f":
                            forceUpdate = true;
                            updateClient = true;
                            break;
                        case "--t":
                        case "--test":
                            updateClient = true;
                            loadTest = true;
                            break;
                        case "--m":
                        case "--map":
                        case "--maps":
                            string[] mapArgs = arg.Split('|');
                            if (mapArgs.Length > 1) mapdir = mapArgs[1];
                            if (mapArgs.Length > 2) maprepo = mapArgs[2];
                            updateMaps = true;
                            break;
                        case "--c":
                        case "--config":
                            updateConfig = true;
                            break;
                        case "--p":
                        case "--plugin":
                        case "--plugins":
                            string[] pluginArgs = arg.Split('|');
                            if (pluginArgs.Length > 1) plugindir = pluginArgs[1];
                            if (pluginArgs.Length > 2) pluginrepo = pluginArgs[2];
                            updatePlugins = true;
                            break;
                        case "--s":
                        case "--script":
                        case "--scripts":
                            string[] scriptArgs = arg.Split('|');
                            if (scriptArgs.Length > 2)
                            {
                                scriptdir = scriptArgs[1];
                                scriptrepo = scriptArgs[2];
                                updateScripts = true;
                            }
                            break;

                        default:
                            break;
                    }
                } //end foreach arg


                string targetRelease = loadTest ? Paths.GitHub.TestRelease : Paths.GitHub.LatestRelease;
                ReleaseFile release = FileHandler.GetRelease(targetRelease).Result;
                release.LoadAssets();

                if (updateClient) await UpdateClient(release, forceUpdate);
                if (updateConfig) await UpdateConfig(release.Assets);
                if (updateMaps || updatePlugins || updateScripts) await ConfigHandler.Instance.Load();
                if (updateConfig || updateMaps) await UpdateMaps(maprepo, FileHandler.GetAbsolutePath(mapdir));
                if (updateConfig || updatePlugins) await UpdatePlugins(pluginrepo, FileHandler.GetAbsolutePath(plugindir));
                if (updateConfig || updateScripts) await UpdateScripts(scriptrepo, FileHandler.GetAbsolutePath(scriptdir));
                if (auto)
                {
                    continueAfterProcessing = false;
                    Rub();
                }
            }
            catch(Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }
            return continueAfterProcessing;
            
        }

        public static async Task<bool> InstallFull(bool test)
        {
            try
            {
                SafeInstallFullDelegate install = SafeInstallFull;
                var success = await Task.Run(()=>install.Invoke(test));
                return success;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        delegate Task<bool> SafeInstallFullDelegate(bool test);
        private static async Task<bool> SafeInstallFull(bool test)
        {
            string targetRelease = test ? Paths.GitHub.TestRelease : Paths.GitHub.LatestRelease;
            ReleaseFile release = FileHandler.GetRelease(targetRelease).Result;
            release.LoadAssets();
            if (UpdateClient(release, true).Result)
            {
                if (test)
                {
                    release = FileHandler.GetRelease(Paths.GitHub.LatestRelease).Result;
                    release.LoadAssets();
                }
                return await UpdateConfig(release.Assets);
            }
            else
            {
                Console.WriteLine("Install Failed. Client did not update successfully.");
                return false;
            }
        }

        public static async Task<bool> UpdateClient(bool test)
        {
            try
            {
                SafeUpdateClientOverloadDelegate update = SafeUpdateClientOverload;
                var success = await Task.Run(() => update.Invoke(test));
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public static async Task<bool> UpdateClient(ReleaseFile release, bool force)
        {
            try
            {
                SafeUpdateClientDelegate update = SafeUpdateClient;
                var success = await Task.Run(() => update.Invoke(release, force));
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        delegate Task<bool> SafeUpdateClientOverloadDelegate(bool test);
        delegate Task<bool> SafeUpdateClientDelegate(ReleaseFile release, bool force);
        public static async Task<bool> SafeUpdateClientOverload(bool test)
        {
            string targetRelease = test ? Paths.GitHub.TestRelease : Paths.GitHub.LatestRelease;
            ReleaseFile release = FileHandler.GetRelease(targetRelease).Result;
            release.LoadAssets();
            return await UpdateClient(release, true);
        }
        public static async Task<bool> SafeUpdateClient(ReleaseFile release, bool force)
        {
            string genieExecutablePath = Path.Combine(ConfigHandler.Instance.GenieDirectory, "Genie.exe");
            if (!force && File.Exists(genieExecutablePath) && FileHandler.GetFileVersion(genieExecutablePath) == release.Version)
            {
                Console.WriteLine("This instance of Genie is using the latest release.");
                return true; //TRUE that Client is UP TO DATE
            }
            else
            {
                try
                {
                    Dictionary<string, AssetFile> assets = release.Assets;
                    AssetFile zipAsset = new AssetFile() { Name = "Invalid" };
                    if (assets.ContainsKey(Paths.FileNames.Client))
                    {
                        zipAsset = assets[Paths.FileNames.Client];
                    }
                    Console.WriteLine("Downloading latest client");
                    return await FileHandler.AcquirePackageInMemory(zipAsset.DownloadURL, ConfigHandler.Instance.GenieDirectory);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }

        public static async Task<bool> UpdateConfig()
        {
            try
            {
                SafeUpdateConfigDelegate update = SafeUpdateConfig;
                var success = await Task.Run(() => update.Invoke());
                return success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        delegate Task<bool> SafeUpdateConfigDelegate();
        
        public static async Task<bool> SafeUpdateConfig()
        {
            ReleaseFile release = FileHandler.GetRelease(Paths.GitHub.LatestRelease).Result;
            release.LoadAssets();
            return await UpdateConfig(release.Assets);
        }
        private static async Task<bool> UpdateConfig(Dictionary<string, AssetFile> Assets)
        {
            try
            {
                AssetFile configAsset = Assets[Paths.FileNames.Config];
                return await FileHandler.AcquirePackageInMemory(configAsset.DownloadURL, ConfigHandler.Instance.GenieDirectory);
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        
        public static async Task<bool> UpdateMaps(string maprepo, string mapdir)
        {
            try
            {
                bool updateSuccessful = await FileHandler.AcquirePackageInMemory(maprepo, mapdir);
                if (updateSuccessful)
                {
                    List<DirectoryInfo> diMaps = new DirectoryInfo(mapdir).GetDirectories().ToList<DirectoryInfo>();
                    foreach (DirectoryInfo subdirectory in diMaps)
                    {
                        if (subdirectory.Name.ToUpper().Contains("MAPS FOLDER"))
                        {
                            FileHandler.CopyDirectoryContents(subdirectory.FullName, mapdir);
                        }
                        else if (subdirectory.Name.ToUpper().Contains("SCRIPTS FOLDER"))
                        {                            string scriptdir = "Scripts";
                            if (ConfigHandler.Instance.Configs.ContainsKey("scriptdir")) scriptdir = ConfigHandler.Instance.Configs["scriptdir"];
                            scriptdir = FileHandler.GetAbsolutePath(scriptdir);
                            FileHandler.CopyDirectoryContents(subdirectory.FullName, scriptdir);
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static async Task<bool> UpdatePlugins(string pluginrepo, string plugindir)
        {
            return await FileHandler.AcquirePackageInMemory(pluginrepo, plugindir);
        }
        public static async Task<bool> UpdatePlugins(string destination, Dictionary<string, AssetFile> Assets)
        {
            
            try
            {
                AssetFile pluginsAsset = Assets[Paths.FileNames.Plugins];
                return await FileHandler.AcquirePackageInMemory(pluginsAsset.DownloadURL, destination);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public static async Task<bool> UpdateScripts(string scriptrepo, string scriptdir)
        {
            try
            {
                return await FileHandler.AcquirePackageInMemory(scriptrepo, scriptdir);
            }
            catch
            { 
                return false; 
            }
        }

        public static void Rub()
        {
            FileHandler.LaunchGenie();
        }

    }
}
