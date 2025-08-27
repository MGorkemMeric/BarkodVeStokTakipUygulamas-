using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace BarkodStoksistemiApp
{
    public static class DatabaseHelper
    {
        // Burada olmalı
        public static string DbFilePath => Path.Combine(Application.StartupPath, "urunler.db");

        public static string GetConnectionString()
        {
            return $"Data Source={DbFilePath}";
        }

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbFilePath))
            {
                using (File.Create(DbFilePath)) { }

                using (var connection = new SqliteConnection(GetConnectionString()))
                {
                    connection.Open();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText =
                    @"
            CREATE TABLE Logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Date TEXT,
                Action TEXT,
                Barcode TEXT,
                ProductName TEXT,
                OldDetails TEXT,
                NewDetails TEXT,
                Price REAL,
                Stock INTEGER,
                Description TEXT
            );

            CREATE TABLE Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Price REAL,
                GelisFiyati REAL,
                Stock INTEGER,
                Description TEXT,
                ImagePath TEXT
            );

            CREATE TABLE Sales (
                SaleId INTEGER PRIMARY KEY,
                SaleDate TEXT NOT NULL,
                PaymentMethod TEXT NOT NULL,
                TotalAmount NUMERIC NOT NULL,
                Description TEXT
            );

            CREATE TABLE SaleItems (
                SaleItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                SaleId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                PriceAtSale REAL NOT NULL,
                FOREIGN KEY(ProductId) REFERENCES Products(Id),
                FOREIGN KEY(SaleId) REFERENCES Sales(SaleId)
            );

            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE
            );
            ";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
