using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarkodStoksistemiApp
{
    public partial class SalesReportForm : Form
    {
        public SalesReportForm()
        {
            InitializeComponent();
        }
        private void SalesReportForm_Load(object sender, EventArgs e)
        {
            cmbDateFilter.Items.AddRange(new string[]
            {
        "Günlük",
        "Haftalık",
        "Aylık",
        "3 Aylık",
        "6 Aylık",
        "Yıllık",
        "Özel Aralık"
            });
            cmbDateFilter.SelectedIndex = 0; // varsayılan Günlük
        }

        private void LoadReport(DateTime startDate, DateTime endDate)
        {
            string connStr = DatabaseHelper.GetConnectionString();

            using (SQLiteConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string query = @"
        SELECT 
            s.SaleId,
            DATE(s.SaleDate) AS Tarih,
            COUNT(si.SaleItemId) AS SatisSayisi,
            SUM(si.PriceAtSale * si.Quantity) AS ToplamKazanc,
            SUM((si.PriceAtSale - p.GelisFiyati) * si.Quantity) AS KarZarar
        FROM Sales s
        INNER JOIN SaleItems si ON s.SaleId = si.SaleId
        INNER JOIN Products p ON si.ProductId = p.Id
        WHERE DATE(s.SaleDate) BETWEEN DATE(@start) AND DATE(@end)
        GROUP BY DATE(s.SaleDate)
        ORDER BY Tarih DESC";

                using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvSalesReport.DataSource = dt;

                        int totalSales = dt.Rows.Count > 0 ? dt.AsEnumerable().Sum(row => Convert.ToInt32(row["SatisSayisi"])) : 0;
                        double totalRevenue = dt.Rows.Count > 0 ? dt.AsEnumerable().Sum(row => Convert.ToDouble(row["ToplamKazanc"])) : 0;
                        double totalProfitLoss = dt.Rows.Count > 0 ? dt.AsEnumerable().Sum(row => Convert.ToDouble(row["KarZarar"])) : 0;

                        lblTotalSales.Text = $"Toplam Satış Sayısı: {totalSales}";
                        lblTotalRevenue.Text = $"Toplam Kazanç: ₺{totalRevenue:0.00}";
                        lblProfitLoss.Text = $"Kâr/Zarar: ₺{totalProfitLoss:0.00}";
                    }
                }
            }
        }






        private void cmbDateFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate;

            switch (cmbDateFilter.SelectedItem.ToString())
            {
                case "Günlük":
                    startDate = endDate.Date;
                    break;
                case "Haftalık":
                    startDate = endDate.AddDays(-7);
                    break;
                case "Aylık":
                    startDate = endDate.AddMonths(-1);
                    break;
                case "3 Aylık":
                    startDate = endDate.AddMonths(-3);
                    break;
                case "6 Aylık":
                    startDate = endDate.AddMonths(-6);
                    break;
                case "Yıllık":
                    startDate = endDate.AddYears(-1);
                    break;
                case "Özel Aralık":
                    // Tarih seçimi kullanıcıya bırakılıyor
                    return;
            }

            LoadReport(startDate, endDate);
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            DateTime startDate = DateTime.Today;
            DateTime endDate = DateTime.Today;

            switch (cmbDateFilter.SelectedItem.ToString())
            {
                case "Bugün":
                    startDate = DateTime.Today;
                    endDate = DateTime.Today;
                    break;
                case "Bu Ay":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    endDate = DateTime.Today;
                    break;
                case "Son 3 Ay":
                    startDate = DateTime.Today.AddMonths(-3);
                    endDate = DateTime.Today;
                    break;
                case "Son 6 Ay":
                    startDate = DateTime.Today.AddMonths(-6);
                    endDate = DateTime.Today;
                    break;
                case "Bu Yıl":
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    endDate = DateTime.Today;
                    break;
                case "Özel Aralık":
                    startDate = dtpStart.Value.Date;
                    endDate = dtpEnd.Value.Date;
                    break;
            }
            LoadReport(dtpStart.Value, dtpEnd.Value);

            LoadReport(startDate, endDate);
        }
    }
}
