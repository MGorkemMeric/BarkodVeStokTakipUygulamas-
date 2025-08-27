using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace BarkodStoksistemiApp
{
    public partial class VerifyCodeForm : Form
    {
        private string email;
        private string code;
        private string expectedCode;
        private ForgotPasswordForm parentForm;
        public VerifyCodeForm(string code, string email, ForgotPasswordForm parentForm)
        {
            InitializeComponent();
            this.expectedCode = code;
            this.email = email;
            this.parentForm = parentForm;
        }



        private void btnVerify_Click(object sender, EventArgs e)
        {
            string enteredCode = txtCode.Text.Trim();
            string expected = expectedCode.Trim();

            if (enteredCode == expected)
            {
                MessageBox.Show("Doğrulama başarılı. Yeni şifreyi girebilirsiniz.");
                parentForm.ShowPasswordFields();
                this.Close();
            }
            else
            {
                MessageBox.Show("Kod yanlış. Lütfen tekrar deneyin.");
            }
        }
    }
}
