using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
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
using System.Windows.Shapes;
using Microsoft.SqlServer.Server;
using ResUtility.Nop;

namespace ResUtility
{
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : Window
    {
        private NopDataContext _dbContext;

        public ConnectionWindow()
        {
            InitializeComponent();
            ConnectionString.Text = Microsoft.Win32.Registry.GetValue(Microsoft.Win32.Registry.CurrentUser + "\\RegUtility\\Connection", "History","") as string;
        }

        private void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_dbContext == null)
            {

                return;
            }
            var language = Languages.SelectedItem as Language;

            if (language == null)
            {
                return;
            }
            var localeStringResources = language.LocaleStringResources;
            Resource = new Resource();
            Resource.Name = language.Name;
            Resource.LocaleResources = new ObservableCollection<LocaleResource>(
                localeStringResources.Select((x, i) => new LocaleResource()
                {
                    Name = x.ResourceName,
                    Value = x.ResourceValue,
                    Index = i + 1
                }));

            Resource.FileInfo = new FileInfo($"{language.Name}_{Resource.Guid}.xml");

            MessageBox.Show($"Language = {Resource.Name} \r\n{Resource.LocaleResources.Count} Items loaded!");

            DialogResult = true;
            this.Close();
        }

        public Resource Resource { get; set; }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Connect()
        {
            try
            {
                _dbContext = new NopDataContext(ConnectionString.Text);
                var langs = _dbContext.Languages.ToList();
                Languages.ItemsSource = langs;
                Microsoft.Win32.Registry.SetValue(Microsoft.Win32.Registry.CurrentUser + "\\RegUtility\\Connection", "History", ConnectionString.Text);
                MessageBox.Show("connected successfully to database.");
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }
        private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
        {
            Connect();
        }
        public bool? LoadData()
        {
            this.Title = "Load resource from database";
            LoadButton.Visibility = Visibility.Visible;
            return ShowDialog();
        }

        public bool? SaveData()
        {
            this.Title = "Save the resource to database";
            SaveButton.Visibility = Visibility.Visible;
            return ShowDialog();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_dbContext == null)
            {

                return;
            }
            var language = Languages.SelectedItem as Language;

            if (language == null)
            {
                return;
            }

            if (Resource == null)
            {
                return;
            }

            var uq = (from r in Resource.LocaleResources
                      join l in language.LocaleStringResources
                      on r.Name equals l.ResourceName
                      select new { r, l }).Where(x => x.l.ResourceValue != x.r.Value).Select((x) =>
                          {
                              x.l.ResourceValue = x.r.Value;
                              return x.l;
                          }).ToList();
            var names = language.LocaleStringResources.Select(x => x.ResourceName);
            var nq = Resource.LocaleResources.Where(x => names.Any(y => y == x.Name) == false).Select(x => new LocaleStringResource()
            {
                ResourceName = x.Name,
                LanguageId = language.Id,
                ResourceValue = x.Value
            }).ToList();
            _dbContext.Configuration.ValidateOnSaveEnabled = false;
            foreach (var item in uq)
            {
                _dbContext.Entry(item).State = EntityState.Modified;
            }

            foreach (var item in nq)
            {
                _dbContext.LocaleStringResources.Add(item);
            }
            var rows = await _dbContext.SaveChangesAsync();

            MessageBox.Show($"{rows} row(s) updated successfully!");

            DialogResult = true;
            this.Close();
        }
    }
}
