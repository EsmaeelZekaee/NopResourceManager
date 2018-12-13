using Fluent;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using System.Xml;
using System.Xml.Serialization;
using Button = System.Windows.Controls.Button;

namespace ResUtility
{
    public class DataContext : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<Resource> _resources;
        private Resource _defaultResource;


        public Resource DefaultResource
        {
            get => _defaultResource; set
            {

                _defaultResource = value;
                OnPropertyChanged("DefaultResource");
                OnPropertyChanged("SelectedIndex");
            }
        }
        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex; set
            {
                _selectedIndex = value;
                OnPropertyChanged("SelectedIndex");
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public ObservableCollection<Resource> Resources
        {
            get => _resources; set
            {
                _resources = value;
                OnPropertyChanged("Resources");
            }
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private class EncodingItem
        {
            public Encoding EncodingInfo { get; set; }

            public override string ToString()
            {
                if (EncodingInfo == Encoding.Default)
                    return "Default";
                return EncodingInfo.EncodingName;
            }
        }

        private ObservableCollection<EncodingItem> _encodingsCollection = new ObservableCollection<EncodingItem>();

        private DataContext _objContext = new DataContext();
        public MainWindow()
        {
            InitializeComponent();

            _objContext.Resources = new ObservableCollection<Resource>();
            this.DataContext = _objContext;
            Resources.ItemsSource = _objContext.Resources;
            _encodingsCollection.Add(new EncodingItem()
            {
                EncodingInfo = Encoding.Default
            });
            _encodingsCollection.Add(new EncodingItem()
            {
                EncodingInfo = Encoding.ASCII
            });
            _encodingsCollection.Add(new EncodingItem()
            {
                EncodingInfo = Encoding.UTF8
            });
            _encodingsCollection.Add(new EncodingItem()
            {
                EncodingInfo = Encoding.Unicode
            });
            Encodings.ItemsSource = _encodingsCollection;
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _objContext.DefaultResource = LoadResource(System.IO.Path.Combine(path, "defualt.xml"));
            if (_objContext.DefaultResource != null)
                _objContext.Resources.Add(_objContext.DefaultResource);
        }

        public Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }
        public Encoding GetFileEncoding(String fileName)
        {
            Encoding result = null;
            var fi = new FileInfo(fileName);
            FileStream fs = null;

            try
            {
                fs = fi.OpenRead();
                Encoding[] unicodeEncodings = { Encoding.BigEndianUnicode, Encoding.Unicode, Encoding.UTF8 };
                for (var i = 0; result == null && i < unicodeEncodings.Length; i++)
                {
                    fs.Position = 0;
                    var preamble = unicodeEncodings[i].GetPreamble();
                    var preamblesAreEqual = true;
                    for (var j = 0; preamblesAreEqual && j < preamble.Length; j++)
                    {
                        preamblesAreEqual = preamble[j] == fs.ReadByte();
                    }
                    if (preamblesAreEqual)
                    {
                        result = unicodeEncodings[i];
                    }
                }
            }
            catch (System.IO.IOException)
            {
            }
            finally
            {
                fs?.Close();
            }

            return result ?? (result = Encoding.Default);
        }
        private Resource LoadResource(string path)
        {
            //  var encode = GetEncoding(path);
            using (var resFile = new StreamReader(path))
            {
                var xmlSerializer = new XmlSerializer(typeof(Resource));
                var resource = (Resource)xmlSerializer.Deserialize(XmlReader.Create(resFile));
                for (var i = 0; i < resource.LocaleResources.Count; i++)
                {
                    resource.LocaleResources[i].Index = i + 1;
                    resource.LocaleResources[i].ResetUndo();
                }
                resource.FileInfo = new FileInfo(path);
                var dir = System.IO.Path.GetDirectoryName(path);
                return resource;
            }
        }
        private string GetFileText(string path)
        {
            var text = File.ReadAllText(path, GetEncoding(path));

            return text;
        }

        private Resource SaveResource(Resource resource, string path)
        {
            using (var resFile = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                resFile.SetLength(0);
                var endocding = (Encodings.SelectedItem as EncodingItem) ?? new EncodingItem { EncodingInfo = Encoding.UTF8 };
                var xmlWriterSettings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = endocding.EncodingInfo
                };
                var xmlSerializer = new XmlSerializer(typeof(Resource));
                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                xmlSerializer.Serialize(System.Xml.XmlWriter.Create(resFile, xmlWriterSettings), resource, namespaces: namespaces);
                resource.FileInfo = new FileInfo(path);
                return resource;
            }
        }

