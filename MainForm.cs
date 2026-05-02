using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// iText7 — PAdES imzalama  //imzalama kütüphaneleri yükledim. 
using iText.Kernel.Pdf;
using iText.Signatures;
using iText.Commons.Bouncycastle.Cert;
using iText.Bouncycastle.X509;

// PKCS#11 token erişimi //akis tokeni için pkcs#11 arayüzü kullandım. AKİS'in kendi SDK'sı olmadığı için bu yöntemle doğrudan token üzerinden imzalama yapıyorum.
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;

// BouncyCastle sertifika okuma 
using Org.BouncyCastle.X509; //AKİS tokenindeki sertifikayı okumak ve bilgilerini göstermek için BouncyCastle kütüphanesini kullandım. PKCS#11 arayüzü sertifika okuma konusunda sınırlı olduğu için bu kütüphane ile sertifika detaylarını elde ediyorum.


namespace AKISImza
{

// ═══════════════════════════════════════════════════════════════════
// ANA FORM
// ═══════════════════════════════════════════════════════════════════
public class MainForm : Form
{
    // Sol nav
    private Panel  _navPanel  = null!;
    private NavBtn _navHome   = null!;
    private NavBtn _navSign   = null!;
    private NavBtn _navVerify = null!;

    // Sayfalar
    private Panel _pageHome   = null!;
    private Panel _pageSign   = null!;
    private Panel _pageVerify = null!;

    // İmzala sayfası
    private TextBox     _txtSourceDir  = null!;
    private Label       _lblPdfCount   = null!;
    private TextBox     _txtDllPath    = null!;
    private TextBox     _txtPin        = null!;
    private Button      _btnLoadCert   = null!;
    private Panel       _pnlCertCard   = null!;
    private Label       _lblCertName   = null!;
    private Label       _lblCertSerial = null!;
    private Label       _lblCertValid  = null!;
    private TextBox     _txtTargetDir  = null!;
    private Button      _btnSignAll    = null!;
    private ProgressBar _progress      = null!;
    private Label       _lblProgText   = null!;
    private RichTextBox _log           = null!;

    // State
    private CertInfo? _cert;

    public MainForm()
    {
        Text          = "AKİS Elektronik İmza — Toplu PDF İmzalama";
        Size          = new Size(1150, 760);
        MinimumSize   = new Size(900, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font          = new Font("Segoe UI", 9F);
        BackColor     = Color.White;

        BuildNav();
        BuildPageHome();
        BuildPageSign();
        BuildPageVerify();

        Controls.Add(_pageHome);
        Controls.Add(_pageSign);
        Controls.Add(_pageVerify);
        Controls.Add(_navPanel);

        ShowPage("home");
    }

    // ── SOL NAVİGASYON ──────────────────────────────────────────────

    private void BuildNav()
    {
        _navPanel = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 88,
            BackColor = Color.FromArgb(243, 245, 249)
        };

        var logo = new Panel { Dock = DockStyle.Top, Height = 60,
            BackColor = Color.FromArgb(30, 90, 160) };
        logo.Controls.Add(new Label { Text = "🔐",
            Font = new Font("Segoe UI", 22F), ForeColor = Color.White,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent });
        _navPanel.Controls.Add(logo);

        _navHome   = new NavBtn("🏠", "Başlangıç", 60);
        _navSign   = new NavBtn("📑", "İmzala",    150);
        _navVerify = new NavBtn("✅", "Doğrula",   240);

        _navHome.Click   += (s, e) => ShowPage("home");
        _navSign.Click   += (s, e) => ShowPage("sign");
        _navVerify.Click += (s, e) => ShowPage("verify");

        _navPanel.Controls.Add(_navHome);
        _navPanel.Controls.Add(_navSign);
        _navPanel.Controls.Add(_navVerify);
    }

    // ── ANA SAYFA ───────────────────────────────────────────────────

    private void BuildPageHome()
    {
        _pageHome = NewPage();
        int x = 50, y = 40;

        _pageHome.Controls.Add(new Label {
            Text = GreetText() + "  👋",
            Font = new Font("Segoe UI Light", 26F),
            ForeColor = Color.FromArgb(40, 55, 80),
            Location = new Point(x, y), AutoSize = true });

        _pageHome.Controls.Add(new Label {
            Text = "Ne yapmak istersiniz?",
            Font = new Font("Segoe UI", 12F),
            ForeColor = Color.FromArgb(110, 120, 140),
            Location = new Point(x, y + 52), AutoSize = true });

        var c1 = new CardBtn("📑", "Toplu PDF İmzala",
            "Klasördeki tüm PDF'leri\notomatik olarak imzalar")
            { Location = new Point(x, y + 110) };
        c1.Click += (s, e) => ShowPage("sign");
        _pageHome.Controls.Add(c1);

        var c2 = new CardBtn("✅", "İmza Doğrula",
            "İmzalı PDF dosyasını\nkontrol eder")
            { Location = new Point(x + 240, y + 110) };
        c2.Click += (s, e) => ShowPage("verify");
        _pageHome.Controls.Add(c2);

        var box = new Panel { Location = new Point(x, y + 370),
            Size = new Size(720, 155), BackColor = Color.FromArgb(248, 251, 255) };
        box.Paint += BPaint(Color.FromArgb(210, 225, 245));
        _pageHome.Controls.Add(box);

        box.Controls.Add(new Label {
            Text = "💡  Nasıl Çalışır?",
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Color.FromArgb(30, 90, 160),
            Location = new Point(18, 14), AutoSize = true });

        box.Controls.Add(new Label {
            Text =
              "1.  \"İmzala\" sekmesine geçin\n" +
              "2.  PDF dosyalarının bulunduğu klasörü seçin  →  kaç dosya olduğu görünür\n" +
              "3.  AKİS DLL yolunu girin, PIN ile \"Sertifikayı Yükle\" butonuna tıklayın\n" +
              "     Sertifika sahibi adı ve seri numarası yeşil kutuda gösterilir\n" +
              "4.  İmzalı dosyaların kaydedileceği klasörü seçin\n" +
              "5.  \"Tümünü İmzala\" butonuna tıklayın  →  her PDF  →  isim+imzali.pdf",
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(60, 75, 100),
            Location = new Point(18, 44), AutoSize = true });
    }

