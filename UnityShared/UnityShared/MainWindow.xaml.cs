using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace UnityShared
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow main;

        public MainWindow()
        {
            main = this;
            InitializeComponent();
            ReloadMetaFile();
        }

        string BaseDir => AppDomain.CurrentDomain.BaseDirectory;

        string Meta()
        {
            return Path.Combine(BaseDir, "meta.txt");
        }

        void ReloadMetaFile()
        {
            // precleanup
            projects.Items.Clear();
            gitPath.Path = "";

            if (!File.Exists(Meta()))
                File.Create(Meta()).Close();
            string[] lines = File.ReadAllLines("meta.txt");
            if (lines.Length < 2)
            {
                Error("Empty meta, creating it.");
                GenerateFromMeta(lines, "");
                return;
            }
            var version = lines[0];
            var gitFolderPath = lines[1];
            GenerateFromMeta(lines, gitFolderPath);
        }

        private void GenerateFromMeta(string[] lines, string gitFolderPath)
        {
            gitPath.Path = gitFolderPath;

            if (lines.Length > 3)
            {
                var projectFileCountStr = lines[2];
                int projectFileCount;
                if (!int.TryParse(projectFileCountStr, out projectFileCount))
                    Error($"Invalid meta: project count isn't number.");
                else if (lines.Length < 2 + projectFileCount)
                    Error($"Invalid meta: invalid project count. {projectFileCount} {Meta()}");
                else
                {
                    for (int i = 0; i < projectFileCount; i++)
                    {
                        var projectMeta = lines[3 + i];
                        var projectSubfolder = projectMeta;

                        AddProject(projectSubfolder);
                    }
                }
            }

            UpdateFoldersUpToDate(gitPath.Path);
        }

        private void UpdateFoldersUpToDate(string gitFolderPath)
        {
            foreach (var item in projects.Items)
            {
                var project = (singleProjet)item;
                bool diffFileNames;
                project.Updated = EqualFolders(gitFolderPath, project.Path, out diffFileNames);
            }
        }

        static bool EqualFolders(string folder1, string folder2, out bool diffFileNames)
        {
            diffFileNames = false;
            if (!Directory.Exists(folder1) || !Directory.Exists(folder2))
                return false;
            if (!folder1.EndsWith("\\"))
                folder1 += '\\';
            if (!folder2.EndsWith("\\"))
                folder2 += '\\';

            // both folders contain path and have same date.
            Dictionary<string, bool> paths = new Dictionary<string, bool>();

            var sourceFiles1 = Directory.GetFiles(folder1, "*", SearchOption.AllDirectories);
            var sourceFiles2 = Directory.GetFiles(folder2, "*", SearchOption.AllDirectories);
            int prefixLen1 = folder1.Length;
            int prefixLen2 = folder2.Length;
            foreach (var file in sourceFiles1)
            {
                if (file.EndsWith(".meta"))
                    continue;
                var fileInFolder = file.Substring(prefixLen1);
                paths.Add(fileInFolder, false);
            }
            foreach (var file in sourceFiles2)
            {
                if (file.EndsWith(".meta"))
                    continue;
                var fileInFolder = file.Substring(prefixLen2);
                if (!paths.ContainsKey(fileInFolder))
                {
                    paths.Add(fileInFolder, false);
                    diffFileNames = true;
                }
                else
                {
                    // match to existing folder 1 file
                    var timesMatch = File.GetLastWriteTime(file)
                        .Equals(File.GetLastWriteTime(folder1 + fileInFolder));
                    paths[fileInFolder] = timesMatch;
                }
            }
            // any folder is updated
            foreach (var item in paths)
            {
                if (!item.Value)
                    return false;
            }
            return true;
        }

        internal static void Pull(singleProjet projec)
        {
            // don't erase when pulling, because it can override metadata
            CopySourceToFolder(main.gitPath.Path, projec.Path, false);
            projec.Updated = true;
        }

        internal static void PushGit(singleProjet projec)
        {
            bool diffFileNames;
            bool changes = EqualFolders(projec.Path, projec.Path, out diffFileNames);
            CopySourceToFolder(projec.Path, main.gitPath.Path, diffFileNames);
            projec.Updated = true;
        }

        void SaveMeta()
        {
            string content = $"v1\n{gitPath.Path}\n{projects.Items.Count}";
            for (int i = 0; i < projects.Items.Count; i++)
                content += $"\n{((singleProjet)projects.Items[i]).Path}";
            try
            {
                File.WriteAllText(Meta(), content);
            }catch(IOException ioe)
            {
                Error(ioe.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // add project
            AddProject("empty");
        }

        private void AddProject(string path)
        {
            var projec = new singleProjet();
            projects.Items.Add(projec);
            projec.Path = path;
        }

        static void CopySourceToFolder(string sourceDir, string destinationDir, bool eraseDestination)
        {
            try
            {
                if (string.IsNullOrEmpty (sourceDir) || string.IsNullOrEmpty(destinationDir))
                    return;
                // .NET Docs
                // Get information about the source directory
                var dir = new DirectoryInfo(sourceDir);

                // Check if the source directory exists
                if (!dir.Exists)
                    throw new DirectoryNotFoundException($"Abort source transfer:Git folder is missing: {dir.FullName}{sourceDir} -> {destinationDir}");


                // Cache directories before we start copying
                DirectoryInfo[] dirs = dir.GetDirectories();

                if (eraseDestination && Directory.Exists(destinationDir))
                    Directory.Delete(destinationDir, true);

                // Create the destination directory
                Directory.CreateDirectory(destinationDir);

                // Get the files in the source directory and copy to the destination directory
                foreach (FileInfo file in dir.GetFiles())
                {
                    if (file.Name.EndsWith(".meta"))
                    {
                        if (eraseDestination)
                        {
                            Error("Do not erase meta files");
                            return;
                        }
                        continue;
                    }
                    string targetFilePath = Path.Combine(destinationDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }

                // If recursive and copying subdirectories, recursively call this method
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = System.IO.Path.Combine(destinationDir, subDir.Name);
                    CopySourceToFolder(subDir.FullName, newDestinationDir, eraseDestination);
                }

            }
            catch (IOException ioe)
            {
                Error(ioe.Message);
            }
        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SaveMeta();
            ReloadMetaFile();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ReloadMetaFile();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Helpers.OpenFolder(BaseDir);
        }

        static void Error(string msg)
        {
            MessageBox.Show(msg);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            UpdateFoldersUpToDate(gitPath.Path);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var id = projects.SelectedIndex;
            if (id > -1)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Remove project?", "Delete", MessageBoxButton.YesNoCancel);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    projects.Items.RemoveAt(id);
                }
            }
        }
    }

}

public static class Helpers{
    public static void OpenFolder(string folderPath, bool err = true)
    {
        if (Directory.Exists(folderPath))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = folderPath,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }
        else if(err)
        {
            Error($"{folderPath} directory does not exist!");
        }
    }
    public static void Error(string msg)
    {
        MessageBox.Show(msg);
    }
}