        private void ResourcesItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddResource_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog()
            {
                Filter = "Resource file |*.xml"
            };
            if (fileDialog.ShowDialog() == true)
            {
                try
                {
                    var res = LoadResource(fileDialog.FileName);
                    var old = _objContext.Resources.SingleOrDefault(x => x.FileInfo.FullName == fileDialog.FileName);
                    if (old != null)
                    {
                        if (MessageBox.Show("This file already loaded. your changes will be lost, do you want to reload this file?") == MessageBoxResult.No)
                            return;
                        old = res;
                        return;
                    }
                    else
                        _objContext.Resources.Add(res);

                    if (_objContext.DefaultResource == null)
                    {
                        _objContext.DefaultResource = res;
                        return;
                    }
                    if (MessageBox.Show("Do you want to merge keys?", "Merge", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        var addedItemsCount = 0;
                        foreach (var item in res.LocaleResources)
                        {
                            if (_objContext.DefaultResource.LocaleResources.Any(x => x.Name == item.Name) == false)
                            {
                                _objContext.DefaultResource.LocaleResources.Add(item);
                                addedItemsCount++;
                            }
                        }
                        _objContext.DefaultResource.Update();
                        MessageBox.Show($"{addedItemsCount} new item(s) added to default source.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResourcesItemsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_objContext.DefaultResource == null)
                return;
            var item = e.NewValue as ResourceItem;
            var view = CollectionViewSource.GetDefaultView(LangItems.Items);
            LangItems.SelectedItem = _objContext.DefaultResource.LocaleResources.SingleOrDefault(x => item != null && x.Name == item?.Path);
            if (LangItems.SelectedItem != null)
                LangItems.ScrollIntoView(LangItems.SelectedItem);
        }

        private void Resources_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _objContext.DefaultResource = (Resources.SelectedItem as Resource);
        }

        private void RtlButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlowDirection(FlowDirection.RightToLeft);
        }

        private void LtrButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeFlowDirection(FlowDirection.LeftToRight);
        }

        private void ChangeFlowDirection(FlowDirection flowDirection)
        {
            var textBox = GetTextBox();
            if (textBox == null)
                return;
            textBox.FlowDirection = flowDirection;
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (LangItems.SelectedItem == null)
                return;
            var name = (LangItems.SelectedItem as LocaleResource).Name;
            if (name == null)
                return;
            var chWin = new ChangeWindow();
            var selected = "";
            chWin.ValuesList.ItemsSource = _objContext.Resources.SelectMany(x => x.LocaleResources).Where(x => x.Name == name).Select(x => x.Value);
            chWin.ValuesList.SelectionChanged += (o, v) =>
            {
                selected = chWin.ValuesList.SelectedItem as string;
            };
            if (chWin.ShowDialog() == true)
            {
                var textBox = GetTextBox();
                textBox.Text = selected;
                textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty).UpdateSource();

            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetLangGuid(sender);
            _objContext.Resources.Where(x => x.Guid == guid);
            SaveResource(_objContext.DefaultResource, _objContext.DefaultResource.FileInfo.FullName);
        }

        private Guid? GetLangGuid(object sender)
        {
            return (sender as System.Windows.Controls.Button).Tag as Guid?;
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var res = _objContext.DefaultResource;
            var saveFileDialog = GetFileName(res.Name);
            if (saveFileDialog.ShowDialog() == true)
            {
                var guid = GetLangGuid(sender);
                _objContext.Resources.Where(x => x.Guid == guid);
                // if (System.IO.File.Exists(saveFileDialog.FileName) == false || MessageBox.Show("The filename file already exists. Do you want to overwrite it?") == MessageBoxResult.Yes)
                try
                {
                    SaveResource(res, saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }

        }

        private SaveFileDialog GetFileName(string defName)
        {
            return new SaveFileDialog()
            {
                FileName = $"{defName}.xml",
                Filter = $"{defName}.xml|*.xml"
            };
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            var line = 0;
            int.TryParse(GoTextBox.Text, out line);
            LangItems.SelectedIndex = line;
            LangItems.ScrollIntoView(LangItems.SelectedItem);
        }

        private void TranslateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var textBox = GetTextBox();
            if (textBox == null)
                return;

            var translateWindow = new TranslateWindow();
            translateWindow.TranslatedTextBox.Tag = translateWindow.OrginalTextBox.Text = textBox.Text;
            if (translateWindow.ShowDialog() == true)
            {
                if (!(translateWindow.TranslatedTextBox.Tag is null))
                {
                    textBox.Text = (string)translateWindow.TranslatedTextBox.Tag;
                    textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty).UpdateSource();
                }
            }
        }

        private void ConnectionButton_OnClick(object sender, RoutedEventArgs e)
        {
            var connectionWindow = new ConnectionWindow();
            if (connectionWindow.LoadData() == true)
            {
                if (connectionWindow.Resource != null)
                {
                    _objContext.Resources.Add(connectionWindow.Resource);
                }
            }
        }

        private void UpdateDatabase_Click(object sender, RoutedEventArgs e)
        {
            var guid = GetLangGuid(sender);
            var connectionWindow = new ConnectionWindow();
            connectionWindow.Resource = _objContext.Resources.Single(x => x.Guid == guid);
            if (connectionWindow.SaveData() == true)
            {

            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (MessageBox.Show("Are you sure you want to exit?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private T GetFrameworkElementByName<T>(FrameworkElement referenceElement) where T : FrameworkElement
        {
            FrameworkElement child = null;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(referenceElement); i++)
            {
                child = VisualTreeHelper.GetChild(referenceElement, i) as FrameworkElement;
                System.Diagnostics.Debug.WriteLine(child);
                if (child != null && child.GetType() == typeof(T))

                { break; }

                if (child == null) continue;
                child = GetFrameworkElementByName<T>(child);
                if (child != null && child.GetType() == typeof(T))
                {
                    break;
                }

            }
            return child as T;
        }

        private System.Windows.Controls.TextBox GetTextBox()
        {
            if (LangItems.SelectedIndex == -1)
                return null;
            var item = LangItems.ItemContainerGenerator.ContainerFromIndex(LangItems.SelectedIndex) as Control;
            System.Windows.Controls.TextBox valueEditor = null;
            if (item != null)
            {
                //get the item's template parent
                var templateParent = GetFrameworkElementByName<ContentPresenter>(item);
                //get the DataTemplate that TextBlock in.
                var dataTemplate = LangItems.ItemTemplate;
                if (dataTemplate != null && templateParent != null)
                {
                    valueEditor = dataTemplate.FindName("ValueEditor", templateParent) as System.Windows.Controls.TextBox;
                }
            }
            return valueEditor;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            GetTextBox()?.Undo();
        }

        private void RedButton_Click(object sender, RoutedEventArgs e)
        {
            GetTextBox()?.Redo();
        }

        private void ValueEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            LangItems.SelectedItem = (((System.Windows.Controls.TextBox)sender)).GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.DataItem;
        }

        private void CloseDocument_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(((Button)sender).Tag is Resource item))
                return;
            _objContext.Resources.Remove(item);
            if (_objContext.DefaultResource?.Guid != item.Guid) return;
            _objContext.DefaultResource = _objContext.Resources.FirstOrDefault();
            _objContext.SelectedIndex = _objContext.Resources.IndexOf(_objContext.DefaultResource);
        }

        private void ExportNode_Click(object sender, RoutedEventArgs e)
        {
            if (!(((Control)sender).Tag is ResourceItem item))
            {
                return;
            }
            Resource resource = new Resource()
            {
                Name = _objContext.DefaultResource.Name,
                LocaleResources = new ObservableCollection<LocaleResource>(_objContext.DefaultResource.LocaleResources.Where(x => x.Name.StartsWith(item.Path)))
            };
            var openFile = GetFileName(item.Path);
            if (openFile.ShowDialog() == true)
            {
                SaveResource(resource, openFile.FileName);
            }
        }


        private void ScanButton_Click(object sender, RoutedEventArgs e)

        {
            var oldPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Resutility", "OldPath", "") as string;

            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = oldPath;
            var rslt = folderBrowserDialog.ShowDialog();
            if (rslt == System.Windows.Forms.DialogResult.OK)
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Resutility", "OldPath", folderBrowserDialog.SelectedPath);
                ScanButtonMethod(folderBrowserDialog.SelectedPath);
            }
        }