    // ── TOPLU İMZALA ────────────────────────────────────────────────

    private void BuildPageSign()
    {
        _pageSign = NewPage();
        int lx = 40, y = 22;

        _pageSign.Controls.Add(new Label {
            Text = "📑  Toplu PDF İmzalama",
            Font = new Font("Segoe UI Light", 22F),
            ForeColor = Color.FromArgb(40, 55, 80),
            Location = new Point(lx, y), AutoSize = true });
        y += 50;

        // ── 1. PDF KLASÖRÜ ──
        _pageSign.Controls.Add(Badge(1, lx, y));
        _pageSign.Controls.Add(BoldLbl("PDF Klasörü  (imzalanacak dosyalar)", lx + 38, y + 3));
        y += 34;

        _txtSourceDir = TB(lx, y, 640);
        _txtSourceDir.TextChanged += OnSourceChanged;
        _pageSign.Controls.Add(_txtSourceDir);

        var bSrc = SBtn("📂  Gözat", lx + 648, y - 1, 120, 30);
        bSrc.Click += (s, e) => {
            using var d = new FolderBrowserDialog
                { Description = "PDF dosyalarının olduğu klasörü seçin" };
            if (d.ShowDialog() == DialogResult.OK) _txtSourceDir.Text = d.SelectedPath;
        };
        _pageSign.Controls.Add(bSrc);
        y += 32;

        _lblPdfCount = new Label {
            Text = "Henüz klasör seçilmedi",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(130, 140, 160),
            Location = new Point(lx, y), AutoSize = true };
        _pageSign.Controls.Add(_lblPdfCount);
        y += 30;

        // ── 2. SERTİFİKA ──
        _pageSign.Controls.Add(Badge(2, lx, y));
        _pageSign.Controls.Add(BoldLbl("E-İmza Sertifikası", lx + 38, y + 3));
        y += 34;

        SmLbl(_pageSign, "AKİS DLL Yolu:", lx, y); y += 18;
        _txtDllPath = TB(lx, y, 550);
        _txtDllPath.Text = FindDll();
        _pageSign.Controls.Add(_txtDllPath);

        var bDll = SBtn("Gözat", lx + 558, y - 1, 80, 30);
        bDll.Click += (s, e) => {
            using var d = new OpenFileDialog { Filter = "DLL (*.dll)|*.dll" };
            if (d.ShowDialog() == DialogResult.OK) _txtDllPath.Text = d.FileName;
        };
        _pageSign.Controls.Add(bDll);
        y += 36;

        SmLbl(_pageSign, "Token PIN:", lx, y); y += 18;
        _txtPin = TB(lx, y, 320);
        _txtPin.UseSystemPasswordChar = true;
        _txtPin.Font = new Font("Segoe UI", 11F);
        _pageSign.Controls.Add(_txtPin);

        _btnLoadCert = new Button {
            Text = "🔑  Sertifikayı Yükle",
            Location = new Point(lx + 330, y - 2), Size = new Size(210, 34),
            BackColor = Color.FromArgb(60, 140, 100), ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        _btnLoadCert.FlatAppearance.BorderSize = 0;
        _btnLoadCert.Click += OnLoadCert;
        _pageSign.Controls.Add(_btnLoadCert);
        y += 46;

        // Sertifika kartı
        _pnlCertCard = new Panel {
            Location = new Point(lx, y), Size = new Size(770, 95),
            BackColor = Color.FromArgb(244, 251, 246), Visible = false };
        _pnlCertCard.Paint += BPaint(Color.FromArgb(170, 215, 185));
        _pageSign.Controls.Add(_pnlCertCard);

        _pnlCertCard.Controls.Add(new Label {
            Text = "✅  Sertifika Yüklendi",
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Color.FromArgb(40, 120, 70),
            Location = new Point(14, 10), AutoSize = true });

        CertRow(_pnlCertCard, "E-İmza Sahibi:", out _lblCertName,   32);
        CertRow(_pnlCertCard, "Seri Numarası:", out _lblCertSerial, 54);
        CertRow(_pnlCertCard, "Geçerlilik:",    out _lblCertValid,  74);
        y += 105;

        // ── 3. HEDEF KLASÖR ──
        _pageSign.Controls.Add(Badge(3, lx, y));
        _pageSign.Controls.Add(BoldLbl("Kayıt Klasörü  (imzalı dosyalar buraya kaydedilir)", lx + 38, y + 3));
        y += 34;

        _txtTargetDir = TB(lx, y, 640);
        _txtTargetDir.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ImzaliPDFler");
        _pageSign.Controls.Add(_txtTargetDir);

        var bTgt = SBtn("📂  Gözat", lx + 648, y - 1, 120, 30);
        bTgt.Click += (s, e) => {
            using var d = new FolderBrowserDialog {
                Description = "İmzalı PDF'lerin kaydedileceği klasör",
                SelectedPath = Directory.Exists(_txtTargetDir.Text) ? _txtTargetDir.Text : ""
            };
            if (d.ShowDialog() == DialogResult.OK) _txtTargetDir.Text = d.SelectedPath;
        };
        _pageSign.Controls.Add(bTgt);
        y += 50;

        // ── İMZALA BUTONU ──
        _btnSignAll = new Button {
            Text = "🔏   TÜM PDF'LERİ İMZALA",
            Location = new Point(lx, y), Size = new Size(770, 52),
            BackColor = Color.FromArgb(30, 110, 200), ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 13F),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        _btnSignAll.FlatAppearance.BorderSize = 0;
        _btnSignAll.Click += OnSignAll;
        _pageSign.Controls.Add(_btnSignAll);
        y += 62;

        _progress = new ProgressBar {
            Location = new Point(lx, y), Size = new Size(770, 8),
            Style = ProgressBarStyle.Continuous, Visible = false };
        _pageSign.Controls.Add(_progress);
        y += 12;

        _lblProgText = new Label {
            Location = new Point(lx, y), Size = new Size(770, 22),
            Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(80, 100, 130) };
        _pageSign.Controls.Add(_lblProgText);
        y += 26;

        _log = new RichTextBox {
            Location = new Point(lx, y), Size = new Size(770, 150),
            ReadOnly = true, Font = new Font("Consolas", 9F),
            BackColor = Color.FromArgb(22, 27, 38),
            ForeColor = Color.FromArgb(210, 218, 235),
            BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical };
        _pageSign.Controls.Add(_log);
    }

    // ── DOĞRULAMA ───────────────────────────────────────────────────

    private void BuildPageVerify()
    {
        _pageVerify = NewPage();
        int x = 50, y = 40;

        _pageVerify.Controls.Add(new Label {
            Text = "✅  İmza Doğrulama",
            Font = new Font("Segoe UI Light", 22F),
            ForeColor = Color.FromArgb(40, 55, 80),
            Location = new Point(x, y), AutoSize = true });

        _pageVerify.Controls.Add(new Label {
            Text = "İmzalı PDF dosyasını seçerek imza bilgilerini kontrol edebilirsiniz.",
            Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(110, 120, 140),
            Location = new Point(x, y + 50), AutoSize = true });

        var btn = new Button {
            Text = "📂   Dosya Seç ve Doğrula",
            Location = new Point(x, y + 100), Size = new Size(340, 48),
            BackColor = Color.FromArgb(30, 110, 200), ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11F),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += OnVerify;
        _pageVerify.Controls.Add(btn);
    }

    // ═══════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════

    private void OnSourceChanged(object? sender, EventArgs e)
    {
        string path = _txtSourceDir.Text.Trim();
        if (!Directory.Exists(path)) {
            _lblPdfCount.Text = "Geçerli bir klasör yolu girin";
            _lblPdfCount.ForeColor = Color.FromArgb(130, 140, 160);
            return;
        }
        int n = Directory.GetFiles(path, "*.pdf", SearchOption.TopDirectoryOnly).Length;
        _lblPdfCount.Text = n > 0
            ? $"📄  {n} adet PDF dosyası bulundu"
            : "⚠️  Bu klasörde PDF yok";
        _lblPdfCount.ForeColor = n > 0
            ? Color.FromArgb(40, 130, 70)
            : Color.FromArgb(190, 120, 30);
    }

    private async void OnLoadCert(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtPin.Text)) {
            Warn("Lütfen PIN kodunu girin."); _txtPin.Focus(); return;
        }

