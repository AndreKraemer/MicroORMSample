// --------------------------------------------------------------------------------------
// <copyright file="DapperSample.cs" company="André Krämer - Software, Training & Consulting">
//      Copyright (c) 2015 André Krämer http://andrekraemer.de
// </copyright>
// <summary>
//  Micro O/R Mapper Demo Project
// </summary>
// --------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MicroOrmSample.Models;
using Dapper;

namespace MicroOrmSample
{
    public class DapperSample : ISample
    {
        private int _locationId = 0;

        #region 1 - Simple Query with Dapper
        public  void SimpleQuery()
        {
            Console.WriteLine("Dapper: Einfache Abfrage");
            using (var connection = GetConnection())
            {
                connection.Open();
                var products = connection.Query<Product>(
                    @"SELECT ProductID ,
                           Name ,
                           ProductNumber  FROM Production.Product	");

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
            Console.WriteLine("Dapper: Parametrisierte Abfrage");
            using (var connection = GetConnection())
            {
                connection.Open();
                var products = connection.Query<Product>(
                    "Select * From Production.Product WHERE Color = @Color or ProductID  = @Id",
                    new { Color = "Blue", Id = 1 });

                foreach (var product in products)
                {
                    Console.WriteLine("{0}: {1} - {2}", product.ProductID, product.ProductNumber, product.Name);
                }
            }
        }
        #endregion

        #region 3 - Many to One Relation (N:1)

        public  void ManyToOneRelations()
        {
            Console.WriteLine("Dapper: Many To One (N:1)");
            using (var connection = GetConnection())
            {
                connection.Open();
                var products = connection.Query<Product, ProductSubcategory, Product>(
                    @"SELECT p.ProductID, p.Name, p.ProductNumber,c.ProductSubcategoryID ,
                       c.Name	From Production.ProductSubcategory AS c
	                   INNER JOIN Production.Product AS p ON p.ProductSubcategoryID = c.ProductSubcategoryID
	                   ORDER BY p.Name
                    ", (p, c) =>
                    {
                        p.ProductSubcategory = c;
                        return p;
                    },
                    splitOn: "ProductSubcategoryID");

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
        public  void Relations()
        {
            Console.WriteLine("Dapper 1:N");
            using (var connection = GetConnection())
            {
                connection.Open();
                var lookup = new Dictionary<int, ProductSubcategory>();
                
                connection.Query<ProductSubcategory, Product, ProductSubcategory>(
                    @"SELECT c.ProductSubcategoryID ,
                       c.Name,p.ProductID, p.Name, p.ProductNumber, p.ProductSubcategoryID	From Production.ProductSubCategory AS c
	                   INNER JOIN Production.Product AS p ON p.ProductSubcategoryID = c.ProductSubCategoryID
	                   ORDER BY c.ProductSubcategoryID, p.Name", (c, p) =>
                                              {
                                                  return MapCategory(lookup, c, p);

                                              },
                      splitOn: "ProductID");

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

        private static  ProductSubcategory MapCategory(Dictionary<int, ProductSubcategory> lookup, ProductSubcategory c, Product p)
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
            using (var connection = GetConnection())
            {
                connection.Open();

                string sql = @"
                    	SELECT TOP 10 p.Name, Total = SUM(sd.LineTotal)
	                     FROM Production.Product 
	                    p INNER JOIN Sales.SalesOrderDetail sd
	                    ON sd.ProductID = p.ProductID
	                    GROUP BY p.Name
                            order by Total desc";

                var topSales = connection.Query(sql);

                foreach (var sale in topSales)
                {
                    Console.WriteLine("{0:00} * {1}", sale.Total, sale.Name);
                }


            }
        }
        #endregion

        #region 6 - Stored Procedure Call
        public void SP()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                var orderHistory = connection.Query("[uspGetEmployeeManagers]",
                    new { @BusinessEntityID = 257 },
                    commandType: CommandType.StoredProcedure);

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

            using (var connection = GetConnection())
            {
                connection.Open();

                string sql =
                    @"  INSERT INTO Production.Location
          ( Name ,
            CostRate ,
            Availability ,
            ModifiedDate
          )
  VALUES  ( @Name ,
            @CostRate ,
            @Availability ,
            @ModifiedDate  
          )";

                connection.Execute(sql, location);
                _locationId = Convert.ToInt32(connection.Query("SELECT @@IDENTITY AS Id").Single().Id);


                var newLocation = connection.Query<Location>("Select * from Production.Location where LocationID = @LocationID",
                    new { LocationID = _locationId }).Single();
                Console.WriteLine("{0} - {1}: {2:c}", newLocation.LocationID, newLocation.Name, newLocation.CostRate);

            }
        }
        #endregion

        #region 8 - Update
        public void Update()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string sql = @"UPDATE Production.Location 
                        SET CostRate = @CostRate WHERE Location.LocationID = @LocationID";

                var updateData = new { LocationID = _locationId, CostRate = 500 };

                connection.Execute(sql, updateData);

                var newLocation = connection.Query<Location>("Select * from Production.Location where LocationID = @LocationID",
                                new { LocationID = _locationId }).Single();
                Console.WriteLine("{0} - {1}: {2:c}", newLocation.LocationID, newLocation.Name, newLocation.CostRate);

            }
        }
        #endregion

        #region 9 - Delete
        public void Delete()
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string sql = "delete from Production.Location where LocationID = @LocationID";

                var updateData = new { LocationID = _locationId };

                int result = connection.Execute(sql, updateData);


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