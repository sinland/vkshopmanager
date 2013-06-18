using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using VkShopManager.Core;
using VkShopManager.Domain;

namespace VkShopManager
{
    /// <summary>
    /// Interaction logic for CommentsViewWindow.xaml
    /// </summary>
    public partial class CommentsViewWindow : Window
    {
        private readonly Product m_product;
        private struct CommentEntry
        {
            public string Message;
            public string Sender;
            public string Date;
        }

        public CommentsViewWindow(Window owner, Product product)
        {
            m_product = product;

            InitializeComponent();
            Owner = owner;

            LoadComments();
        }

        private void LoadComments()
        {
            var bg = new BackgroundWorker();
            bg.DoWork += (sender, args) =>
                {
                    var repo = Core.Repositories.DbManger.GetInstance().GetCommentsRepository();
                    var res = (from comment in repo.GetForProduct(m_product)
                               where !String.IsNullOrEmpty(comment.Message)
                               select new CommentEntry
                                           {
                                               Sender = comment.SenderName,
                                               Message = comment.Message,
                                               Date = comment.PostingDate
                                           }).ToList();
                    args.Result = res;
                };
            bg.RunWorkerCompleted += (sender, args) =>
                {
                    if (args.Error != null)
                    {
                        this.ShowError(args.Error.Message);
                        return;
                    }

                    var comments = args.Result as List<CommentEntry>;
                    if (comments == null) return;

                    contentTable.RowGroups.Add(new TableRowGroup());
                    
                    for (var i = 0; i < comments.Count; i++)
                    {
                        var row = new TableRow();
                        contentTable.RowGroups[0].Rows.Add(row);

                        row.Background = new SolidColorBrush(new Color(){A = 0xFF, R = 0xCC, G = 0xCC, B = 0xFF});
                        row.FontSize = 14;

                        row.FontWeight = FontWeights.Bold;
                        row.Cells.Add(
                            new TableCell(
                                new Paragraph(new Run(String.Format("{0} ({1})", comments[i].Sender, comments[i].Date)))){Padding = new Thickness(3)});

                        var txtRow = new TableRow();
                        contentTable.RowGroups[0].Rows.Add(txtRow);
                        txtRow.FontSize = 12;
                        txtRow.Cells.Add(new TableCell(new Paragraph(new Run(comments[i].Message))) { Padding = new Thickness(5, 0, 0, 7) });
                    }
                };
            bg.RunWorkerAsync();
        }
    }
}
