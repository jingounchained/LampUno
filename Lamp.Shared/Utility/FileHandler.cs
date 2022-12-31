using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.UI.Xaml.Shapes;


namespace Lamp
{
    internal static class FileHandler
    {
        private static readonly HttpClient Client = new HttpClient();
        public static readonly string LocalDirectory = AppDomain.CurrentDomain.BaseDirectory;
        
        public static string GetFileVersion(string filename)
        {
            return FileVersionInfo.GetVersionInfo(filename).FileVersion;
        }
        public static async Task<MemoryStream> DownloadToMemoryStream(string downloadURL)
        {
            SafeDownloadToMemoryStreamDelegate threadSafeCall = SafeDownloadToMemoryStream;
            var memoryStream = Task.Run(() => threadSafeCall.Invoke(downloadURL));
            return await memoryStream;
        }
        delegate Task<MemoryStream> SafeDownloadToMemoryStreamDelegate(string downloadURL);
        private static async Task<MemoryStream> SafeDownloadToMemoryStream(string downloadURL)
        {
            try
            {
                Console.Write($"Downloading {downloadURL}");
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Add("User-Agent", "Genie Client Updater");
                var response = Client.GetAsync(new Uri(downloadURL)).Result;
                MemoryStream memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream);
                return memoryStream;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                string s = ex.Message;
                return new MemoryStream();
            }
        }