        _btnLoadCert.Enabled = false;
        _btnLoadCert.Text    = "Yükleniyor...";
        Cursor = Cursors.WaitCursor;

        try
        {
            string dll = _txtDllPath.Text.Trim();
            string pin = _txtPin.Text;

            var info = await Task.Run(() => TokenHelper.ReadCert(dll, pin));
            _cert = info;

            _lblCertName.Text   = info.Subject;
            _lblCertSerial.Text = info.Serial;
            _lblCertValid.Text  = $"{info.ValidFrom:dd.MM.yyyy}  –  {info.ValidTo:dd.MM.yyyy}";
            _lblCertValid.ForeColor = info.ValidTo < DateTime.Now
                ? Color.FromArgb(200, 50, 50)
                : info.ValidTo < DateTime.Now.AddDays(30)
                    ? Color.FromArgb(200, 130, 30)
                    : Color.FromArgb(40, 120, 70);

            _pnlCertCard.Visible = true;
        }
        catch (Exception ex)
        {
            Err("Sertifika okunamadı:\n\n" + ex.Message +
                "\n\n• Token takılı mı?\n• PIN doğru mu?\n• DLL yolu geçerli mi?");
        }
        finally
        {
            _btnLoadCert.Enabled = true;
            _btnLoadCert.Text    = "🔑  Sertifikayı Yükle";
            Cursor = Cursors.Default;
        }
    }

