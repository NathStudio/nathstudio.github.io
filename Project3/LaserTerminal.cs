using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace LaserTerminal
{
    public class MainForm : Form
    {
        private string serverUrl = "http://192.168.55.110:5001"; // IP du Pi / PC serveur
        private string myName = "";
        private string step = "BADGE"; // BADGE, QRS, WAIT, GAME
        private int score = 0;
        private int lives = 5;
        private List<string> bigQrs = new List<string>();
        private List<string> smallQrs = new List<string>();
        private Dictionary<string, DateTime> lastShots = new Dictionary<string, DateTime>();

        private TextBox txtScan;
        private Label lblTitle;
        private Label lblMainMsg;
        private Label lblSubMsg;
        private Label lblError;

        public MainForm()
        {
            this.Text = "Laser Game Terminal";
            this.Size = new Size(240, 320);
            this.BackColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            lblTitle = new Label();
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Height = 30;
            lblTitle.BackColor = Color.FromArgb(0, 255, 0);
            lblTitle.ForeColor = Color.Black;
            lblTitle.Font = new Font("Tahoma", 11, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Text = "⚡ TERMINAL SCANNER";
            this.Controls.Add(lblTitle);

            lblError = new Label();
            lblError.Dock = DockStyle.Top;
            lblError.Height = 35;
            lblError.BackColor = Color.Red;
            lblError.ForeColor = Color.White;
            lblError.Font = new Font("Tahoma", 9, FontStyle.Bold);
            lblError.TextAlign = ContentAlignment.MiddleCenter;
            lblError.Visible = false;
            this.Controls.Add(lblError);

            lblMainMsg = new Label();
            lblMainMsg.Dock = DockStyle.Top;
            lblMainMsg.Height = 80;
            lblMainMsg.ForeColor = Color.Yellow;
            lblMainMsg.Font = new Font("Tahoma", 16, FontStyle.Bold);
            lblMainMsg.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblMainMsg);

            lblSubMsg = new Label();
            lblSubMsg.Dock = DockStyle.Fill;
            lblSubMsg.ForeColor = Color.Cyan;
            lblSubMsg.Font = new Font("Tahoma", 11, FontStyle.Regular);
            lblSubMsg.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblSubMsg);

            txtScan = new TextBox();
            txtScan.Size = new Size(1, 1);
            txtScan.Location = new Point(-10, -10);
            txtScan.KeyDown += new KeyEventHandler(TxtScan_KeyDown);
            this.Controls.Add(txtScan);

            UpdateUI();
        }

        private void TxtScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string code = txtScan.Text.Trim().ToUpper();
                txtScan.Text = "";
                if (code.Length > 0)
                {
                    ProcessScan(code);
                }
            }
        }

        private void ProcessScan(string code)
        {
            lblError.Visible = false;

            if (step == "BADGE")
            {
                if (code.StartsWith("G") || code.StartsWith("P"))
                {
                    ShowError("SCANNEZ VOTRE PRÉNOM SUR LE PI !");
                    return;
                }
                myName = code;
                SendPost("/api/register", "name=" + myName);
                step = "QRS";
                UpdateUI();
            }
            else if (step == "QRS")
            {
                bool isBig = code.Contains("G");
                bool isSmall = code.Contains("P");

                if (bigQrs.Contains(code) || smallQrs.Contains(code))
                {
                    ShowError("CODE DÉJÀ SCANNÉ !");
                    return;
                }

                if (isBig && bigQrs.Count < 2)
                {
                    bigQrs.Add(code);
                    SendPost("/api/check_and_assign_single_qr", "qr=" + code + "&player=" + myName + "&type=big");
                }
                else if (isSmall && smallQrs.Count < 2)
                {
                    smallQrs.Add(code);
                    SendPost("/api/check_and_assign_single_qr", "qr=" + code + "&player=" + myName + "&type=small");
                }
                else
                {
                    ShowError("QUOTA ATTEINT OU CODE INVALIDE");
                    return;
                }

                if (bigQrs.Count >= 2 && smallQrs.Count >= 2)
                {
                    SendPost("/api/ready", "name=" + myName);
                    step = "WAIT";
                }
                UpdateUI();
            }
            else if (step == "GAME") {
                if (lastShots.ContainsKey(code))
                {
                    double diff = (DateTime.Now - lastShots[code]).TotalSeconds;
                    if (diff < 5.0)
                    {
                        ShowError("ATTENDEZ " + ((int)(5 - diff) + 1) + "S AVANT DE RE-TIRER !");
                        return;
                    }
                }
                lastShots[code] = DateTime.Now;

                string res = SendPost("/api/shoot", "shooter=" + myName + "&target_qr=" + code);
                if (res.Contains("true")) {
                    score += 50;
                    ShowError("🎯 TOUCHÉ !");
                    lblError.BackColor = Color.Yellow;
                    lblError.ForeColor = Color.Black;
                } else {
                    ShowError("❌ TIR INVALIDE");
                }
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (step == "BADGE") {
                lblMainMsg.Text = "ÉTAPE 1";
                lblSubMsg.Text = "Scannez votre prénom\naffiché sur le Pi.";
            } else if (step == "QRS") {
                lblMainMsg.Text = myName.ToUpper();
                lblSubMsg.Text = "SCANNEZ VOS CIBLES\nGros : " + bigQrs.Count + "/2\nPetits : " + smallQrs.Count + "/2";
            } else if (step == "WAIT") {
                lblMainMsg.Text = "CIBLES OK !";
                lblSubMsg.Text = "Attente du signal de départ...\n\n(Appuyez sur 'Entrée' pour tester si le jeu a démarré)";
            } else if (step == "GAME") {
                lblMainMsg.Text = "⚡ " + myName.ToUpper();
                lblSubMsg.Text = "VIES : " + lives + "\nSCORE : " + score + " PTS";
            }
            txtScan.Focus();
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.BackColor = Color.Red;
            lblError.ForeColor = Color.White;
            lblError.Visible = true;
        }

        private string SendPost(string endpoint, string data)
        {
            try {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(serverUrl + endpoint);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                req.ContentLength = bytes.Length;
                using (Stream os = req.GetRequestStream()) { os.Write(bytes, 0, bytes.Length); }
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                    return sr.ReadToEnd();
                }
            } catch { return ""; }
        }

        [STAThread]
        static void Main() { Application.Run(new MainForm()); }
    }
}