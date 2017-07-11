using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using IO = System.IO;
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
using System.Reflection;
using System.ComponentModel;
using MagickCanvas.Properties;

namespace MagickCanvas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string magick;
        private readonly string fileFilter;
        private readonly string imagePath;
        private readonly string scriptPath;
        private readonly object runScriptLock;
        private readonly string tempFilePrefix;

        private double outputScrollVerticalOffset = 0;
        private double outputScrollHorizontalOffset = 0;
        private Point outputScrollMousePoint = new Point();

        private bool __unsavedChanges;
        private bool unsavedChanges
        {
            get { return __unsavedChanges; }
            set
            {
                if (value == true)
                {
                    if (string.IsNullOrEmpty(workingFile))
                    {
                        Title = "Magick (unsaved)";
                    }
                    else
                    {
                        Title = "Magick - " + workingFile + " (unsaved)";
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(workingFile))
                    {
                        Title = "Magick";
                    }
                    else
                    {
                        Title = "Magick - " + workingFile;
                    }
                }

                __unsavedChanges = value;
            }
        }

        private string workingDirectory
        {
            get
            {
                return Settings.Default.WorkingDirectory;
            }
            set
            {
                Settings.Default.WorkingDirectory = value;
                Settings.Default.Save();
            }
        }

        private string workingFile
        {
            get
            {
                return Settings.Default.WorkingFile;
            }
            set
            {
                Settings.Default.WorkingFile = value;
                Settings.Default.Save();
            }
        }

        public MainWindow()
        {
            tempFilePrefix = "MagickCanvas.";
            magick = AppDomain.CurrentDomain.BaseDirectory + @"imagemagick\magick.exe";
            fileFilter = "Magick Canvas file (*.mkc)|*.mkc";
            imagePath = GetTempFilePath(".png");
            scriptPath = GetTempFilePath(".cmd");
            runScriptLock = new object();
            
            InitializeComponent();
        }

        private string GetTempFilePath(string extension)
        {
            var filename = tempFilePrefix + IO.Path.GetRandomFileName();
            filename = IO.Path.ChangeExtension(filename, extension);
            return IO.Path.Combine(IO.Path.GetTempPath(), filename);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClearEditorText();
            ClearConsoleText();

            if (IO.File.Exists(workingFile))
            {
                SetEditorText(IO.File.ReadAllText(workingFile));
            }
            else
            {
                workingFile = string.Empty;
                workingDirectory = string.Empty;
            }

            unsavedChanges = false;

            Editor.Focus();
            
            Dispatcher.BeginInvoke(new Action(() => { CleanupTempFiles(); }));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (unsavedChanges)
            {
                ShowSaveConfirmation();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {            
            CleanupTempFiles();   
        }

        private void CleanupTempFiles()
        {
            foreach (var file in IO.Directory.EnumerateFiles(IO.Path.GetTempPath(), tempFilePrefix + "*"))
            {
                IO.File.Delete(file);
            }
        }

        private void New(object sender, ExecutedRoutedEventArgs e)
        {
            if (unsavedChanges)
            {
                ShowSaveConfirmation();
            }

            ClearEditorText();
            workingFile = string.Empty;
            workingDirectory = string.Empty;
            unsavedChanges = false;
        }

        private void ShowSaveConfirmation()
        {
            var caption = "Save confirmation";
            var message = "Do you want to save the changes?";
            if (MessageBox.Show(message, caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (string.IsNullOrEmpty(workingFile))
                {
                    ShowSaveDialog();
                }

                SaveWorkingFile();
            }
        }

        private void Open(object sender, ExecutedRoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Multiselect = false;
            openFileDialog.Filter = fileFilter;

            if (openFileDialog.ShowDialog() == true)
            {
                if (unsavedChanges)
                {
                    ShowSaveConfirmation();
                }

                workingFile = openFileDialog.FileName;
                workingDirectory = IO.Path.GetDirectoryName(openFileDialog.FileName);
                SetEditorText(IO.File.ReadAllText(openFileDialog.FileName));
                unsavedChanges = false;
            }
        }

        private void Save(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(workingFile))
            {
                ShowSaveDialog();
            }

            SaveWorkingFile();
        }
        
        private void SaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            ShowSaveDialog();
            SaveWorkingFile();
        }

        private void ShowSaveDialog()
        {
            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = fileFilter;

            if (saveFileDialog.ShowDialog() == true)
            {
                workingFile = saveFileDialog.FileName;
            }
        }

        private void SaveWorkingFile()
        {
            if (!string.IsNullOrEmpty(workingFile))
            {
                IO.File.WriteAllText(workingFile, GetEditorText());
                unsavedChanges = false;
            }
        }

        private void RunScript(object sender, ExecutedRoutedEventArgs e)
        {
            lock (runScriptLock)
            {
                if (IsExecuting())
                {
                    return;
                }

                IsExecuting(true);
            }

            var script = GetScript();

            IO.File.WriteAllText(scriptPath, script);

            if (ShowScript.IsChecked)
            {
                WriteToConsole(script);
            }
            
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    Arguments = "",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? IO.Path.GetTempPath() : workingDirectory
                };

                process.Start();
                process.WaitForExit();

                WriteToConsole(process.StandardOutput.ReadToEnd());
                WriteToConsole(process.StandardError.ReadToEnd());
            }
            
            DisplayOuput(imagePath);

            IsExecuting(false);

            IO.File.Delete(scriptPath);
            IO.File.Delete(imagePath);
        }

        private string GetScript()
        {
            var lines = GetEditorText().Split(new[] { "\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);

            var builder = new StringBuilder();

            foreach (var item in lines)
            {
                var line = item.Trim();

                if (line != string.Empty && !line.StartsWith("#"))
                {
                    builder.Append(line).Append(" ");
                }
            }

            var script = magick + " " + builder + imagePath;
            
            return "@echo off\r\nchcp 65001>nul\r\n" + script;
        }

        private void DisplayOuput(string path)
        {
            if (IO.File.Exists(path))
            {
                var image = new BitmapImage();
                using (var stream = IO.File.OpenRead(path))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                
                Output.Source = image;
            }
        }

        private void ClearConsole(object sender, ExecutedRoutedEventArgs e)
        {
            ClearConsoleText();
        }

        private void ClearConsoleText()
        {
            Console.Document.Blocks.Clear();
        }

        private void WriteToConsole(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Console.Document.Blocks.Add(new Paragraph(new Run(text)));
            Console.ScrollToEnd();
        }

        private void ClearEditorText()
        {
            Editor.Document.Blocks.Clear();
        }

        private string GetEditorText()
        {
            var document = Editor.Document;
            var textRange = new TextRange(document.ContentStart, document.ContentEnd);
            return textRange.Text;
        }

        private void SetEditorText(string text)
        {
            ClearEditorText();
            Editor.Document.Blocks.Add(new Paragraph(new Run(text)));
        }

        private void IsExecuting(bool value)
        {
            Executing.IsIndeterminate = value;
        }

        private bool IsExecuting()
        {
            return Executing.IsIndeterminate;
        }
        
        private void OutputScroll_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (IsScrollOutputEnabled())
                {
                    OutputScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    OutputScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                }
                else
                {
                    OutputScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    OutputScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                }
            }
            else
            {
                outputScrollMousePoint = e.GetPosition(OutputScroll);
                outputScrollVerticalOffset = OutputScroll.VerticalOffset;
                outputScrollHorizontalOffset = OutputScroll.HorizontalOffset;
                OutputScroll.CaptureMouse();
            }
        }
        
        private void OutputScroll_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (OutputScroll.IsMouseCaptured)
            {
                var position = e.GetPosition(OutputScroll);
                OutputScroll.ScrollToVerticalOffset(outputScrollVerticalOffset + (outputScrollMousePoint.Y - position.Y));
                OutputScroll.ScrollToHorizontalOffset(outputScrollHorizontalOffset + (outputScrollMousePoint.X - position.X));
            }
        }

        private void OutputScroll_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OutputScroll.ReleaseMouseCapture();
        }

        private bool IsScrollOutputEnabled()
        {
            return OutputScroll.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            unsavedChanges = true;
        }

        private void Exit(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
    }
}