    private async void OnSignAll(object? sender, EventArgs e)
    {
        if (!Directory.Exists(_txtSourceDir.Text)) { Err("Geçerli kaynak klasör seçin."); return; }
        if (_cert == null) { Err("Önce sertifikayı yükleyin."); return; }
        if (string.IsNullOrWhiteSpace(_txtTargetDir.Text)) { Err("Kayıt klasörünü belirtin."); return; }
        if (string.IsNullOrWhiteSpace(_txtPin.Text)) { Err("PIN kodunu girin."); _txtPin.Focus(); return; }

        var pdfs = Directory.GetFiles(_txtSourceDir.Text, "*.pdf", SearchOption.TopDirectoryOnly);
        if (pdfs.Length == 0) { Err("Kaynak klasörde PDF yok."); return; }

        if (MessageBox.Show(
            $"{pdfs.Length} adet PDF imzalanacak.\n\n" +
            $"Kaynak:  {_txtSourceDir.Text}\n" +
            $"Hedef:   {_txtTargetDir.Text}\n\n" +
            "Devam edilsin mi?",
            "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        Directory.CreateDirectory(_txtTargetDir.Text);

        _btnSignAll.Enabled = false;
        _progress.Visible   = true;
        _progress.Maximum   = pdfs.Length;
        _progress.Value     = 0;
        _log.Clear();
        Cursor = Cursors.WaitCursor;

        int ok = 0, fail = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        string dll        = _txtDllPath.Text.Trim();
        string pin        = _txtPin.Text;
        string targetDir  = _txtTargetDir.Text;
        string signerName = _cert.Subject;
        byte[] certDer    = _cert.CertDer;

        Log($"► Başlatıldı: {pdfs.Length} dosya", Color.FromArgb(120, 180, 255));
        Log($"  Sahip: {signerName}", Color.FromArgb(170, 180, 200));
        Log($"  Format: PAdES-B-B (ETSI.CAdES.detached)", Color.FromArgb(170, 180, 200));

        for (int i = 0; i < pdfs.Length; i++)
        {
            string src      = pdfs[i];
            string name     = Path.GetFileName(src);
            string baseName = Path.GetFileNameWithoutExtension(src);
            string dst      = Path.Combine(targetDir, $"{baseName}+imzali.pdf");

            _lblProgText.Text = $"İşleniyor ({i + 1}/{pdfs.Length}):  {name}";
            _progress.Value   = i + 1;
            Application.DoEvents();

            try
            {
                byte[] signed = await Task.Run(() =>
                    PadesHelper.Sign(src, dll, pin, certDer, signerName));

                await File.WriteAllBytesAsync(dst, signed);
                Log($"✓  {name}  →  {Path.GetFileName(dst)}", Color.FromArgb(120, 220, 130));
                ok++;
            }
            catch (Exception ex)
            {
                Log($"✗  {name}: {ex.Message}", Color.FromArgb(255, 110, 110));
                fail++;
            }
        }

        sw.Stop();
        Log("", Color.White);
        Log($"══ Tamamlandı ══  Başarılı: {ok}  Başarısız: {fail}  " +
            $"Süre: {sw.Elapsed.TotalSeconds:F1}sn",
            Color.FromArgb(120, 180, 255));

        _lblProgText.Text   = $"Tamamlandı  •  {ok} başarılı, {fail} başarısız  •  {sw.Elapsed.TotalSeconds:F1} sn";
        _btnSignAll.Enabled = true;
        Cursor = Cursors.Default;

        if (ok > 0 && MessageBox.Show(
            $"{ok} PDF başarıyla imzalandı ve kaydedildi.\n\nKlasörü açmak ister misiniz?",
            "Tamamlandı", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
        {
            System.Diagnostics.Process.Start("explorer.exe", targetDir);
        }
    }

    private async void OnVerify(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog {
            Filter = "PDF Dosyaları (*.pdf)|*.pdf",
            Title  = "Doğrulanacak imzalı PDF'i seçin"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        Cursor = Cursors.WaitCursor;
        try
        {
            string path = dlg.FileName;
            var result  = await Task.Run(() => PadesHelper.Verify(path));

            if (result.HasSignature)
            {
                MessageBox.Show(
                    $"✅  Belgede elektronik imza bulundu.\n\n" +
                    $"İmzacı: {result.SignerName}\n" +
                    $"Tarih:  {result.SignTime:dd.MM.yyyy HH:mm}",
                    "İmza Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("❌  Belgede imza bulunamadı.",
                    "İmza Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex) { Err("Doğrulama hatası: " + ex.Message); }
        finally { Cursor = Cursors.Default; }
    }

    // ═══════════════════════════════════════════════════════════════
    // Yardımcılar
    // ═══════════════════════════════════════════════════════════════

    private void ShowPage(string p)
    {
        _pageHome.Visible   = p == "home";
        _pageSign.Visible   = p == "sign";
        _pageVerify.Visible = p == "verify";
        _navHome.Active     = p == "home";
        _navSign.Active     = p == "sign";
        _navVerify.Active   = p == "verify";
        _navHome.Refresh(); _navSign.Refresh(); _navVerify.Refresh();
    }

    private void Log(string msg, Color color)
    {
        _log.SelectionStart  = _log.TextLength;
        _log.SelectionLength = 0;
        _log.SelectionColor  = color;
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}]  {msg}\n");
        _log.ScrollToCaret();
    }

    private static Panel NewPage() => new Panel {
        Dock = DockStyle.Fill, BackColor = Color.White,
        AutoScroll = true, Visible = false };

    private static TextBox TB(int x, int y, int w) => new TextBox {
        Location = new Point(x, y), Size = new Size(w, 28),
        Font = new Font("Segoe UI", 10F), BorderStyle = BorderStyle.FixedSingle };

    private static Button SBtn(string t, int x, int y, int w, int h)
    {
        var b = new Button {
            Text = t, Location = new Point(x, y), Size = new Size(w, h),
            BackColor = Color.FromArgb(230, 237, 250), ForeColor = Color.FromArgb(50, 70, 110),
            Font = new Font("Segoe UI", 9F), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = Color.FromArgb(190, 210, 235);
        return b;
    }

    private static Panel Badge(int n, int x, int y)
    {
        var p = new Panel { Location = new Point(x, y), Size = new Size(28, 28) };
        p.Paint += (s, e) => {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(30, 110, 200)), 0, 0, 27, 27);
            using var sf = new StringFormat
                { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(n.ToString(), new Font("Segoe UI Semibold", 11F),
                Brushes.White, new RectangleF(0, 0, 28, 28), sf);
        };
        return p;
    }

    private static Label BoldLbl(string t, int x, int y) => new Label {
        Text = t, Font = new Font("Segoe UI Semibold", 11F),
        ForeColor = Color.FromArgb(40, 55, 80), Location = new Point(x, y), AutoSize = true };

    private static void SmLbl(Control p, string t, int x, int y) =>
        p.Controls.Add(new Label {
            Text = t, Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(110, 125, 150),
            Location = new Point(x, y), AutoSize = true });

    private static void CertRow(Control parent, string label, out Label value, int y)
    {
        parent.Controls.Add(new Label {
            Text = label, Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(80, 100, 120),
            Location = new Point(14, y), AutoSize = true });
        value = new Label {
            Text = "-", Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(30, 50, 80),
            Location = new Point(115, y), Size = new Size(630, 18),
            AutoEllipsis = true };
        parent.Controls.Add(value);
    }

    private static PaintEventHandler BPaint(Color c) =>
        (s, e) => {
            var ctrl = (Control)s!;
            using var pen = new Pen(c, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
        };

    private static CardBtn NewCard(string icon, string title, string desc, int x, int y) =>
        new CardBtn(icon, title, desc) { Location = new Point(x, y) };

    private static string GreetText() {
        int h = DateTime.Now.Hour;
        return h < 6 ? "İyi geceler" : h < 12 ? "Günaydın" : h < 18 ? "İyi günler" : "İyi akşamlar";
    }

    private static string FindDll()
    {
        var cands = new[] {
            Path.Combine(AppContext.BaseDirectory, "akisp11.dll"),
            @"C:\Windows\System32\akisp11.dll", "akisp11.dll"
        };
        return cands.FirstOrDefault(File.Exists) ?? "akisp11.dll";
    }

    private static void Warn(string m) =>
        MessageBox.Show(m, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    private static void Err(string m) =>
        MessageBox.Show(m, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

// ═══════════════════════════════════════════════════════════════════
// PKCS#11 Token yardımcısı
// ═══════════════════════════════════════════════════════════════════
record CertInfo(string Subject, string Serial, DateTime ValidFrom, DateTime ValidTo, byte[] CertDer);

static class TokenHelper
{
    public static CertInfo ReadCert(string dllPath, string pin)
    {
        var fac  = new Pkcs11InteropFactories();
        using var lib  = fac.Pkcs11LibraryFactory
            .LoadPkcs11Library(fac, dllPath, AppType.SingleThreaded);

        var slots = lib.GetSlotList(SlotsType.WithOrWithoutTokenPresent);
        if (!slots.Any()) throw new Exception("Token bulunamadı. AKİS tokeni takın.");

        using var session = slots.First().OpenSession(SessionType.ReadOnly);
        session.Login(CKU.CKU_USER, pin);

        var certAttrs = new List<IObjectAttribute> {
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_CERTIFICATE),
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CERTIFICATE_TYPE, CKC.CKC_X_509)
        };
        session.FindObjectsInit(certAttrs);
        var handles = session.FindObjects(10);
        session.FindObjectsFinal();

        if (!handles.Any()) throw new Exception("Token'da sertifika yok.");

        var parser = new X509CertificateParser();
        Org.BouncyCastle.X509.X509Certificate? sigCert = null;

        foreach (var h in handles)
        {
            var attrs = session.GetAttributeValue(h, new List<CKA> { CKA.CKA_VALUE });
            var bytes = attrs[0].GetValueAsByteArray();
            if (bytes == null) continue;
            var c  = parser.ReadCertificate(bytes);
            var ku = c.GetKeyUsage();
            if (ku != null && ku[1]) { sigCert = c; break; }  // nonRepudiation
            sigCert ??= c;
        }

        if (sigCert == null) throw new Exception("İmzalama sertifikası bulunamadı.");

        string subject = ExtractCN(sigCert.SubjectDN.ToString());
        string serial  = sigCert.SerialNumber.ToString(16).ToUpperInvariant();
        byte[] der     = sigCert.GetEncoded();

        return new CertInfo(subject, serial, sigCert.NotBefore, sigCert.NotAfter, der);
    }

    public static byte[] SignHash(string dllPath, string pin, byte[] digestInfo)
    {
        var fac  = new Pkcs11InteropFactories();
        using var lib  = fac.Pkcs11LibraryFactory
            .LoadPkcs11Library(fac, dllPath, AppType.SingleThreaded);

        var slots = lib.GetSlotList(SlotsType.WithOrWithoutTokenPresent);
        using var session = slots.First().OpenSession(SessionType.ReadOnly);
        session.Login(CKU.CKU_USER, pin);

        var keyAttrs = new List<IObjectAttribute> {
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_SIGN, true)
        };
        session.FindObjectsInit(keyAttrs);
        var keys = session.FindObjects(1);
        session.FindObjectsFinal();
        if (!keys.Any()) throw new Exception("İmzalama anahtarı bulunamadı.");

        var mech = session.Factories.MechanismFactory.Create(CKM.CKM_RSA_PKCS);
        return session.Sign(mech, keys.First(), digestInfo);
    }

    public static string ExtractCN(string dn)
    {
        foreach (var p in dn.Split(',')) {
            var t = p.Trim();
            if (t.StartsWith("CN=", StringComparison.OrdinalIgnoreCase)) return t[3..];
        }
        return dn;
    }
}

// ═══════════════════════════════════════════════════════════════════
// PAdES imzalama — iText7 ile (ETSI EN 319 132 uyumlu)
// ═══════════════════════════════════════════════════════════════════
record VerifyResult(bool HasSignature, string SignerName, DateTime SignTime);

static class PadesHelper
{
    // ─── BouncyCastle using alias'ları ───────────────────────────────
    // (ambiguous reference'ı önlemek için)
    private static readonly Org.BouncyCastle.Asn1.DerObjectIdentifier SHA256_OID =
        new Org.BouncyCastle.Asn1.DerObjectIdentifier("2.16.840.1.101.3.4.2.1");
    private static readonly Org.BouncyCastle.Asn1.DerObjectIdentifier RSA_OID =
        new Org.BouncyCastle.Asn1.DerObjectIdentifier("1.2.840.113549.1.1.1");
    private static readonly Org.BouncyCastle.Asn1.DerObjectIdentifier ESS_CERT_V2_OID =
        new Org.BouncyCastle.Asn1.DerObjectIdentifier("1.2.840.113549.1.9.16.2.47");

    // ─── SHA-256 DigestInfo prefix (RFC 3447) ────────────────────────
    private static readonly byte[] SHA256_DIGESTINFO_PREFIX = {
        0x30, 0x31, 0x30, 0x0d, 0x06, 0x09,
        0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01,
        0x05, 0x00, 0x04, 0x20
    };

    // ═══════════════════════════════════════════════════════════════
    // IExternalSignatureContainer — iText7'ye tam CMS döndürürüz
    // ═══════════════════════════════════════════════════════════════
    private class AkisCmsContainer : IExternalSignatureContainer
    {
        private readonly string _dll;
        private readonly string _pin;
        private readonly Org.BouncyCastle.X509.X509Certificate _cert;

        public AkisCmsContainer(string dll, string pin,
            Org.BouncyCastle.X509.X509Certificate cert)
        {
            _dll  = dll;
            _pin  = pin;
            _cert = cert;
        }

        // iText7 imza dict'ini hazırlar — biz SubFilter'ı ayarlıyoruz
        public void ModifySigningDictionary(iText.Kernel.Pdf.PdfDictionary signDic)
        {
            signDic.Put(iText.Kernel.Pdf.PdfName.Filter,
                new iText.Kernel.Pdf.PdfName("Adobe.PPKLite"));
            signDic.Put(iText.Kernel.Pdf.PdfName.SubFilter,
                new iText.Kernel.Pdf.PdfName("ETSI.CAdES.detached"));
        }

        // iText7 ByteRange içeriğini stream olarak verir, biz CMS döndürürüz
        public byte[] Sign(Stream rangeStream)
        {
            // 1. ByteRange baytlarını oku (imzalanacak gerçek içerik)
            using var ms = new MemoryStream();
            rangeStream.CopyTo(ms);
            byte[] toSign = ms.ToArray();

            // 2. İçerik hash'i (messageDigest için)
            byte[] contentHash = SHA256.HashData(toSign);

            // 3. Signed attributes oluştur (ESSCertIDv2 issuerSerial dahil)
            var signedAttrs = BuildSignedAttributes(contentHash, _cert);

            // 4. Signed attributes'ün DER encoding'ini hash'le (token'ın imzalayacağı şey)
            byte[] signedAttrsEncoded = signedAttrs.GetEncoded();
            byte[] signedAttrsHash    = SHA256.HashData(signedAttrsEncoded);

            // 5. DigestInfo yapısı (CKM_RSA_PKCS için PKCS#1 padding)
            byte[] digestInfo = BuildDigestInfo(signedAttrsHash);

            // 6. Token ile imzala (CKM_RSA_PKCS)
            byte[] rawSignature = TokenHelper.SignHash(_dll, _pin, digestInfo);

            // 7. Tam CMS SignedData yapısını oluştur
            return BuildCms(signedAttrs, rawSignature, _cert);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Ana imzalama metodu
    // ═══════════════════════════════════════════════════════════════
    public static byte[] Sign(
        string pdfPath,
        string dllPath,
        string pin,
        byte[] certDer,
        string signerName)
    {
        var bcCert = new Org.BouncyCastle.X509.X509CertificateParser()
            .ReadCertificate(certDer);

        using var reader = new PdfReader(pdfPath);
        using var output = new MemoryStream();

        // iText7 8.x — tüm imza meta bilgileri SignerProperties üzerinden
        var signerProps = new SignerProperties()
            .SetReason("Belge Onayı")
            .SetLocation("Türkiye")
            .SetContact(signerName)
            .SetFieldName($"Sig_{DateTime.Now:yyyyMMddHHmmss}");

        // AppendMode = orijinal PDF korunur, imza incremental olarak eklenir
        var signer = new PdfSigner(
            reader,
            output,
            (string?)null,
            new StampingProperties().UseAppendMode(),
            signerProps);

        // SignExternalContainer ile tam CMS kontrolü bizde
        var container = new AkisCmsContainer(dllPath, pin, bcCert);
        signer.SignExternalContainer(container, 16384); // 16KB rezerv

        return output.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════
    // Doğrulama
    // ═══════════════════════════════════════════════════════════════
    public static VerifyResult Verify(string pdfPath)
    {
        using var reader = new PdfReader(pdfPath);
        using var doc    = new PdfDocument(reader);

        var signUtil = new SignatureUtil(doc);
        var names    = signUtil.GetSignatureNames();

        if (names.Count == 0)
            return new VerifyResult(false, "", DateTime.MinValue);

        var pkcs7 = signUtil.ReadSignatureData(names[0]);

        string dn = pkcs7.GetSigningCertificate()?.GetSubjectDN()?.ToString() ?? "";
        string cn = TokenHelper.ExtractCN(dn);
        DateTime dt = pkcs7.GetSignDate().ToUniversalTime();

        return new VerifyResult(true, cn, dt);
    }

    // ═══════════════════════════════════════════════════════════════
    // CMS Signed Attributes — ESSCertIDv2 WITH issuerSerial
    // ETSI EN 319 122 — PAdES-B-B zorunlu alanlar
    // ═══════════════════════════════════════════════════════════════
    private static Org.BouncyCastle.Asn1.DerSet BuildSignedAttributes(
        byte[] contentHash,
        Org.BouncyCastle.X509.X509Certificate cert)
    {
        var v = new Org.BouncyCastle.Asn1.Asn1EncodableVector();

        // 1. id-contentType = data
        v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
            Org.BouncyCastle.Asn1.Cms.CmsAttributes.ContentType,
            new Org.BouncyCastle.Asn1.DerSet(new Org.BouncyCastle.Asn1.Asn1Encodable[]
                { Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Data })));

        // 2. signingTime
        v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
            Org.BouncyCastle.Asn1.Cms.CmsAttributes.SigningTime,
            new Org.BouncyCastle.Asn1.DerSet(new Org.BouncyCastle.Asn1.Asn1Encodable[]
                { new Org.BouncyCastle.Asn1.DerUtcTime(DateTime.UtcNow, 2049) })));

        // 3. messageDigest = SHA256(content)
        v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
            Org.BouncyCastle.Asn1.Cms.CmsAttributes.MessageDigest,
            new Org.BouncyCastle.Asn1.DerSet(new Org.BouncyCastle.Asn1.Asn1Encodable[]
                { new Org.BouncyCastle.Asn1.DerOctetString(contentHash) })));

        // 4. id-aa-signingCertificateV2 — ESSCertIDv2 WITH issuerSerial
        //    Bu alan eksikti → "Özellik issuer serial alanı içermiyor" hatası
        byte[] certHash   = SHA256.HashData(cert.GetEncoded());
        var    sha256AlgId = new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(
            SHA256_OID, Org.BouncyCastle.Asn1.DerNull.Instance);

        // IssuerSerial ::= SEQUENCE { issuer GeneralNames, serialNumber CertificateSerialNumber }
        var issuerGeneralName  = new Org.BouncyCastle.Asn1.X509.GeneralName(
            Org.BouncyCastle.Asn1.X509.GeneralName.DirectoryName, cert.IssuerDN);
        var issuerGeneralNames = new Org.BouncyCastle.Asn1.DerSequence(
            new Org.BouncyCastle.Asn1.Asn1Encodable[] { issuerGeneralName });
        var issuerSerial = new Org.BouncyCastle.Asn1.DerSequence(
            new Org.BouncyCastle.Asn1.Asn1Encodable[] {
                issuerGeneralNames,
                new Org.BouncyCastle.Asn1.DerInteger(cert.SerialNumber) // ← ZORUNLU
            });

        // ESSCertIDv2 ::= SEQUENCE { hashAlgorithm, certHash, issuerSerial }
        var essCertIdV2 = new Org.BouncyCastle.Asn1.DerSequence(
            new Org.BouncyCastle.Asn1.Asn1Encodable[] {
                sha256AlgId,
                new Org.BouncyCastle.Asn1.DerOctetString(certHash),
                issuerSerial  // ← DÜZELTME BURADA yapıldı son versiyonda, önceki versiyonlarda eksikti
            });

        // SigningCertificateV2 ::= SEQUENCE { SEQUENCE OF ESSCertIDv2 }
        var signingCertV2 = new Org.BouncyCastle.Asn1.DerSequence(
            new Org.BouncyCastle.Asn1.Asn1Encodable[] {
                new Org.BouncyCastle.Asn1.DerSequence(
                    new Org.BouncyCastle.Asn1.Asn1Encodable[] { essCertIdV2 })
            });

        v.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
            ESS_CERT_V2_OID,
            new Org.BouncyCastle.Asn1.DerSet(new Org.BouncyCastle.Asn1.Asn1Encodable[]
                { signingCertV2 })));

        return new Org.BouncyCastle.Asn1.DerSet(v);
    }

    // ═══════════════════════════════════════════════════════════════
    // CMS SignedData yapısı — ETSI CAdES detached
    // ═══════════════════════════════════════════════════════════════
    private static byte[] BuildCms(
        Org.BouncyCastle.Asn1.DerSet signedAttrs,
        byte[] rawSignature,
        Org.BouncyCastle.X509.X509Certificate cert)
    {
        var hashAlgId = new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(
            SHA256_OID, Org.BouncyCastle.Asn1.DerNull.Instance);
        var encAlgId = new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(
            RSA_OID, Org.BouncyCastle.Asn1.DerNull.Instance);

        // SignerIdentifier = IssuerAndSerialNumber
        var certStruct = Org.BouncyCastle.Asn1.X509.X509CertificateStructure.GetInstance(
            Org.BouncyCastle.Asn1.Asn1Object.FromByteArray(cert.GetEncoded()));
        var sid = new Org.BouncyCastle.Asn1.Cms.SignerIdentifier(
            new Org.BouncyCastle.Asn1.Cms.IssuerAndSerialNumber(certStruct));

        // SignerInfo
        var signerInfo = new Org.BouncyCastle.Asn1.Cms.SignerInfo(
            sid,
            hashAlgId,
            (Org.BouncyCastle.Asn1.Asn1Set)signedAttrs,
            encAlgId,
            new Org.BouncyCastle.Asn1.DerOctetString(rawSignature),
            (Org.BouncyCastle.Asn1.Asn1Set?)null);

        // Certificates
        var certSet = new Org.BouncyCastle.Asn1.DerSet(
            new Org.BouncyCastle.Asn1.Asn1Encodable[] { certStruct });

        // Detached — eContent yok
        var encapContent = new Org.BouncyCastle.Asn1.Cms.ContentInfo(
            Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Data, null);

        // SignedData
        var signedData = new Org.BouncyCastle.Asn1.Cms.SignedData(
            new Org.BouncyCastle.Asn1.DerSet(
                new Org.BouncyCastle.Asn1.Asn1Encodable[] { hashAlgId }),
            encapContent,
            certSet,
            (Org.BouncyCastle.Asn1.Asn1Set?)null,
            new Org.BouncyCastle.Asn1.DerSet(
                new Org.BouncyCastle.Asn1.Asn1Encodable[] { signerInfo }));

        var contentInfo = new Org.BouncyCastle.Asn1.Cms.ContentInfo(
            Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.SignedData, signedData);

        return contentInfo.GetEncoded();
    }

    // SHA-256 DigestInfo (CKM_RSA_PKCS için)
    private static byte[] BuildDigestInfo(byte[] hash)
    {
        var result = new byte[SHA256_DIGESTINFO_PREFIX.Length + hash.Length];
        Buffer.BlockCopy(SHA256_DIGESTINFO_PREFIX, 0, result, 0, SHA256_DIGESTINFO_PREFIX.Length);
        Buffer.BlockCopy(hash, 0, result, SHA256_DIGESTINFO_PREFIX.Length, hash.Length);
        return result;
    }
}

// ═══════════════════════════════════════════════════════════════════
// Sol nav butonu (custom paint)
// ═══════════════════════════════════════════════════════════════════
class NavBtn : Button
{
    private readonly string _icon, _label;
    public bool Active { get; set; }

    public NavBtn(string icon, string label, int top)
    {
        _icon = icon; _label = label;
        Location = new Point(0, top);
        Size     = new Size(88, 82);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Active ? Color.White : Color.FromArgb(243, 245, 249));

        if (Active)
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 110, 200)), 0, 0, 4, Height);

        using var sf = new StringFormat
            { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        Color clr = Active ? Color.FromArgb(30, 110, 200) : Color.FromArgb(80, 95, 120);

        using var fIcon = new Font("Segoe UI", 22F);
        g.DrawString(_icon, fIcon, new SolidBrush(clr),
            new RectangleF(0, 8, Width, 38), sf);

        using var fTxt = new Font("Segoe UI", 8F,
            Active ? FontStyle.Bold : FontStyle.Regular);
        g.DrawString(_label, fTxt, new SolidBrush(clr),
            new RectangleF(0, 48, Width, 28), sf);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!Active) { BackColor = Color.FromArgb(228, 234, 245); Invalidate(); }
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = Color.Transparent; Invalidate();
    }
}

