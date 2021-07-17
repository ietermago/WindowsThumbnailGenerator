﻿using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Thumbnail_Generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string[] supportedFiles = {
            "jpg", "jpeg", "png", "mp4", "mov", "wmv", "avi", "mkv"
        };

        private volatile int progressCount;
        private volatile float progressPercentage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void generateThumbnailsForFolder(string rootFolder, int fileCount, bool recursive)
        {
            progressCount = 0;
            progressPercentage = 0;

            startBtn.IsEnabled = false;
            startBtn.Visibility = Visibility.Hidden;
            currentProgress.Visibility = Visibility.Visible;
            progressLabel.Visibility = Visibility.Visible;

            string[] pathList = { rootFolder };

            if (recursive)
            {
                pathList = pathList.Concat(Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories)).ToArray();
            }

            await Task.Run(() => processParallel(pathList, fileCount));

            startBtn.IsEnabled = true;
            startBtn.Visibility = Visibility.Visible;
            currentProgress.Visibility = Visibility.Hidden;
            progressLabel.Visibility = Visibility.Hidden;
            currentProgress.Value = 0;
            progressLabel.Content = "0%";
        }

        private void processParallel(string[] pathList, int fileCount)
        {
            _ = Parallel.ForEach(pathList, directory =>
              {

                  string iconLocation = Path.Combine(directory, "thumb.ico");
                  ImageHandler imageHandler = new();
                  unsetSystem(iconLocation);

                  string[] fileList = { };
                  foreach (string fileFormat in supportedFiles)
                  {
                      fileList = fileList.Concat(Directory.GetFiles(directory, "*." + fileFormat)).ToArray();
                  }

                  if (fileList.Length <= 0) return;
                  if (fileList.Length > fileCount) fileList = fileList.Take(fileCount).ToArray();

                  imageHandler.generateThumbnail(fileList, iconLocation);

                  setSystem(iconLocation);
                  applyFolderIcon(directory, iconLocation);

                  progressCount++;
                  progressPercentage = (float)progressCount / pathList.Length * 100;

                  Dispatcher.Invoke(new Action(() =>
                  {
                      currentProgress.Value = progressPercentage;
                      progressLabel.Content = string.Format("{0:0.##}", progressPercentage) + "%";
                  }));

              });
        }

        private void applyFolderIcon(string targetFolderPath, string iconFilePath)
        {
            var iniPath = Path.Combine(targetFolderPath, "desktop.ini");
            unsetSystem(iniPath);

            // Writes desktop.ini Contents
            var iniContents = new StringBuilder()
                .AppendLine("[.ShellClassInfo]")
                .AppendLine("IconResource=" + iconFilePath + ",0")
                .AppendLine("IconFile=" + iconFilePath)
                .AppendLine("IconIndex=0")
                .AppendLine("[ViewState]")
                .AppendLine("Mode=")
                .AppendLine("Mode=")
                .AppendLine("Vid=")
                .ToString();

            File.WriteAllText(iniPath, iniContents);

            // Set Folder SYSTEM flag, to show thumbnail
            File.SetAttributes(
                targetFolderPath,
                FileAttributes.ReadOnly);

            setSystem(iniPath);
        }

        private void unsetSystem(string targetPath)
        {
            if (File.Exists(targetPath))
            {
                // Make Read Writable
                File.SetAttributes(
                   targetPath,
                   File.GetAttributes(targetPath) &
                   ~(FileAttributes.Hidden | FileAttributes.System));
            }
        }

        private void setSystem(string targetPath)
        {
            File.SetAttributes(
               targetPath,
               File.GetAttributes(targetPath) | FileAttributes.Hidden | FileAttributes.System);
        }

        private void browseBtn_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowser = new();
            if (!folderBrowser.ShowDialog().GetValueOrDefault())
            {
                return;
            }
            targetFolder.Text = folderBrowser.SelectedPath;
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            if (targetFolder.Text.Length <= 0)
            {
                System.Windows.Forms.MessageBox.Show("You didn't choose a folder!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            } else if (!Directory.Exists(targetFolder.Text))
            {
                System.Windows.Forms.MessageBox.Show("The directory you chose does not exist!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            generateThumbnailsForFolder(targetFolder.Text, 3, recursiveChk.IsChecked.GetValueOrDefault());
        }
    }
}
