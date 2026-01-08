using System;
using System.IO;
using System.Windows;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Microsoft.Win32;

namespace CodeGlyphX.Demo.Wpf;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        Loaded += (_, _) => InitializeUi();
    }

    private void InitializeUi() {
        QrEccCombo.ItemsSource = Enum.GetValues(typeof(QrErrorCorrectionLevel));
        QrEccCombo.SelectedItem = QrErrorCorrectionLevel.M;
        BuildOtpUri();
    }

    private QrErrorCorrectionLevel SelectedEcc =>
        QrEccCombo.SelectedItem is QrErrorCorrectionLevel ecc ? ecc : QrErrorCorrectionLevel.M;

    private void BuildOtp_Click(object sender, RoutedEventArgs e) => BuildOtpUri();

    private void BuildOtpUri() {
        try {
            var secretBytes = OtpAuthSecret.FromBase32(OtpSecretBox.Text ?? string.Empty);
            var uri = OtpAuthTotp.Create(OtpIssuerBox.Text ?? string.Empty, OtpAccountBox.Text ?? string.Empty, secretBytes);
            OtpUriBox.Text = uri;
        } catch (Exception ex) {
            OtpUriBox.Text = string.Empty;
            MessageBox.Show(this, ex.Message, "OTP build failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveQrSvg_Click(object sender, RoutedEventArgs e) => SaveTextFile("Save QR SVG", "SVG|*.svg", () => {
        var qr = QrCodeEncoder.EncodeText(QrTextBox.Text ?? string.Empty, SelectedEcc, 1, 40);
        return SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { ModuleSize = 8, QuietZone = 4 });
    });

    private void SaveQrHtml_Click(object sender, RoutedEventArgs e) => SaveTextFile("Save QR HTML", "HTML|*.html", () => {
        var qr = QrCodeEncoder.EncodeText(QrTextBox.Text ?? string.Empty, SelectedEcc, 1, 40);
        return HtmlQrRenderer.Render(qr.Modules, new QrHtmlRenderOptions { ModuleSize = 8, QuietZone = 4, EmailSafeTable = false });
    });

    private void SaveQrPng_Click(object sender, RoutedEventArgs e) => SaveBinaryFile("Save QR PNG", "PNG|*.png", () => {
        var qr = QrCodeEncoder.EncodeText(QrTextBox.Text ?? string.Empty, SelectedEcc, 1, 40);
        return QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });
    });

    private void SaveBarcodeSvg_Click(object sender, RoutedEventArgs e) => SaveTextFile("Save Code 128 SVG", "SVG|*.svg", () => {
        var b = BarcodeEncoder.Encode(BarcodeType.Code128, BarcodeValueBox.Text ?? string.Empty);
        return SvgBarcodeRenderer.Render(b, new BarcodeSvgRenderOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 40 });
    });

    private void SaveBarcodeHtml_Click(object sender, RoutedEventArgs e) => SaveTextFile("Save Code 128 HTML", "HTML|*.html", () => {
        var b = BarcodeEncoder.Encode(BarcodeType.Code128, BarcodeValueBox.Text ?? string.Empty);
        return HtmlBarcodeRenderer.Render(b, new BarcodeHtmlRenderOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 40, EmailSafeTable = false });
    });

    private void SaveBarcodePng_Click(object sender, RoutedEventArgs e) => SaveBinaryFile("Save Code 128 PNG", "PNG|*.png", () => {
        var b = BarcodeEncoder.Encode(BarcodeType.Code128, BarcodeValueBox.Text ?? string.Empty);
        return BarcodePngRenderer.Render(b, new BarcodePngRenderOptions { ModuleSize = 2, QuietZone = 10, HeightModules = 40 });
    });

    private void SaveOtpQrPng_Click(object sender, RoutedEventArgs e) => SaveBinaryFile("Save OTP QR PNG", "PNG|*.png", () => {
        BuildOtpUri();
        var qr = QrCodeEncoder.EncodeText(OtpUriBox.Text ?? string.Empty, QrErrorCorrectionLevel.M, 1, 40);
        return QrPngRenderer.Render(qr.Modules, new QrPngRenderOptions { ModuleSize = 8, QuietZone = 4 });
    });

    private void SaveTextFile(string title, string filter, Func<string> produce) {
        try {
            var dlg = new SaveFileDialog { Title = title, Filter = filter };
            if (dlg.ShowDialog(this) != true) return;
            File.WriteAllText(dlg.FileName, produce());
        } catch (Exception ex) {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveBinaryFile(string title, string filter, Func<byte[]> produce) {
        try {
            var dlg = new SaveFileDialog { Title = title, Filter = filter };
            if (dlg.ShowDialog(this) != true) return;
            File.WriteAllBytes(dlg.FileName, produce());
        } catch (Exception ex) {
            MessageBox.Show(this, ex.Message, "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
