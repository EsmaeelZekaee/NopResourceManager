using System;
using System.Collections.Generic;
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
using YandexTranslateCSharpSdk;

namespace ResUtility
{
    /// <summary>
    /// Interaction logic for TranslateWindow.xaml
    /// </summary>
    public partial class TranslateWindow : Window
    {
        public TranslateWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OkeyButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private async void TranslateButton_OnClick(object sender, RoutedEventArgs e)
        {
            LanguageImageAwesome.Spin = true;
            YandexTranslateSdk wrapper = new YandexTranslateSdk();
            wrapper.ApiKey = "trnsl.1.1.20180416T100013Z.1af4661b948d09f6.123be2fc345c3411b70cc3d73e84b563f4345cdb";
            string englishText = OrginalTextBox.Text;
            string translatedText = await wrapper.TranslateText(englishText, "en-fa");
            TranslatedTextBox.Text = translatedText;
            LanguageImageAwesome.Spin = false;
        }
    }
}
