using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using RAMRewrite.Models;
using RAMRewrite.Utils;

namespace RAMRewrite
{
    public partial class MainForm : Form
    {
        private List<Account> accounts = new List<Account>();
        private TextBox passwordBox;
        private ListView accountList;

        public MainForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "RAM C# Rewrite";
            this.Width = 900;
            this.Height = 600;

            passwordBox = new TextBox() { Top = 10, Left = 10, Width = 200, PasswordChar = '*' };
            this.Controls.Add(passwordBox);

            Button loadBtn = new Button() { Top = 10, Left = 220, Text = "Load" };
            loadBtn.Click += LoadBtn_Click;
            this.Controls.Add(loadBtn);

            Button saveBtn = new Button() { Top = 10, Left = 300, Text = "Save" };
            saveBtn.Click += SaveBtn_Click;
            this.Controls.Add(saveBtn);

            Button launchBtn = new Button() { Top = 10, Left = 380, Text = "Launch Roblox" };
            launchBtn.Click += LaunchBtn_Click;
            this.Controls.Add(launchBtn);

            Button editBtn = new Button() { Top = 10, Left = 500, Text = "Edit Selected" };
            editBtn.Click += EditBtn_Click;
            this.Controls.Add(editBtn);

            Button addBtn = new Button() { Top = 10, Left = 600, Text = "Add" };
            addBtn.Click += (s, e) => { accounts.Add(new Account()); RefreshList(); };
            this.Controls.Add(addBtn);

            Button removeBtn = new Button() { Top = 10, Left = 660, Text = "Remove" };
            removeBtn.Click += (s, e) =>
            {
                if (accountList.SelectedIndices.Count > 0)
                {
                    int idx = accountList.SelectedIndices[0];
                    accounts.RemoveAt(idx);
                    RefreshList();
                }
            };
            this.Controls.Add(removeBtn);

            accountList = new ListView() { Top = 50, Left = 10, Width = 860, Height = 500, View = View.Details, FullRowSelect = true };
            accountList.Columns.Add("Username", 200);
            accountList.Columns.Add("Note", 400);
            accountList.Columns.Add("Token", 240);
            this.Controls.Add(accountList);
        }

        private void RefreshList()
        {
            accountList.Items.Clear();
            foreach (var a in accounts)
            {
                // Show masked token in the list (do not expose token content)
                string tokenDisplay = string.IsNullOrEmpty(a.Token) ? "" : ("●●●●●" + (a.Token.Length >= 6 ? a.Token.Substring(a.Token.Length - 6) : ""));
                accountList.Items.Add(new ListViewItem(new[] { a.Username, a.Note, tokenDisplay }));
            }
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            string password = passwordBox.Text;
            if (string.IsNullOrEmpty(password)) { MessageBox.Show("Enter password"); return; }
            string encrypted = CryptoHelper.EncryptAccounts(accounts, password);
            File.WriteAllText("accounts.dat", encrypted);
            MessageBox.Show("Accounts saved encrypted.");
        }

        private void LoadBtn_Click(object sender, EventArgs e)
        {
            string password = passwordBox.Text;
            if (string.IsNullOrEmpty(password)) { MessageBox.Show("Enter password"); return; }
            try
            {
                string encrypted = File.ReadAllText("accounts.dat");
                accounts = CryptoHelper.DecryptAccounts(encrypted, password);
                RefreshList();
                MessageBox.Show("Accounts loaded.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load: " + ex.Message);
            }
        }

        private void EditBtn_Click(object sender, EventArgs e)
        {
            if (accountList.SelectedIndices.Count == 0) { MessageBox.Show("Select an account first."); return; }
            int idx = accountList.SelectedIndices[0];
            ShowEditDialog(accounts[idx]);
        }

        private void ShowEditDialog(Account account)
        {
            // Simple modal dialog to edit Username, Note, and Token (token field can be masked)
            Form dlg = new Form() { Width = 500, Height = 260, Text = "Edit Account", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent };
            var usr = new TextBox() { Top = 10, Left = 10, Width = 450, Text = account.Username };
            var note = new TextBox() { Top = 50, Left = 10, Width = 450, Text = account.Note };
            var token = new TextBox() { Top = 90, Left = 10, Width = 450, Text = account.Token ?? "" };
            var maskToken = new CheckBox() { Top = 120, Left = 10, Width = 200, Text = "Mask token display" , Checked = true };
            token.UseSystemPasswordChar = true;
            maskToken.CheckedChanged += (s, e) => token.UseSystemPasswordChar = maskToken.Checked;

            var ok = new Button() { Text = "OK", Left = 300, Width = 80, Top = 150, DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "Cancel", Left = 390, Width = 80, Top = 150, DialogResult = DialogResult.Cancel };

            dlg.Controls.Add(new Label() { Text = "Username", Top = -2, Left = 10 });
            dlg.Controls.Add(usr);
            dlg.Controls.Add(new Label() { Text = "Note", Top = 38, Left = 10 });
            dlg.Controls.Add(note);
            dlg.Controls.Add(new Label() { Text = ".ROBLOSECURITY token (paste if you want)", Top = 88, Left = 10 });
            dlg.Controls.Add(token);
            dlg.Controls.Add(maskToken);
            dlg.Controls.Add(ok);
            dlg.Controls.Add(cancel);

            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;

            var res = dlg.ShowDialog(this);
            if (res == DialogResult.OK)
            {
                account.Username = usr.Text;
                account.Note = note.Text;
                account.Token = token.Text; // stored in memory; persisted on Save
                RefreshList();
            }
        }

        private void LaunchBtn_Click(object sender, EventArgs e)
        {
            // If user selected an account, we can optionally show warning about token usage
            Account selected = null;
            if (accountList.SelectedIndices.Count > 0)
            {
                selected = accounts[accountList.SelectedIndices[0]];
            }

            // Close Roblox singleton event (we added RobloxSingleton.CloseSingletonEvent earlier)
            bool closed = RobloxSingleton.CloseSingletonEvent();

            // Optionally warn user if no account selected or token missing
            if (selected != null && string.IsNullOrEmpty(selected.Token))
            {
                var ask = MessageBox.Show("Selected account does not contain a token. Launch Roblox anyway?", "No token", MessageBoxButtons.YesNo);
                if (ask == DialogResult.No) return;
            }

            // Launch Roblox launcher
            string robloxPath = GetRobloxLauncherPath();
            try
            {
                Process.Start(robloxPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch Roblox: " + ex.Message + "\nMake sure path is correct: " + robloxPath);
            }
        }

        private string GetRobloxLauncherPath()
        {
            // Default path; users can change in code or you can add UI to configure
            // Replace <User> or make this dynamic by reading %LocalAppData%
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string path = Path.Combine(local, "Roblox", "RobloxPlayerLauncher.exe");
            return path;
        }
    }
}
