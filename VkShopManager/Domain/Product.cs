using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VkShopManager.Annotations;

namespace VkShopManager.Domain
{
    public class Product : INotifyPropertyChanged
    {
        private string m_title;
        private decimal m_price;
        private int m_minAmount;
        private string m_genericUrl;
        private string m_codeNumber;

        public virtual int Id { get; set; }
        public virtual long VkId { get; set; }
        public virtual int AlbumId { get; set; }
        public virtual string GenericUrl
        {
            get { return m_genericUrl; }
            set { m_genericUrl = value; OnPropertyChanged("GenericUrl"); }
        }
        public virtual string Title
        {
            get { return m_title; }
            set { m_title = value; OnPropertyChanged("Title"); }
        }
            public virtual decimal Price
        {
            get { return m_price; }
            set { m_price = value; OnPropertyChanged("Price"); }
        }
        public virtual int MinAmount
        {
            get { return m_minAmount; }
            set { m_minAmount = value; OnPropertyChanged("MinAmount"); }
        }

        public string ImageFile { get; set; }

        /// <summary>
        /// Артикул
        /// </summary>
        public virtual string CodeNumber
        {
            get { return m_codeNumber; }
            set { m_codeNumber = value; OnPropertyChanged("CodeNumber"); }
        }

        public Product()
        {

        }

        public override string ToString()
        {
            return (Title.Length > 0) ? Title : base.ToString();
        }
        
        public Product CopyToAlbum(Album album)
        {
            var p = new Product
            {
                AlbumId = album.Id,
                GenericUrl = this.GenericUrl,
                ImageFile = this.ImageFile,
                MinAmount = this.MinAmount,
                Price = this.Price,
                Title = this.Title,
                VkId = this.VkId,
                CodeNumber = this.CodeNumber
            };
            return p;
        }

        public void ParsePhotoDescription(string description)
        {
            var parts = description.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);

            int subject = 0;
            for (int k = 0; k < parts.Length; k++)
            {
                parts[k] = parts[k].Trim();
                if (String.IsNullOrEmpty(parts[k])) continue;
                if (subject == 2 && !parts[k].Contains("р.")) continue;
                if (subject == 3 && !(parts[k].Contains("шт") || parts[k].Contains("набор"))) continue; // + упак

                if (subject == 0)
                {
                    GenericUrl = parts[k];
                }
                else if (subject == 1)
                {
                    Title = parts[k];
                }
                else if (subject == 2)
                {
                    // цена
                    parts[k] = parts[k].Replace('.', ',');
                    for (int n = 0; n < parts[k].Length; n++)
                    {
                        if (!Char.IsDigit(parts[k][n]) && parts[k][n] != ',')
                        {
                            parts[k] = parts[k].Replace(parts[k][n], ' ');
                        }
                    }
                    parts[k] = parts[k].Replace(" ", "");
                    parts[k] = parts[k].Trim(',');
                    try
                    {
                        Price = Decimal.Parse(parts[k]);
                    }
                    catch
                    {

                    }
                }
                else if (subject == 3)
                {
                    // кол-во
                    for (int n = 0; n < parts[k].Length; n++)
                    {
                        if (!Char.IsDigit(parts[k][n]))
                        {
                            parts[k] = parts[k].Replace(parts[k][n], ' ');
                        }
                    }
                    parts[k] = parts[k].Replace(" ", "");

                    try
                    {
                        MinAmount = Int32.Parse(parts[k]);
                    }
                    catch (Exception)
                    {

                    }
                }
                subject++;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
