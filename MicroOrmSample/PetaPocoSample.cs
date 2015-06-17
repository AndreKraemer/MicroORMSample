// --------------------------------------------------------------------------------------
// <copyright file="PetaPocoSample.cs" company="André Krämer - Software, Training & Consulting">
//      Copyright (c) 2015 André Krämer http://andrekraemer.de
// </copyright>
// <summary>
//  Micro O/R Mapper Demo Project
// </summary>
// --------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using PetaPoco;
using MicroOrmSample.Models;

namespace MicroOrmSample
{
    public class PetaPocoSample : ISample
    {
        private int _locationId;

        #region 1 -Simple Query with PetaPoco
        public void SimpleQuery()
        {
            Console.WriteLine("Peta Poco: Einfache Abfrage");
            using (var connection = new Database("AdventureWorksDb"))
            {
                var products = connection.Query<Product>("Select * From Production.Product");

                foreach (var product in products)
                {
                    Console.WriteLine("{0}: {1} - {2}", product.ProductID, product.ProductNumber, product.Name);
                }
            }
        }
        #endregion

        #region 2 - Parameterized Query
        public void ParamQuery()
        {
            using (var connection = new Database("AdventureWorksDb"))
            {
                var products = connection.Query<Product>("Select * From Production.Product WHERE Color = @Color or ProductID  = @Id",
                   new { Color = "Blue", Id = 1 });

                foreach (var product in products)
                {
                    Console.WriteLine("{0}: {1} - {2}", product.ProductID, product.ProductNumber, product.Name);
                }
            }
        }
        #endregion

        #region 3 - Many to One Relation (N:1)
        public void ManyToOneRelations()
        {
            using (var connection = new Database("AdventureWorksDb"))
            {
                var products = connection.Query<Product, ProductSubcategory>(
                    @"SELECT p.ProductID, p.Name, p.ProductNumber,c.ProductSubcategoryID ,
                       c.Name	From Production.ProductSubcategory AS c
	                   INNER JOIN Production.Product AS p ON p.ProductSubcategoryID = c.ProductSubcategoryID
	                   ORDER BY p.Name
                    ");
                    

                foreach (var product in products)
                {
                    Console.WriteLine("Produkt: {0}, vom {1}. Versand an {2}", product.ProductID, product.ProductNumber,
                        product.Name);
                    Console.WriteLine("   --> Subkategorie: {0}, {1}", product.ProductSubcategory.ProductSubcategoryID,
                        product.ProductSubcategory.Name);
                }
            }
            
        }
        #endregion

        #region 4 - 1 to Many Relation (1:N)
        public void Relations()
        {
            Console.WriteLine("Peta Poco 1:N");
            using (var connection = new Database("AdventureWorksDb"))
            {
   
                var lookup = new Dictionary<int, ProductSubcategory>();

                connection.Fetch<ProductSubcategory, Product, ProductSubcategory>((c,p) => MapCategory(lookup, c, p),
                    @"SELECT c.ProductSubcategoryID ,
                       c.Name,p.ProductID, p.Name, p.ProductNumber, p.ProductSubcategoryID	From Production.ProductSubCategory AS c
	                   INNER JOIN Production.Product AS p ON p.ProductSubcategoryID = c.ProductSubCategoryID
	                   ORDER BY c.ProductSubcategoryID, p.Name");

                var categories = lookup.Values;
                foreach (var category in categories)
                {
                    Console.WriteLine("{0}: {1}", category.ProductSubcategoryID, category.Name);
                    foreach (var product in category.Products)
                    {
                        Console.WriteLine("   * Produkt: {0}, {1}: {2}", product.ProductID, product.ProductNumber, product.Name);
                    }
                }
            }
        }

        private static ProductSubcategory MapCategory(Dictionary<int, ProductSubcategory> lookup, ProductSubcategory c, Product p)
        {
            ProductSubcategory category;
            if (!lookup.TryGetValue(c.ProductSubcategoryID, out category))
            {
                lookup.Add(c.ProductSubcategoryID, category = c);
            }
            if (category.Products == null)
            {
                category.Products = new HashSet<Product>();
            }
            if (p != null)
            {
                category.Products.Add(p);
                p.ProductSubcategory = c;
            }
            return c;
        }
        #endregion

        #region 5 - Dynamic Query
        public void DynamicQuery()
        {
            using (var connection = new Database("AdventureWorksDb"))
            {
                string sql = @"
                    	SELECT TOP 10 p.Name, Total = SUM(sd.LineTotal)
	                     FROM Production.Product 
	                    p INNER JOIN Sales.SalesOrderDetail sd
	                    ON sd.ProductID = p.ProductID
	                    GROUP BY p.Name
                            order by Total desc";

                var topSales = connection.Fetch<dynamic>(sql);

                foreach (var sale in topSales)
                {
                    Console.WriteLine("{0:c} * {1}", sale.Total, sale.Name);
                }
            }
        }
        #endregion

        #region 6 - Stored Procedure Call
        public void SP()
        {
            using (var connection = new Database("AdventureWorksDb"))
            {
                var orderHistory = connection.Fetch<dynamic>(";EXEC uspGetEmployeeManagers @0", 257);
                foreach (var entry in orderHistory)
                {
                    Console.WriteLine("{0} * {1} {2} - OrgLevel: {3}, Manager: {4} {5}", entry.RecursionLevel, entry.FirstName, entry.LastName, entry.OrganizationNode, entry.ManagerFirstName, entry.ManagerLastName);
                }
            }
        }
        #endregion

        #region 7 - Insert
        public void Insert()
        {
            var location = new Location();
            location.Name = "Bad Breisig";
            location.CostRate = 10;
            location.Availability = 1;
            location.ModifiedDate = DateTime.Now;

            using (var connection = new Database("AdventureWorksDb"))
            {
                // the convention based approach only works when collection properties are ignored
                // via the PetaPoco.Ignore Attribute
               _locationId =  Convert.ToInt32(connection.Insert("Production.Location", "LocationId", location));
               var newLocation = connection.SingleOrDefault<Location>("Select * from Production.Location where LocationID = @0", _locationId);
               Console.WriteLine("{0} - {1}: {2:c}", newLocation.LocationID, newLocation.Name, newLocation.CostRate);

            }
        }
        #endregion

        #region 8 - Update
        public void Update()
        {
            var updateData = new { LocationID = _locationId, CostRate = 500 };
            using (var connection = new Database("AdventureWorksDb"))
            {
                connection.Update("Production.Location", "LocationId", updateData);
                var newLocation = connection.SingleOrDefault<Location>("Select * from Production.Location where LocationID = @0", _locationId);
                Console.WriteLine("{0} - {1}: {2:c}", newLocation.LocationID, newLocation.Name, newLocation.CostRate);

            }
        }
        #endregion

        #region 9 - Delete
        public void Delete()
        {
            using (var connection = new Database("AdventureWorksDb"))
            {
                int result = connection.Delete("Production.Location", "LocationId", null, _locationId);
                Console.WriteLine("Es wurden {0} Datensätze gelöscht", result);
            }
        }
        #endregion

        public SqlConnection GetConnection()
        {
            return new SqlConnection(ConfigurationManager.ConnectionStrings["AdventureWorksDb"].ConnectionString);
        }
    }
}