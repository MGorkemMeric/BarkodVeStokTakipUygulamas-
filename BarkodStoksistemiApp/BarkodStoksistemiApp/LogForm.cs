using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace BarkodStoksistemiApp
{
    public partial class LogForm : Form
    {
        private DataTable logsTable; // Arama için tabloyu saklayacağız

        public LogForm()
        {
            InitializeComponent();
            LoadLogs();

            // Arama kutusuna yazıldıkça filtre uygula
            txtSearch.TextChanged += TxtSearch_TextChanged;
        }

        private void LoadLogs()
        {
            using (var connection = new SQLiteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                string query = "SELECT Date, Action, Barcode, ProductName, OldDetails, NewDetails FROM Logs ORDER BY Date DESC";

                using (var adapter = new SQLiteDataAdapter(query, connection))
                {
                    logsTable = new DataTable();
                    adapter.Fill(logsTable);
                    dataGridViewLogs.DataSource = logsTable;

                    // Kolon başlıklarını değiştirme
                    if (dataGridViewLogs.Columns.Contains("Date"))
                        dataGridViewLogs.Columns["Date"].HeaderText = "Tarih";

                    if (dataGridViewLogs.Columns.Contains("Action"))
                        dataGridViewLogs.Columns["Action"].HeaderText = "İşlem";

                    if (dataGridViewLogs.Columns.Contains("Barcode"))
                        dataGridViewLogs.Columns["Barcode"].HeaderText = "Barkod";

                    if (dataGridViewLogs.Columns.Contains("ProductName"))
                        dataGridViewLogs.Columns["ProductName"].HeaderText = "Ürün Adı";

                    if (dataGridViewLogs.Columns.Contains("OldDetails"))
                        dataGridViewLogs.Columns["OldDetails"].HeaderText = "Eski Bilgi";

                    if (dataGridViewLogs.Columns.Contains("NewDetails"))
                        dataGridViewLogs.Columns["NewDetails"].HeaderText = "Yeni Bilgi";

                    if (dataGridViewLogs.Columns.Contains("Id"))
                        dataGridViewLogs.Columns["Id"].Visible = false;
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            if (logsTable == null) return;

            string filterText = txtSearch.Text.Replace("'", "''"); // SQL injection engellemek için tek tırnak kaçar
            if (string.IsNullOrWhiteSpace(filterText))
            {
                logsTable.DefaultView.RowFilter = ""; // Filtre yok
            }
            else
            {
                // Burada istediğin kolonlarda arama yapabilirsin
                logsTable.DefaultView.RowFilter =
                    $"Date LIKE '%{filterText}%' OR " +
                    $"Action LIKE '%{filterText}%' OR " +
                    $"Barcode LIKE '%{filterText}%' OR " +
                    $"ProductName LIKE '%{filterText}%' OR " +
                    $"OldDetails LIKE '%{filterText}%' OR " +
                    $"NewDetails LIKE '%{filterText}%'";
            }
        }

        private async void PythonRunner_Click(object sender, EventArgs e)
        {
            try
            {
                string exePath = Path.Combine(Application.StartupPath, "scraper.exe");
                if (!File.Exists(exePath))
                {
                    MessageBox.Show("Scraper exe bulunamadı: " + exePath);
                    return;
                }

                progressBar1.Visible = true;
                progressBar1.Style = ProgressBarStyle.Marquee;
                txtLog.Clear();
                PythonRunner.Enabled = false;

                var logBuffer = new System.Text.StringBuilder();

                await Task.Run(async () =>
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath), // ÖNEMLİ
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    };

                    using (var process = new System.Diagnostics.Process { StartInfo = psi, EnableRaisingEvents = true })
                    {
                        process.OutputDataReceived += (s, ev) =>
                        {
                            if (ev.Data == null) return;
                            this.BeginInvoke(new Action(() =>
                            {
                                txtLog.AppendText(ev.Data + Environment.NewLine);
                            }));
                            lock (logBuffer) logBuffer.AppendLine(ev.Data);
                        };

                        process.ErrorDataReceived += (s, ev) =>
                        {
                            if (ev.Data == null) return;
                            this.BeginInvoke(new Action(() =>
                            {
                                // Hata/stderr’ı da aynı pencereye akıt
                                txtLog.AppendText(ev.Data + Environment.NewLine);
                            }));
                            lock (logBuffer) logBuffer.AppendLine(ev.Data);
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        await Task.Run(() => process.WaitForExit());
                        int exitCode = process.ExitCode;

                        this.BeginInvoke(new Action(() =>
                        {
                            progressBar1.Visible = false;
                            PythonRunner.Enabled = true;

                            // Logtan özet çıkar
                            string all = logBuffer.ToString();
                            string summary = ExtractSummary(all);
                            MessageBox.Show(
                                (exitCode == 0 ? "İşlem tamamlandı ✅" : $"Exit code: {exitCode}") +
                                (string.IsNullOrWhiteSpace(summary) ? "" : "\n\n" + summary),
                                "Scraper Durumu");
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                progressBar1.Visible = false;
                PythonRunner.Enabled = true;
            }
        }

        // Basit bir özetleyici: log’tan toplam/insert/update satırlarını çek
        private string ExtractSummary(string log)
        {
            if (string.IsNullOrEmpty(log)) return "";
            var lines = log.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                           .Where(l => l.Contains("Total products "))
                           .ToArray();
            return string.Join("\n", lines);
        }
    }
}
