using System;
using System.Collections.Generic;
using System.Text;

namespace Lamp
{
    public static class Paths
    {
        public static class GitHub
        {
            public const string LatestRelease = @"https://api.github.com/repos/GenieClient/Genie4/releases/latest";
            public const string TestRelease = @"https://api.github.com/repos/GenieClient/Genie4/releases/tags/Test_Build";
            public const string MapRepositoryZip = @"https://github.com/GenieClient/Maps/archive/refs/heads/main.zip";
            public const string PluginRepositoryZip = @"https://github.com/GenieClient/Plugins/archive/refs/heads/main.zip";
        }

        public static class FileNames
        {
            //these are the file names that will be published to GitHub
            public const string Client = "Genie4.zip";
            public const string Plugins = "Plugins.zip";
            public const string Config = "Base.Config.Files.zip";
        }
    }
}

