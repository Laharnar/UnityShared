using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UnityShared
{
    /// <summary>
    /// Interaction logic for setupScreen.xaml
    /// </summary>
    public partial class setupScreen : UserControl
    {
        public setupScreen()
        {
            InitializeComponent();
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

        internal void Clear()
        {
            projects.Items.Clear();
            gitPath.Path = "";
        }

        public void UpdateFoldersUpToDate(string gitFolderPath)
        {
            foreach (var item in projects.Items)
            {
                var project = (singleProjet)item;
                bool diffFileNames;
                project.Updated = EqualFolders(gitFolderPath, project.Path, out diffFileNames);
            }
        }

        internal static void Pull(setupScreen setup, singleProjet projec)
        {
            // don't erase when pulling, because it can override metadata
            CopySourceToFolder(setup.gitPath.Path, projec.Path, false);
            projec.Updated = true;
        }

        internal static void PushGit(setupScreen setup, singleProjet projec)
        {
            bool diffFileNames;
            bool changes = EqualFolders(projec.Path, projec.Path, out diffFileNames);
            CopySourceToFolder(projec.Path, setup.gitPath.Path, diffFileNames);
            projec.Updated = true;
        }


        private void AddProject(string path)
        {
            var projec = new singleProjet();
            projects.Items.Add(projec);
            projec.Path = path;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // add project
            AddProject("empty");
        }

        internal void Save(ref StringBuilder store)
        {
            string content = $"{gitPath.Path}\n{projects.Items.Count}";
            for (int i = 0; i < projects.Items.Count; i++)
                content += $"\n{((singleProjet)projects.Items[i]).Path}";
            store.Append($"{content}\n");
        }

        internal int OnLoadSetup(string[] store, int start, int minCount)
        { 
            // return last line taken
            if (store.Length < start + minCount)
                return -1;

            var gitFolder = store[start];
            gitPath.Path = gitFolder;
            var count = 0;
            if (!int.TryParse(store[start + 1], out count))
                return -1;
            for (int i = start + 2; i < start + 2 + count; i++)
            {
                var projectSubfolder = store[i];
                AddProject(projectSubfolder);
            }
            UpdateFoldersUpToDate(gitPath.Path);
            return start + 2 + count - 1; // returns last id
        }

        static void CopySourceToFolder(string sourceDir, string destinationDir, bool eraseDestination)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(destinationDir))
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
                    string targetFilePath = System.IO.Path.Combine(destinationDir, file.Name);
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

        static void Error(string msg)
        {
            MessageBox.Show(msg);
        }

    }
}
