using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RapidAid
{
    // ==========================================
    // 1. BACKEND LOGIC (DSA)
    // ==========================================
    public class Supply
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public int Urgency { get; set; }
    }

    public class ReliefSystem
    {
        public List<Supply> Inventory = new List<Supply>();
        public Queue<string> OrderQueue = new Queue<string>();

        // MANUAL BUBBLE SORT
        public void SortInventory(bool byUrgency, bool descending)
        {
            int n = Inventory.Count;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    bool swap = false;
                    Supply a = Inventory[j];
                    Supply b = Inventory[j + 1];

                    if (byUrgency)
                    {
                        if (descending ? a.Urgency < b.Urgency : a.Urgency > b.Urgency) swap = true;
                    }
                    else
                    {
                        if (descending ? a.Quantity < b.Quantity : a.Quantity > b.Quantity) swap = true;
                    }

                    if (swap)
                    {
                        Inventory[j] = b;
                        Inventory[j + 1] = a;
                    }
                }
            }
        }

        // MANUAL LINEAR SEARCH
        public Supply FindSupply(string name)
        {
            foreach (var item in Inventory)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return item;
            }
            return null;
        }
    }

    // ==========================================
    // 2. MODERN UI CONTROLS
    // ==========================================
    public class GradientPanel : Panel
    {
        public Color ColorTop { get; set; }
        public Color ColorBottom { get; set; }
        protected override void OnPaint(PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(this.ClientRectangle, ColorTop, ColorBottom, 90F))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }
    }

    public class CardPanel : Panel
    {
        public CardPanel() { this.BackColor = Color.White; this.Padding = new Padding(20); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = 20;
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
                path.CloseFigure();
                using (SolidBrush brush = new SolidBrush(Color.White)) g.FillPath(brush, path);
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1)) g.DrawPath(pen, path);
            }
        }
    }

    public class ModernButton : Button
    {
        private bool isHovered = false;
        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0; 
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            this.Size = new Size(190, 55);
            this.Cursor = Cursors.Hand;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.MouseEnter += (s, e) => { isHovered = true; Invalidate(); };
            this.MouseLeave += (s, e) => { isHovered = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            int radius = 25;

            Color c1 = Color.FromArgb(40, 40, 60);
            Color c2 = Color.FromArgb(20, 20, 30);

            if (this.Tag != null && this.Tag.ToString() == "Action")
            {
                c1 = isHovered ? Color.FromArgb(0, 200, 150) : Color.FromArgb(0, 180, 130);
                c2 = isHovered ? Color.FromArgb(0, 180, 130) : Color.FromArgb(0, 150, 100);
            }
            else
            {
                if (isHovered) { c1 = Color.FromArgb(60, 60, 90); c2 = Color.FromArgb(40, 40, 70); }
            }

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
                path.CloseFigure();
                using (LinearGradientBrush brush = new LinearGradientBrush(rect, c1, c2, 45F)) g.FillPath(brush, path);
            }
            TextRenderer.DrawText(g, Text, Font, rect, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ==========================================
    // 3. MAIN FORM
    // ==========================================
    public partial class Form1 : Form
    {
        ReliefSystem system = new ReliefSystem();
        GradientPanel sidebar;
        Panel contentPanel;

        // Screens
        Panel pnlDash, pnlAdd, pnlInv, pnlSearch;

        // Dashboard Widgets
        Label lblTotal, lblUrgent, lblOrders;

        // Add Inputs
        TextBox txtName, txtQty, txtUrg;

        // Search Inputs
        TextBox txtSearch;
        Panel pnlResultCard;
        Label lblResName, lblResQty, lblResUrg;

        // Grid
        DataGridView grid;

        // NEW: fields from design
        private int borderSize = 2;
        private Size formSize; // Keep form size when it is minimized and restored.
        private Label lblTitle; // title label reference used for dragging & collapsing behavior
        private ToolTip sidebarToolTip; // tooltip for collapsed menu

        // Win32 drag helpers
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();

        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // Constructor
        public Form1()
        {
            this.Text = "RapidAid | Disaster Relief System";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            // Set border/back color for custom border visual
            this.Padding = new Padding(borderSize); // Border size
            this.BackColor = Color.FromArgb(98, 102, 244); // Border color

            InitializeUI();

            // Do NOT auto-collapse at startup (keeps buttons text visible and layout intact).
            // CollapseMenu();

            // Wire form events for custom behavior
            this.Load += Form1_Load;
            this.Resize += Form1_Resize;

            // Mock Data
            system.Inventory.Add(new Supply { Name = "Blankets", Quantity = 100, Urgency = 5 });
            system.Inventory.Add(new Supply { Name = "Water", Quantity = 500, Urgency = 5 });
            system.Inventory.Add(new Supply { Name = "Bandages", Quantity = 200, Urgency = 3 });
            system.OrderQueue.Enqueue("Request for Blankets");
            ShowDashboard();
        }

        private void InitializeUI()
        {
            // Start with expanded width that matches CollapseMenu's expanded width
            sidebar = new GradientPanel { Dock = DockStyle.Left, Width = 230, ColorTop = Color.FromArgb(30, 50, 90), ColorBottom = Color.FromArgb(10, 20, 40) };

            // Tooltip for collapsed buttons
            sidebarToolTip = new ToolTip();

            // Title label (promoted from local to field so it can be used elsewhere)
            lblTitle = new Label { Text = "RAPID AID", Dock = DockStyle.Top, Height = 120, Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            // Allow dragging by mouse down on title
            lblTitle.MouseDown += panelTitleBar_MouseDown;

            // Add buttons in the order they should appear (Top-down). Tag stores the intended text for restore.
            sidebar.Controls.Add(CreateBtn("❌  Exit", (s, e) => Application.Exit()));
            sidebar.Controls.Add(CreateBtn("📊  Summary", (s, e) => ShowDashboard()));
            sidebar.Controls.Add(CreateBtn("📋  Process Queue", Btn_Process_Click));
            sidebar.Controls.Add(CreateBtn("🔍  Search Item", (s, e) => ShowSearchPanel()));
            sidebar.Controls.Add(CreateBtn("🔃  Sort Stock", Btn_Sort_Click));
            sidebar.Controls.Add(CreateBtn("➕  Add Supply", (s, e) => ShowAddPanel()));
            sidebar.Controls.Add(lblTitle);

            contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40) };
            this.Controls.Add(contentPanel);
            this.Controls.Add(sidebar);

            InitDashboard();
            InitAddPanel();
            InitSearchPanel(); // NEW Custom Search Screen
            InitGrid();
        }

        private ModernButton CreateBtn(string t, EventHandler e)
        {
            var b = new ModernButton { Text = t, Dock = DockStyle.Top, Height = 70, BackColor = Color.Transparent, Tag = t };
            b.Click += e;
            // Provide a tooltip showing full label (visible when sidebar is collapsed)
            if (sidebarToolTip != null) sidebarToolTip.SetToolTip(b, t);
            return b;
        }

        // --- DASHBOARD ---
        private void InitDashboard()
        {
            pnlDash = new Panel { Dock = DockStyle.Fill, Visible = false };
            Label head = new Label { Text = "Dashboard Overview", Font = new Font("Segoe UI", 26, FontStyle.Bold), ForeColor = Color.FromArgb(40, 40, 60), AutoSize = true, Location = new Point(50, 20) };
            pnlDash.Controls.Add(head);

            FlowLayoutPanel cardContainer = new FlowLayoutPanel { Location = new Point(50, 100), Size = new Size(900, 250), AutoSize = true };
            cardContainer.Controls.Add(CreateStatCard("Total Supplies", "0", Color.DodgerBlue, out lblTotal));
            cardContainer.Controls.Add(CreateStatCard("Critical Items", "0", Color.OrangeRed, out lblUrgent));
            cardContainer.Controls.Add(CreateStatCard("Pending Orders", "0", Color.MediumSeaGreen, out lblOrders));

            pnlDash.Controls.Add(cardContainer);
            contentPanel.Controls.Add(pnlDash);
        }

        private Panel CreateStatCard(string title, string val, Color c, out Label valLabel)
        {
            CardPanel card = new CardPanel { Size = new Size(260, 160), Margin = new Padding(0, 0, 30, 0) };
            Label t = new Label { Text = title, Dock = DockStyle.Top, ForeColor = Color.Gray, Font = new Font("Segoe UI", 11) };
            Label v = new Label { Text = val, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 36, FontStyle.Bold), ForeColor = c, TextAlign = ContentAlignment.MiddleCenter };
            card.Controls.Add(v); card.Controls.Add(t);
            valLabel = v;
            return card;
        }

        // --- NEW: STYLED SEARCH SCREEN ---
        private void InitSearchPanel()
        {
            pnlSearch = new Panel { Dock = DockStyle.Fill, Visible = false };

            // Container for Search UI
            CardPanel searchCard = new CardPanel { Size = new Size(600, 500) };
            searchCard.Left = (this.ClientSize.Width - 260 - 600) / 2; // Center horizontally
            searchCard.Top = 80;

            Label head = new Label { Text = "Find Supply", Font = new Font("Segoe UI", 22, FontStyle.Bold), Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };

            // Search Bar
            Panel bar = new Panel { Size = new Size(500, 60), Location = new Point(50, 80) };
            txtSearch = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 16), BorderStyle = BorderStyle.FixedSingle };
            Label lblHelper = new Label { Text = "Enter item name (e.g., 'Water')", Dock = DockStyle.Top, ForeColor = Color.Gray };
            bar.Controls.Add(txtSearch); bar.Controls.Add(lblHelper);

            // Search Button
            ModernButton btnFind = new ModernButton { Text = "Search Inventory", Location = new Point(150, 160), Size = new Size(300, 50), Tag = "Action" };
            btnFind.Click += Btn_PerformSearch_Click;

            // --- RESULT CARD (Hidden by default) ---
            pnlResultCard = new Panel { Size = new Size(500, 200), Location = new Point(50, 240), Visible = false, BackColor = Color.WhiteSmoke };

            lblResName = new Label { Text = "ITEM NAME", Dock = DockStyle.Top, Font = new Font("Segoe UI", 18, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Height = 50 };
            lblResQty = new Label { Text = "Quantity: 000", Dock = DockStyle.Top, Font = new Font("Segoe UI", 14), TextAlign = ContentAlignment.MiddleCenter, Height = 40 };
            lblResUrg = new Label { Text = "Urgency: 5", Dock = DockStyle.Top, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.Red, TextAlign = ContentAlignment.MiddleCenter, Height = 40 };

            pnlResultCard.Controls.Add(lblResUrg);
            pnlResultCard.Controls.Add(lblResQty);
            pnlResultCard.Controls.Add(lblResName);

            searchCard.Controls.Add(pnlResultCard);
            searchCard.Controls.Add(btnFind);
            searchCard.Controls.Add(bar);
            searchCard.Controls.Add(head);

            pnlSearch.Controls.Add(searchCard);

            // Re-center on resize
            pnlSearch.Resize += (s, e) => { searchCard.Left = (pnlSearch.Width - searchCard.Width) / 2; };

            contentPanel.Controls.Add(pnlSearch);
        }

        private void Btn_PerformSearch_Click(object sender, EventArgs e)
        {
            string query = txtSearch.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            var found = system.FindSupply(query);

            if (found != null)
            {
                pnlResultCard.Visible = true;
                pnlResultCard.BackColor = Color.FromArgb(230, 255, 240); // Light Green bg
                lblResName.Text = found.Name.ToUpper();
                lblResQty.Text = "Quantity Available: " + found.Quantity;
                lblResUrg.Text = "Urgency Level: " + found.Urgency;
                lblResUrg.ForeColor = found.Urgency == 5 ? Color.Red : Color.Orange;
            }
            else
            {
                pnlResultCard.Visible = true;
                pnlResultCard.BackColor = Color.FromArgb(255, 235, 235); // Light Red bg
                lblResName.Text = "NOT FOUND";
                lblResQty.Text = "We do not have '" + query + "'";
                lblResUrg.Text = "Please check spelling.";
                lblResUrg.ForeColor = Color.Gray;
            }
        }

        // --- ADD SUPPLY ---
        private void InitAddPanel()
        {
            pnlAdd = new Panel { Dock = DockStyle.Fill, Visible = false };
            CardPanel formCard = new CardPanel { Size = new Size(500, 450) };
            formCard.Left = (this.ClientSize.Width - 260 - 500) / 2;
            formCard.Top = 80;

            Label head = new Label { Text = "New Supply Entry", Font = new Font("Segoe UI", 20, FontStyle.Bold), Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };

            int y = 80;
            formCard.Controls.Add(head);
            formCard.Controls.Add(MakeInput("Item Name", out txtName, ref y));
            formCard.Controls.Add(MakeInput("Quantity", out txtQty, ref y));
            formCard.Controls.Add(MakeInput("Urgency (1-5)", out txtUrg, ref y));

            var btn = new ModernButton { Text = "Save to Inventory", Location = new Point(50, y + 20), Size = new Size(400, 50), Tag = "Action" };
            btn.Click += Btn_Save_Click;
            formCard.Controls.Add(btn);

            pnlAdd.Controls.Add(formCard);
            pnlAdd.Resize += (s, e) => { formCard.Left = (pnlAdd.Width - formCard.Width) / 2; };

            contentPanel.Controls.Add(pnlAdd);
        }

        private Panel MakeInput(string l, out TextBox t, ref int y)
        {
            Panel p = new Panel { Location = new Point(50, y), Size = new Size(400, 75) };
            Label lbl = new Label { Text = l, Dock = DockStyle.Top, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray };
            t = new TextBox { Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 14), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
            p.Controls.Add(t); p.Controls.Add(lbl);
            y += 90; return p;
        }

        // --- GRID ---
        private void InitGrid()
        {
            pnlInv = new Panel { Dock = DockStyle.Fill, Visible = false };
            Label head = new Label { Text = "Current Inventory", Font = new Font("Segoe UI", 26, FontStyle.Bold), Dock = DockStyle.Top, Height = 80, ForeColor = Color.FromArgb(40, 40, 60) };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                RowTemplate = { Height = 40 }
            };

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 50, 90);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            grid.ColumnHeadersHeight = 50;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 180, 130);
            grid.DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);

            pnlInv.Controls.Add(grid);
            pnlInv.Controls.Add(head);
            contentPanel.Controls.Add(pnlInv);
        }

        // --- BUTTON HANDLERS ---
        private void Btn_Save_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) throw new Exception("Name required");
                int q = int.Parse(txtQty.Text);
                int u = int.Parse(txtUrg.Text);
                if (u < 1 || u > 5) throw new Exception("Urgency 1-5");

                system.Inventory.Add(new Supply { Name = txtName.Text, Quantity = q, Urgency = u });
                system.OrderQueue.Enqueue("Order for " + txtName.Text);
                MessageBox.Show("Success!");
                txtName.Clear(); txtQty.Clear(); txtUrg.Clear();
            }
            catch { MessageBox.Show("Invalid Input"); }
        }

        private void Btn_Sort_Click(object sender, EventArgs e)
        {
            var r = MessageBox.Show("Sort by Urgency? (Yes)\nSort by Quantity? (No)", "Sort", MessageBoxButtons.YesNoCancel);
            if (r == DialogResult.Cancel) return;
            system.SortInventory(r == DialogResult.Yes, true);
            ShowInventory();
        }

        private void Btn_Process_Click(object sender, EventArgs e)
        {
            if (system.OrderQueue.Count > 0) { MessageBox.Show("✅ Processed: " + system.OrderQueue.Dequeue()); ShowDashboard(); }
            else MessageBox.Show("Queue Empty");
        }

        private void HideAll() { pnlAdd.Visible = false; pnlInv.Visible = false; pnlDash.Visible = false; pnlSearch.Visible = false; }
        private void ShowAddPanel() { HideAll(); pnlAdd.Visible = true; }
        private void ShowSearchPanel() { HideAll(); pnlSearch.Visible = true; pnlResultCard.Visible = false; txtSearch.Clear(); }
        private void ShowInventory() { HideAll(); pnlInv.Visible = true; grid.DataSource = null; grid.DataSource = system.Inventory; }
        private void ShowDashboard()
        {
            HideAll(); pnlDash.Visible = true;
            lblTotal.Text = system.Inventory.Count.ToString();
            lblUrgent.Text = system.Inventory.Count(x => x.Urgency == 5).ToString();
            lblOrders.Text = system.OrderQueue.Count.ToString();
        }

        // -----------------------
        // Design: custom chrome
        // -----------------------

        private void Form1_Load(object sender, EventArgs e)
        {
            formSize = this.ClientSize;
        }

        // Drag Form (allow dragging by title label)
        private void panelTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        // Overridden methods to support resizing, snap, minimize/restore size preservation
        protected override void WndProc(ref Message m)
        {
            const int WM_NCCALCSIZE = 0x0083; // Standard Title Bar - Snap Window
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020; // Minimize form (Before)
            const int SC_RESTORE = 0xF120; // Restore form (Before)
            const int WM_NCHITTEST = 0x0084; // Determine which part of window corresponds to a point (resizing)
            const int resizeAreaSize = 10;

            #region Form Resize
            // Resize/WM_NCHITTEST values
            const int HTCLIENT = 1;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if (this.WindowState == FormWindowState.Normal)
                {
                    if ((int)m.Result == HTCLIENT)
                    {
                        Point screenPoint = new Point(m.LParam.ToInt32());
                        Point clientPoint = this.PointToClient(screenPoint);

                        if (clientPoint.Y <= resizeAreaSize)
                        {
                            if (clientPoint.X <= resizeAreaSize)
                                m.Result = (IntPtr)HTTOPLEFT;
                            else if (clientPoint.X < (this.Size.Width - resizeAreaSize))
                                m.Result = (IntPtr)HTTOP;
                            else
                                m.Result = (IntPtr)HTTOPRIGHT;
                        }
                        else if (clientPoint.Y <= (this.Size.Height - resizeAreaSize))
                        {
                            if (clientPoint.X <= resizeAreaSize)
                                m.Result = (IntPtr)HTLEFT;
                            else if (clientPoint.X > (this.Width - resizeAreaSize))
                                m.Result = (IntPtr)HTRIGHT;
                        }
                        else
                        {
                            if (clientPoint.X <= resizeAreaSize)
                                m.Result = (IntPtr)HTBOTTOMLEFT;
                            else if (clientPoint.X < (this.Size.Width - resizeAreaSize))
                                m.Result = (IntPtr)HTBOTTOM;
                            else
                                m.Result = (IntPtr)HTBOTTOMRIGHT;
                        }
                    }
                }
                return;
            }
            #endregion

            // Remove border and keep snap window
            if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
            {
                return;
            }

            // Keep form size when it is minimized and restored.
            if (m.Msg == WM_SYSCOMMAND)
            {
                int wParam = (m.WParam.ToInt32() & 0xFFF0);

                if (wParam == SC_MINIMIZE)
                    formSize = this.ClientSize;
                if (wParam == SC_RESTORE)
                    this.Size = formSize;
            }
            base.WndProc(ref m);
        }

        // Private methods from design
        private void AdjustForm()
        {
            switch (this.WindowState)
            {
                case FormWindowState.Maximized:
                    this.Padding = new Padding(8, 8, 8, 0);
                    break;
                case FormWindowState.Normal:
                    if (this.Padding.Top != borderSize)
                        this.Padding = new Padding(borderSize);
                    break;
            }
        }

        private void CollapseMenu()
        {
            // Collapse/expand the left sidebar. Works with ModernButton instances added to sidebar.
            if (this.sidebar.Width > 200)
            {
                // Collapse menu
                sidebar.Width = 100;
                if (lblTitle != null) lblTitle.Visible = false;
                foreach (Button menuButton in sidebar.Controls.OfType<Button>())
                {
                    menuButton.Text = "";
                    menuButton.ImageAlign = ContentAlignment.MiddleCenter;
                    menuButton.Padding = new Padding(0);
                }
            }
            else
            {
                // Expand menu
                sidebar.Width = 230;
                if (lblTitle != null) lblTitle.Visible = true;
                foreach (Button menuButton in sidebar.Controls.OfType<Button>())
                {
                    if (menuButton.Tag != null)
                    {
                        menuButton.Text = "   " + menuButton.Tag.ToString();
                    }
                    menuButton.ImageAlign = ContentAlignment.MiddleLeft;
                    menuButton.Padding = new Padding(10, 0, 0, 0);
                }
            }
        }

        // Event methods from design
        private void Form1_Resize(object sender, EventArgs e)
        {
            AdjustForm();
        }

        private void btnMenu_Click(object sender, EventArgs e)
        {
            CollapseMenu(); 
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            formSize = this.ClientSize;
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                formSize = this.ClientSize;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.Size = formSize;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}