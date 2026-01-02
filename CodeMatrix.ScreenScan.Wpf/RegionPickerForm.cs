using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CodeMatrix.ScreenScan.Wpf;

internal static class ScreenRegionPicker {
    public static bool TryPick(out Rectangle region) {
        using var form = new RegionPickerForm();
        var result = form.ShowDialog();
        if (result == DialogResult.OK && form.SelectedRegion.Width > 0 && form.SelectedRegion.Height > 0) {
            region = form.SelectedRegion;
            return true;
        }

        region = default;
        return false;
    }

    private sealed class RegionPickerForm : Form {
        private readonly Rectangle _virtual;
        private readonly Screen[] _screens;

        private bool _dragging;
        private Point _dragStart;
        private Point _dragCurrent;
        private Rectangle _selectionClient;

        public Rectangle SelectedRegion { get; private set; }

        public RegionPickerForm() {
            _screens = Screen.AllScreens;
            _virtual = SystemInformation.VirtualScreen;

            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            KeyPreview = true;

            Bounds = _virtual;
            BackColor = Color.Black;
            Opacity = 0.35;
            Cursor = Cursors.Cross;
        }

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            Activate();
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragStart = e.Location;
            _dragCurrent = e.Location;
            _selectionClient = Rectangle.Empty;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);

            if (!_dragging) return;
            _dragCurrent = e.Location;
            _selectionClient = Normalize(_dragStart, _dragCurrent);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left) return;
            if (!_dragging) return;

            _dragging = false;
            _dragCurrent = e.Location;
            _selectionClient = Normalize(_dragStart, _dragCurrent);
            Invalidate();

            // Require a minimal size to avoid accidental clicks.
            if (_selectionClient.Width < 20 || _selectionClient.Height < 20) {
                _selectionClient = Rectangle.Empty;
                return;
            }

            SelectedRegion = new Rectangle(
                x: _virtual.X + _selectionClient.X,
                y: _virtual.Y + _selectionClient.Y,
                width: _selectionClient.Width,
                height: _selectionClient.Height);

            DialogResult = DialogResult.OK;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (keyData == Keys.Escape) {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            DrawMonitorNumbers(e.Graphics);
            DrawInstructions(e.Graphics);

            if (_selectionClient.Width > 0 && _selectionClient.Height > 0) {
                using var fill = new SolidBrush(Color.FromArgb(50, 255, 255, 255));
                e.Graphics.FillRectangle(fill, _selectionClient);

                using var pen = new Pen(Color.Red, 3);
                e.Graphics.DrawRectangle(pen, _selectionClient);
            }
        }

        private void DrawMonitorNumbers(Graphics g) {
            using var font = new Font(FontFamily.GenericSansSerif, 96, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
            using var shadow = new SolidBrush(Color.FromArgb(140, 0, 0, 0));

            for (var i = 0; i < _screens.Length; i++) {
                var s = _screens[i];
                var b = s.Bounds;
                var r = new Rectangle(b.X - _virtual.X, b.Y - _virtual.Y, b.Width, b.Height);

                // Draw a subtle inset border so monitor boundaries are visible.
                using (var border = new Pen(Color.FromArgb(120, 255, 255, 255), 2)) {
                    var inset = Rectangle.Inflate(r, -3, -3);
                    if (inset.Width > 0 && inset.Height > 0) g.DrawRectangle(border, inset);
                }

                var label = (i + 1).ToString();
                var size = g.MeasureString(label, font);
                var x = r.Left + (r.Width - size.Width) / 2;
                var y = r.Top + (r.Height - size.Height) / 2;

                g.DrawString(label, font, shadow, x + 4, y + 4);
                g.DrawString(label, font, brush, x, y);
            }
        }

        private void DrawInstructions(Graphics g) {
            const string text = "Drag to select capture region • Esc to cancel";

            using var font = new Font(FontFamily.GenericSansSerif, 18, FontStyle.Bold, GraphicsUnit.Pixel);
            var size = g.MeasureString(text, font);
            var pad = 10;
            var rect = new RectangleF(pad, pad, size.Width + pad * 2, size.Height + pad * 2);

            using var bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
            using var fg = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
            g.FillRectangle(bg, rect);
            g.DrawString(text, font, fg, rect.Left + pad, rect.Top + pad);
        }

        private static Rectangle Normalize(Point a, Point b) {
            var x = Math.Min(a.X, b.X);
            var y = Math.Min(a.Y, b.Y);
            var w = Math.Abs(a.X - b.X);
            var h = Math.Abs(a.Y - b.Y);
            return new Rectangle(x, y, w, h);
        }
    }
}
