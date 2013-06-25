using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VkShopManager.Core;
using VkShopManager.Core.Repositories;
using VkShopManager.Domain;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public enum Result
        {
            Cancel,
            Ok
        }

        private readonly RegistrySettings m_settings;
        private Result m_result = Result.Cancel;

        public SettingsWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;

            KeyUp += OnKeyUp;

            m_settings = RegistrySettings.GetInstance();

            tbGalleryPath.Text = m_settings.GalleryPath;
            tbGalleryPath.IsReadOnly = true;
            tbWorkgroupId.Text = m_settings.WorkGroupId.ToString();
            lblTokenLifeTime.Content = m_settings.ExpirationDate.ToString();
            cbEnableAutoClear.IsChecked = m_settings.ClearReportsOnExit;

            var indx = cbCodeNumberTypes.Items.Add(CodeNumberGenerator.Type.NumericOnly);
            if (m_settings.SelectedCodeNumberGenerator == (int) CodeNumberGenerator.Type.NumericOnly)
                cbCodeNumberTypes.SelectedIndex = indx;
            indx = cbCodeNumberTypes.Items.Add(CodeNumberGenerator.Type.AlphaNumeric);
            if (m_settings.SelectedCodeNumberGenerator == (int)CodeNumberGenerator.Type.AlphaNumeric)
                cbCodeNumberTypes.SelectedIndex = indx;


            var bg = new BackgroundWorker();
            bg.DoWork += (sender, args) =>
                {
                    var albums = DbManger.GetInstance().GetAlbumsRepository();
                    var hiddens = m_settings.GetHiddenList();
                    var res = hiddens.Select(albums.GetByVkId).Where(a => a != null).ToList();

                    res.Sort((a1, a2) => String.Compare(a1.Title, a2.Title, StringComparison.CurrentCultureIgnoreCase));
                    args.Result = res;
                };
            bg.RunWorkerCompleted += (sender, args) =>
                {
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }

                    var list = (List<Album>) args.Result;
                    foreach (Album album in list)
                    {
                        lbHiddenAlbums.Items.Add(album);
                    }
                };
            bg.RunWorkerAsync();
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.Escape)
            {
                Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            int gid;
            try
            {
                gid = Int32.Parse(tbWorkgroupId.Text);
            }
            catch
            {
                this.ShowError("ID группы задан некорректно");
                return;
            }
            
            m_settings.WorkGroupId = gid;
            m_result = Result.Ok;
            m_settings.ClearReportsOnExit = cbEnableAutoClear.IsChecked.HasValue && cbEnableAutoClear.IsChecked.Value;
            m_settings.SelectedCodeNumberGenerator = (int) (CodeNumberGenerator.Type) cbCodeNumberTypes.SelectedItem;

            Close();
        }

        private void btnCreateNewDb_Click(object sender, RoutedEventArgs e)
        {
            if (!this.ShowQuestion("Уверены, что хотите пересоздать БД?")) return;

            try
            {
                var db = DbManger.GetInstance();
                db.SetupContext();
                this.ShowMessage("Операция выполнена успешно.");

                var rates = db.GetRatesRepository();
                for (int i = 0; i < 100; i++)
                {
                    rates.Add(new ManagedRate { Comment = String.Format("{0}%", i), Rate = i });    
                }
            }
            catch (Exception)
            {
                this.ShowError("Ошибка. Не удалось создать БД");
            }
        }

        public Result GetResult()
        {
            return m_result;
        }

        private void btnRemoveAlbumFromHiddens_Click(object sender, RoutedEventArgs e)
        {
            var album = lbHiddenAlbums.SelectedItem as Album;
            if (album == null) return;
            var hiddens = m_settings.GetHiddenList();
            hiddens.Remove(album.VkId);
            m_settings.SetHiddenList(hiddens);
            lbHiddenAlbums.Items.Remove(album);

            m_result = Result.Ok;
        }
    }
}
