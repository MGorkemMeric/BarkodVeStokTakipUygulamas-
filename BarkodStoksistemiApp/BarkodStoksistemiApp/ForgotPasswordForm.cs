using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;
using Microsoft.Data.Sqlite;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace BarkodStoksistemiApp
{
    public partial class ForgotPasswordForm : Form
    {
        private string generatedCode;
        private string userEmail;
        private string _username;
        private string _email;
        public ForgotPasswordForm(string username, string email)
        {
            InitializeComponent();

            _username = username;
            _email = email;

            txtEmail.Text = _email;

            // İlk açılışta şifre alanları ve buton gizli olsun
            lblNewPassword.Visible = false;
            txtNewPassword.Visible = false;
            lblConfirmPassword.Visible = false;
            txtConfirmPassword.Visible = false;
            btnResetPassword.Visible = false;
        }
        public void ShowPasswordFields()
        {
            // Şifre sıfırlama alanlarını görünür yap
            txtNewPassword.Visible = true;
            txtConfirmPassword.Visible = true;
            lblNewPassword.Visible = true;
            lblConfirmPassword.Visible = true;
            btnResetPassword.Visible = true;
        }






        private void btnResetPassword_Click(object sender, EventArgs e)
        {
            string newPassword = txtNewPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Şifreler uyuşmuyor.");
                return;
            }

            string hashedPassword = SecurityHelper.ComputeSha256Hash(newPassword);

            using (var connection = new SqliteConnection(DatabaseHelper.GetConnectionString()))
            {
                connection.Open();
                string query = "UPDATE Users SET Password = @Password WHERE Email = @Email";
                using (var cmd = new SqliteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", userEmail);
                    cmd.ExecuteNonQuery();
                }
                connection.Close();
            }

            MessageBox.Show("Şifreniz başarıyla güncellendi.");
            this.Close();
        }

        private void btnSendCode_Click(object sender, EventArgs e)
        {
            userEmail = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(userEmail))
            {
                MessageBox.Show("Lütfen e-posta adresinizi girin.");
                return;
            }

            // Doğrulama kodunu üret
            Random random = new Random();
            generatedCode = random.Next(100000, 999999).ToString();

            try
            {
                // Mail gönderme işlemi
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("YourMail"); // Değiştir
                mail.To.Add(userEmail);
                mail.Subject = "Şifre Yenileme Kodu";
                mail.Body = "Doğrulama kodunuz: " + generatedCode;

                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
                smtpClient.Port = 587;
                smtpClient.Credentials = new NetworkCredential("YourMail", "AppPassword");
                smtpClient.EnableSsl = true;
                smtpClient.Send(mail);

                // Kod doğrulama formunu aç
                VerifyCodeForm verifyForm = new VerifyCodeForm(generatedCode, userEmail, this);
                verifyForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("E-posta gönderilirken bir hata oluştu: " + ex.Message);
            }
        }
    }
}