        private static readonly string lampFilename = Process.GetCurrentProcess().MainModule.FileName.ToUpper();
        public static async Task<bool> AcquirePackageInMemory(string packageURL, string packageDestination)
        {
            SafeAcquirePackageInMemoryDelegate threadSafeCall = SafeAcquirePackageInMemory;
            return await Task.Run(() => threadSafeCall.Invoke(packageURL, packageDestination));
        }
        delegate Task<bool> SafeAcquirePackageInMemoryDelegate(string packageURL, string packageDestination);
        private static async Task<bool> SafeAcquirePackageInMemory(string packageURL, string packageDestination)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Write("The Updater is not currently supported for this system.");
                return false;
            }
            try
            {
                ZipArchive archive = new ZipArchive(DownloadToMemoryStream(packageURL).Result);
                Console.WriteLine($"Unpacking Archive {System.IO.Path.GetFileNameWithoutExtension(packageURL)}");
                int stripFromBeginning = 0;
                if (archive.Entries.Count > 0 && archive.Entries[0].FullName.EndsWith("-main/"))
                {
                    //if this is from a github repo it will be in a folder in the zip's root that ends -main/
                    //and we want to not extract that and extract from it so we need to strip it off the path
                    stripFromBeginning = archive.Entries[0].FullName.Length;
                }
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.ToUpper() == lampFilename || entry.Name.ToLower() == ".gitignore") continue; 
                    string packageFile = System.IO.Path.Combine(packageDestination, entry.FullName.Remove(0, stripFromBeginning).Replace("/", "\\"));
                    if (entry.FullName.Length - stripFromBeginning > 0)
                    {
                        if (packageFile.EndsWith("\\"))
                        {
                            if (!Directory.Exists(packageFile))
                            {
                                Console.WriteLine($"Creating Directory at {packageFile}");
                                Directory.CreateDirectory(packageFile);
                            }
                        }
                        else if (File.Exists(packageFile))
                        {
                            FileInfo existingFile = new FileInfo(packageFile);
                            if (existingFile.LastWriteTime != entry.LastWriteTime)
                            {
                                File.Delete(packageFile);
                                entry.ExtractToFile(packageFile);
                                Console.WriteLine($"Update {packageFile}");
                            }
                        }
                        else
                        {
                            entry.ExtractToFile(packageFile);
                            Console.WriteLine($"Extracted {packageFile}");
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static async Task<bool> SaveTextToFile(List<string> lines, string filename)
        {
            SafeSaveTextToFileDelegate threadSafeCall = SafeSaveTextToFile;
            var success = Task.Run(() => threadSafeCall.Invoke(lines, filename));
            return await success;
        }
        delegate Task<bool> SafeSaveTextToFileDelegate(List<string> lines, string filename);
        public static async Task<bool> SafeSaveTextToFile(List<string> lines, string filename)
        {
            try
            {
                FileInfo file = new FileInfo(filename);

                if (file.Exists)
                {
                    int timer = 0;
                    do
                    {
                        Thread.Sleep(10);
                        timer++;
                    } while (FileIsLocked(file) && timer < 1000); //fail after 10 seconds
                }
                if(!file.Directory.Exists) file.Directory.Create();
                using (StreamWriter writer = new StreamWriter(filename, false))
                {
                    foreach (string line in lines) writer.WriteLine(line);
                }
                Console.WriteLine($"Saved {filename}");
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public static async Task<List<string>> GetFileLines(string filename)
        {
            SafeGetFileLinesDelegate threadSafeCall = SafeGetFileLines;
            var lines = Task.Run(() => threadSafeCall.Invoke(filename));
            return await lines;
        }
        delegate Task<List<string>> SafeGetFileLinesDelegate(string filename);
        public static async Task<List<string>> SafeGetFileLines(string filename)
        {
            FileInfo file = new FileInfo(filename);
            List<string> lines = new List<string>();
            if (file.Exists)
            {
                Console.WriteLine($"Loading {filename}");
                do { Thread.Sleep(10); } while (FileIsLocked(file));
                using (StreamReader reader = new StreamReader(file.FullName))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }
            }
            else
            {
                Console.WriteLine($"File Not Found: {filename}");
            }
            return lines;
        }

        public static async Task<bool> LoadReleaseAssets(ReleaseFile release)
        {
            if (!string.IsNullOrWhiteSpace(release.AssetsURL))
            {
                Console.WriteLine($"Loading Assets for {release.Version}");
                release.Assets = new Dictionary<string, AssetFile>();
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                Client.DefaultRequestHeaders.Add("User-Agent", "Genie Client Updater");

                var streamTask = Client.GetStreamAsync(release.AssetsURL);
                List<AssetFile> latestAssets = await JsonSerializer.DeserializeAsync<List<AssetFile>>(streamTask.Result);
                foreach (AssetFile asset in latestAssets)
                {
                    release.Assets.Add(asset.Name, asset);
                }
                return true;
            }
            return false;
        }
        
        public static async Task<ReleaseFile> GetRelease(string githubPath)
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            Client.DefaultRequestHeaders.Add("User-Agent", "Genie Client Updater");
            try
            {
                var streamTask = Client.GetStreamAsync(githubPath);
                ReleaseFile latest = await JsonSerializer.DeserializeAsync<ReleaseFile>(streamTask.Result);
                return latest;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new ReleaseFile() { Version = "404", AssetsURL = "Something's wrong. No server version could be found. " };
            }
        }
        public static async Task<ReleaseFile> GetCurrentVersion()
        {
            ReleaseFile current = new ReleaseFile();
            if (File.Exists($"{LocalDirectory}\\genie.exe"))
            {
                current.Version = FileVersionInfo.GetVersionInfo($"{LocalDirectory}\\genie.exe").FileVersion;
            }
            else
            {
                current.Version = "0";
            }
            return current;
        }

        public static void LaunchGenie()
        {
            string genie = GetAbsolutePath("Genie.exe");
            if (File.Exists(genie))
            {
                Console.WriteLine("Launching Genie . . .");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    FileInfo file = new FileInfo(genie);
                    do { Thread.Sleep(10); } while (FileIsLocked(file));
                    Process.Start(genie);
                }
            }
        }

        public static string GetAbsolutePath(string path)
        {
            if (path.StartsWith(@"\\") || path.Contains(":")) return path; //it is already absolute
            return System.IO.Path.Combine(ConfigHandler.Instance.GenieDirectory, path); //it is relative, base on Genie Directory
        }

        public static void CopyDirectoryContents(string sourceDirectory, string targetDirectory)
        {
            Console.WriteLine($"Copying Contents of {sourceDirectory} to {targetDirectory}");
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);
            if (!diTarget.Exists) diTarget.Create();
            foreach (FileInfo file in diSource.GetFiles())
            {
                file.CopyTo(System.IO.Path.Combine(targetDirectory, file.Name), true);
                Console.WriteLine($"Copied {file.Name} to {System.IO.Path.GetDirectoryName(targetDirectory)}");
            }
            Console.WriteLine($"Finished Copying {sourceDirectory} to {targetDirectory}");
        }

        public static bool FileIsLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException) //this exception confirms the file is locked
            {
                return true;
            }
            return false;
        }

        public static bool DirectoryContainsGenie(string directory)
        {
            return Directory.Exists(directory) && File.Exists(GetAbsolutePath("Genie.exe"));
        }
    }
}