// ═══════════════════════════════════════════════════════════════════
// Ana sayfa karo butonu (custom paint)
// ═══════════════════════════════════════════════════════════════════
class CardBtn : Button
{
    private readonly string _icon, _title, _desc;

    public CardBtn(string icon, string title, string desc)
    {
        _icon = icon; _title = title; _desc = desc;
        Size  = new Size(210, 230);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.White;
        Cursor    = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        using var pen = new Pen(Color.FromArgb(210, 225, 245), 1);
        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

        using var sf = new StringFormat
            { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        using var fIcon = new Font("Segoe UI", 44F);
        g.DrawString(_icon, fIcon, new SolidBrush(Color.FromArgb(30, 110, 200)),
            new RectangleF(0, 22, Width, 82), sf);

        using var fTitle = new Font("Segoe UI Semibold", 11F);
        g.DrawString(_title, fTitle, new SolidBrush(Color.FromArgb(40, 55, 80)),
            new RectangleF(10, 118, Width - 20, 28), sf);

        using var fDesc = new Font("Segoe UI", 8.5F);
        g.DrawString(_desc, fDesc, new SolidBrush(Color.FromArgb(100, 115, 140)),
            new RectangleF(12, 152, Width - 24, 70), sf);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e); BackColor = Color.FromArgb(245, 249, 255); Invalidate();
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e); BackColor = Color.White; Invalidate();
    }
}

} // namespace