        private Regex CSharpPattern { get; set; }
        private Regex RazorPattern { get; set; }
        private Regex RazorPattern1 { get; set; }

        private List<String> Items { get; set; }

        private void ScanButtonMethod(string path)

        {
            CSharpPattern = new Regex("\\[NopResourceDisplayName( )*?\\(( )*?\"(\\w.*)\"( )*?\\)( )*?\\]");
            RazorPattern = new Regex("T\\(\\\"((\\w*\\.\\w*)*)\\\"\\)");
            RazorPattern1 = new Regex("GetResource\\(\\\"((\\w*\\.\\w*)*)\\\"\\)");
            //GetResource("")
            Items = new List<string>();
            TaskFactory factory = new TaskFactory();
            factory.StartNew(() =>
            {
                ScanFolder(path);
            }).ContinueWith(x =>
            {
                Items = Items.Distinct().ToList();


            }).Wait();

            Resource resource = new Resource();
            resource.Name = "English";
            foreach (var item in Items.Select((v, i) => new LocaleResource()
            {
                Value = v,
                Name = v,
                Index = i,
            }))
            {
                resource.LocaleResources.Add(item);
            }
            _objContext.Resources.Add(resource);
            MessageBox.Show($"{Items.Count} items found!");
        }


        private void ScanFolder(string selectedPath)
        {
            Parallel.ForEach(Directory.GetDirectories(selectedPath), dir => { ScanFolder(dir); });

            var csharp = Directory.GetFiles(selectedPath, "*.cs");
            var razor = Directory.GetFiles(selectedPath, "*.cshtml");
            var allFiles = csharp.Concat(razor).Select(x => new FileInfo(x));
            foreach (var item in allFiles)
            {
                //Console.WriteLine($"Scaning: {item.FullName}");
                ScanBody(item);
            }
        }

        private void ScanBody(FileInfo file)
        {
            var text = File.ReadAllText(file.FullName);

            foreach (Match item in CSharpPattern.Matches(text))
            {
                if (!item.Success)
                {
                    continue;
                }
                Items.Add(item.Groups[3].Value);
            }

            foreach (Match item in RazorPattern.Matches(text))
            {
                Items.Add(item.Groups[1].Value);
            }
            foreach (Match item in RazorPattern1.Matches(text))
            {
                Items.Add(item.Groups[1].Value);
            }
        }

        private void FixNames_Click(object sender, RoutedEventArgs e)
        {
            _objContext.DefaultResource.LocaleResources = new ObservableCollection<LocaleResource>(_objContext.DefaultResource.LocaleResources.GroupBy(x => x.Name).Select(g => g.First())); ;
        }
    }
}
