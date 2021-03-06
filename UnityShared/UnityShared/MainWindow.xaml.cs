using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace UnityShared
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow main;
        int SetupCount => SetupsTab.Items.Count;
        int SelectedId => SetupsTab.SelectedIndex;

        string BaseDir => AppDomain.CurrentDomain.BaseDirectory;
        public MainWindow()
        {
            main = this;
            InitializeComponent();
            LoadProject();
        }

        string Meta()
        {
            return Path.Combine(BaseDir, "meta.txt");
        }

        private void ShowAppFolder_Click(object sender, RoutedEventArgs e)
        {
            Helpers.OpenFolder(BaseDir);
        }

        static void Error(string msg)
        {
            MessageBox.Show(msg);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (SelectedId > -1)
            {
                var setup = GetSetup(SelectedId);
                setup.UpdateAreFoldersLatest(setup.gitPath.Path);
            }
        }

        setupScreen GetSetup(int id)
        {
            return ((setupScreen)GetTab(id).Content);
        }

        TabItem GetTab(int id)
        {
            return (TabItem)SetupsTab.Items[id];
        }

        private void NewSetup_Click(object sender, RoutedEventArgs e)
        {
            NewSetup();
        }

        void NewSetup()
        {
            SetupsTab.Items.Add(new TabItem()
            {
                Header = SetupName.Text,
                Content = new setupScreen()
            });
        }

        private void DeleteSetup_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedId > -1)
            {
                Helpers.YesNo(() =>
                {
                    SetupsTab.Items.RemoveAt(SelectedId);
                });
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder store = new StringBuilder();
            store.Append("v3\n");
            for (int i = 0; i < SetupCount; i++)
            {
                var tab = GetTab(i);
                store.AppendLine(tab.Header.ToString());
                var setup = GetSetup(i);
                setup.Save(ref store);
            }
            File.WriteAllText("meta.txt", store.ToString());
        }

        internal static void Pull(singleProjet singleProjet)
        {
            var setup = main.GetSetup(main.SelectedId);
            setupScreen.Pull(setup, singleProjet);
        }

        internal static void PushGit(singleProjet singleProjet)
        {
            var setup = main.GetSetup(main.SelectedId);
            setupScreen.PushGit(setup, singleProjet);

            main.UpdateAllSetups();
        }

        void UpdateAllSetups()
        {
            for (int i = 0; i < SetupCount; i++)
            {
                var setup = GetSetup(i);
                setup.UpdateAreFoldersLatest(setup.gitPath.Path);
            }
        }

        void PrepareBeforeLoad()
        {
            // precleanup
            for (int i = 0; i < SetupCount; i++)
                GetSetup(i).Clear();

            if (!File.Exists(Meta()))
                File.Create(Meta()).Close();
        }

        private void LoadProject_Click(object sender, RoutedEventArgs e)
        {
            LoadProject();
        }

        private void LoadProject()
        {
            PrepareBeforeLoad();
            var lines = File.ReadAllLines("meta.txt");
            if (lines.Length == 0)
            {
                return;
            }
            var funcs = new Functions();
            funcs.GetSetup = GetSetup;
            funcs.NewSetup = NewSetup;
            funcs.GetTab = GetTab;
            int setupId = 0;
            var version = lines[0];
            var reader = new Version2Reader("v3");
            SetupsTab.Items.Clear();
            reader.LoadProject(
                new List<string>() { "v2", "v3" },
                version, lines, funcs);

            if (setupId > 0)
                SetupsTab.SelectedIndex = 0;
        }

        private void RenameSetup_Click(object sender, RoutedEventArgs e)
        {
            var tab = GetTab(SelectedId);
            tab.Header = SetupName.Text;
        }

        class Functions
        {
            public Action NewSetup;
            public Func<int, setupScreen> GetSetup; // id -> result
            public Func<int, TabItem> GetTab;
        }

        class Version2Reader
        {
            string version;

            public Version2Reader(string version)
            {
                this.version = version;
            }

            public void LoadProject(List<string> supportsVersions, string inFileVersion, string[] lines, Functions funcs)
            {
                if (!supportsVersions.Contains(version))
                    return;

                int setupId = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    funcs.NewSetup();
                    var setup = funcs.GetSetup(setupId);
                    var setupName = lines[i];
                    var tab = funcs.GetTab(setupId);
                    tab.Header = setupName;
                    i = setup.OnLoadSetup(lines, i + 1, 2, inFileVersion);
                    if (i == -1)
                    {
                        Error($"Failure to load meta, invalid setup {i}");
                        break;
                    }
                    setupId++;
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

    public static void YesNo(Action action)
    {
        MessageBoxResult messageBoxResult = MessageBox.Show("Remove project?", "Delete", MessageBoxButton.YesNoCancel);
        if (messageBoxResult == MessageBoxResult.Yes)
            action.Invoke();
    }
}