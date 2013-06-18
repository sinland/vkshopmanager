using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VkShopManager.Domain;
using VkShopManager.Core;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for AskExportSettingsWindow.xaml
    /// </summary>
    public partial class AskExportSettingsWindow : Window
    {
        public enum Result
        {
            None,
            Ok,
            Cancel
        }

        private readonly RegistrySettings m_settings;
        private readonly Album m_album;
        private readonly string m_filePrefix;
        private Result m_result;
        private ExportFormatterBase m_selectedFormatter;

        public AskExportSettingsWindow(Window owner, Album album, string filePrefix)
        {
            m_album = album;
            m_filePrefix = filePrefix;
            m_result = Result.None;
            m_settings = RegistrySettings.GetInstance();

            InitializeComponent();

            Title = String.Format("Экспорт: {0}", m_album.GetCleanTitle());

            boxFormats.Items.Add(new ReportsExportFormatter(m_album));
            boxFormats.Items.Add(new WordExportFormatter(m_album));
            boxFormats.Items.Add(new PlainTextExportFormatter(m_album));

            cbIncludeEmptyPositions.IsChecked = m_settings.ShowEmptyPositions;
            cbIncludePartialPositions.IsChecked = m_settings.ShowPartialPositions;

            KeyUp += OnKeyUp;

            boxFormats.SelectedIndex = 0;

            Owner = owner;
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                m_result = Result.Cancel;
                Close();
            }
        }

        public Result GetResult()
        {
            return m_result;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            m_result = Result.Cancel;
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (boxFormats.SelectedItem == null)
            {
                this.ShowError("Необходимо выбрать формат данных!");
                return;
            }

            m_selectedFormatter = (ExportFormatterBase) boxFormats.SelectedItem;
            m_selectedFormatter.IsIncludingEmpty = cbIncludeEmptyPositions.IsChecked.HasValue && cbIncludeEmptyPositions.IsChecked.Value;
            m_selectedFormatter.IsIncludingPartial = cbIncludePartialPositions.IsChecked.HasValue && cbIncludePartialPositions.IsChecked.Value;
            if (m_selectedFormatter is IFileExporter)
            {
                ((IFileExporter)m_selectedFormatter).Filename = System.IO.Path.Combine(m_settings.ReportsPath, tbFilename.Text);
            }
            m_result = Result.Ok;
            Close();
        }

        public ExportFormatterBase GetSelectedFormatter()
        {
            return m_selectedFormatter;
        }

        private void boxFormats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (boxFormats.SelectedItem is IFileExporter)
            {
                tbFilename.IsEnabled = true;
                tbFilename.Text = String.Format("{0}-SD0{1:D2}{2:D2}{3:D2}", m_filePrefix, DateTime.Now.Hour,
                                                  DateTime.Now.Minute, DateTime.Now.Second);

                if (boxFormats.SelectedItem is PlainTextExportFormatter)
                {
                    tbFilename.Text += ".txt";
                }
                else if (boxFormats.SelectedItem is WordExportFormatter)
                {
                    tbFilename.Text += ".docx";
                }
                else boxFormats.SelectedItem += ".dat";
            }
            else
            {
                tbFilename.Clear();
                tbFilename.IsEnabled = false;
            }
        }
    }
}
