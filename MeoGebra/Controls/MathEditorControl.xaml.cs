using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace MeoGebra.Controls;

public partial class MathEditorControl : UserControl {
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(MathEditorControl),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty PreviewTextProperty = DependencyProperty.Register(
        nameof(PreviewText),
        typeof(string),
        typeof(MathEditorControl),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private bool _isUpdatingFromEditor;

    public MathEditorControl() {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public string Text {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string PreviewText {
        get => (string)GetValue(PreviewTextProperty);
        set => SetValue(PreviewTextProperty, value);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) {
        if (EditorView.CoreWebView2 != null) {
            return;
        }

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MeoGebra",
            "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await EditorView.EnsureCoreWebView2Async(environment);

        if (EditorView.CoreWebView2 == null) {
            return;
        }

        EditorView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        EditorView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        EditorView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        EditorView.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
        EditorView.CoreWebView2.NavigationStarting += (_, args) => {
            if (args.Uri is null || !args.Uri.StartsWith("https://meogebra-assets", StringComparison.OrdinalIgnoreCase)) {
                args.Cancel = true;
            }
        };
        EditorView.CoreWebView2.NavigationCompleted += async (_, _) => await SetEditorTextAsync(Text);
        EditorView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MathEditor");
        EditorView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "meogebra-assets",
            assetsPath,
            CoreWebView2HostResourceAccessKind.Allow);
        EditorView.Source = new Uri("https://meogebra-assets/index.html");
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e) {
        if (EditorView.CoreWebView2 != null) {
            EditorView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e) {
        try {
            var payload = JsonSerializer.Deserialize<EditorMessage>(e.WebMessageAsJson);
            if (payload is null) {
                return;
            }

            _isUpdatingFromEditor = true;
            if (payload.Type == "preview") {
                PreviewText = NormalizeExpression(payload.Text ?? string.Empty);
            } else if (payload.Type == "commit") {
                Text = NormalizeExpression(payload.Text ?? string.Empty);
            }
        } finally {
            _isUpdatingFromEditor = false;
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is MathEditorControl control && !control._isUpdatingFromEditor) {
            _ = control.SetEditorTextAsync((string?)e.NewValue ?? string.Empty);
        }
    }

    private async Task SetEditorTextAsync(string text) {
        if (EditorView.CoreWebView2 == null) {
            return;
        }
        var escaped = JsonSerializer.Serialize(text ?? string.Empty);
        await EditorView.ExecuteScriptAsync($"window.editor && window.editor.setText({escaped});");
    }

    private static string NormalizeExpression(string input) {
        if (string.IsNullOrWhiteSpace(input)) {
            return string.Empty;
        }

        var normalized = input
            .Replace("×", "*")
            .Replace("÷", "/")
            .Replace("−", "-")
            .Replace("π", "pi")
            .Replace("×", "*");

        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(\d)([a-zA-Z(])", "$1*$2");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"([a-zA-Z)])(\d)", "$1*$2");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"([a-zA-Z)])([a-zA-Z(])", "$1*$2");

        return normalized;
    }

    private sealed record EditorMessage(string Type, string? Text);
}