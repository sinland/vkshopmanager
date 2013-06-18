using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VkShopManager.Core.Repositories;
using VkShopManager.Domain;


namespace VkShopManager.Core
{
    internal class InternalTests
    {
        public void DbManagerTest()
        {
            var db = DbManger.GetInstance();
            var repo = db.GetCustomersRepository();
            repo.Add(new Customer
                {
                    VkId = 1,
                    FirstName = "aaaaaa",
                    LastName = "bbbbbb"
                });
        }

        public void DbCreationTest()
        {
            var db = DbManger.GetInstance();
            db.SetupContext();
        }

        public void AlbumRepositoryTests()
        {
            var db = DbManger.GetInstance();
            var mgr = db.GetAlbumsRepository();

            var album1 = new Album()
                {
                    CreationDate = DateTime.Now,
                    VkId = 6666,
                    ThumbImg = "asd",
                    Title = "Title"
                };
            mgr.Add(album1);

            if(album1.Id == 0) throw new Exception("Save operation failed");

            album1.Title = "Title Updated";
            mgr.Update(album1);

            var album2 = mgr.GetById(album1.Id);
            if(album2.Title != album1.Title) throw new Exception("GetById failed");

            var album3 = mgr.GetByVkId(6666);
            if (album3.Title != album1.Title) throw new Exception("GetById failed");

            album3.Title = "Title ReUpdated";
            mgr.Update(album3);
            
            mgr.Delete(album1);
        }
        public void OrderRepositoryTests()
        {
            var db = DbManger.GetInstance();
            var orders = db.GetOrderRepository();
            var products = db.GetProductRepository();
            var albums = db.GetAlbumsRepository();
            var customers = db.GetCustomersRepository();

            var user1 = new Customer {FirstName = "aaaa", LastName = "bbbbb", VkId = 1};
            var user2 = new Customer { FirstName = "aaaa", LastName = "bbbbb", VkId = 1 };
            customers.Add(user1);
            customers.Add(user2);

            var album1 = new Album {CreationDate = DateTime.Now, VkId = 1, Title = "Album 1"};
            var album2 = new Album { CreationDate = DateTime.Now, VkId = 2, Title = "Album 2" };
            albums.Add(album1);
            albums.Add(album2);

            var p1 = new Product { GenericUrl = "aaa", Title = "Product 1", Price = 10, VkId = 1, AlbumId = album1.Id};      // p1 -> album1
            var p2 = new Product { GenericUrl = "aaa", Title = "Product 2", Price = 10, VkId = 2, AlbumId = album1.Id };    // p2 -> album1
            var p3 = new Product { GenericUrl = "aaa", Title = "Product 3", Price = 10, VkId = 3, AlbumId = album1.Id };    // p3 -> album1
            var p4 = new Product { GenericUrl = "aaa", Title = "Product 4", Price = 10, VkId = 4, AlbumId = album1.Id };    // p4 -> album1

            var p5 = new Product { GenericUrl = "aaa", Title = "Product 5", Price = 10, VkId = 5, AlbumId = album2.Id };    // p5 -> album1
            var p6 = new Product { GenericUrl = "aaa", Title = "Product 6", Price = 10, VkId = 6, AlbumId = album2.Id };    // p6 -> album1

            products.Add(p1);
            products.Add(p2);
            products.Add(p3);
            products.Add(p4);
            products.Add(p5);
            products.Add(p6);

            orders.Add(new Order{Amount = 10, CustomerId = user1.Id, Date = DateTime.Now, ProductId = p1.Id}); // user1 -> p1 (album1)
            orders.Add(new Order { Amount = 10, CustomerId = user1.Id, Date = DateTime.Now, ProductId = p2.Id }); // user1 -> p2 (album1)
            orders.Add(new Order { Amount = 10, CustomerId = user1.Id, Date = DateTime.Now, ProductId = p4.Id }); // user1 -> p4 (album1)
            orders.Add(new Order { Amount = 10, CustomerId = user1.Id, Date = DateTime.Now, ProductId = p6.Id }); // user1 -> p6 (album2)

            orders.Add(new Order { Amount = 10, CustomerId = user2.Id, Date = DateTime.Now, ProductId = p1.Id }); // user2 -> p1 (album1)
            orders.Add(new Order { Amount = 10, CustomerId = user2.Id, Date = DateTime.Now, ProductId = p3.Id }); // user2 -> p3 (album1)
            orders.Add(new Order { Amount = 10, CustomerId = user2.Id, Date = DateTime.Now, ProductId = p5.Id }); // user2 -> p5 (album2)

            var album1Orders = orders.GetOrdersForAlbum(album1);
            if (album1Orders.Count != 5) throw new Exception("GetOrdersForAlbum failed");

            var album2Orders = orders.GetOrdersForAlbum(album2);
            if (album2Orders.Count != 2) throw new Exception("GetOrdersForAlbum x2 failed");

            var user1Orders = orders.GetOrdersForCustomer(user1);
            if (user1Orders.Count != 4) throw new Exception("GetOrdersForCustomer failed");
            
            var user2Orders = orders.GetOrdersForCustomer(user2);
            if (user2Orders.Count != 3) throw new Exception("GetOrdersForCustomer x2 failed");

            var user1Album1Orders = orders.GetOrdersForCustomerFromAlbum(user1, album1);
            if (user1Album1Orders.Count != 3) throw new Exception("GetOrdersForCustomerFromAlbum failed");

            var user1Album2Orders = orders.GetOrdersForCustomerFromAlbum(user1, album2);
            if (user1Album2Orders.Count != 1) throw new Exception("GetOrdersForCustomerFromAlbum failed");

            var user2Album1Orders = orders.GetOrdersForCustomerFromAlbum(user2, album1);
            if (user2Album1Orders.Count != 2) throw new Exception("GetOrdersForCustomerFromAlbum failed");

            var user2Album2Orders = orders.GetOrdersForCustomerFromAlbum(user2, album2);
            if (user2Album2Orders.Count != 1) throw new Exception("GetOrdersForCustomerFromAlbum failed");

            products.Delete(p1);
            products.Delete(p2);
            products.Delete(p3);
            products.Delete(p4);
            products.Delete(p5);
            products.Delete(p6);
            albums.Delete(album1);
            albums.Delete(album2);
            customers.Delete(user1);
            customers.Delete(user2);
        }
    }
}
