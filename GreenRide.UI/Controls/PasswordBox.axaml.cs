using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using AvPath = Avalonia.Controls.Shapes.Path;

namespace GreenRide.UI.Controls;

/// <summary>
/// Input password dengan toggle mata. Dipakai di Login dan Sign Up.
/// Bind lewat <see cref="Text"/> (dua arah); Enter menjalankan <see cref="SubmitCommand"/>.
/// </summary>
public partial class PasswordBox : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<PasswordBox, string?>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<PasswordBox, string?>(nameof(Watermark));

    public static readonly StyledProperty<bool> IsInvalidProperty =
        AvaloniaProperty.Register<PasswordBox, bool>(nameof(IsInvalid));

    public static readonly StyledProperty<ICommand?> SubmitCommandProperty =
        AvaloniaProperty.Register<PasswordBox, ICommand?>(nameof(SubmitCommand));

    private readonly TextBox? _input;
    private readonly ToggleButton? _eye;
    private readonly AvPath? _iconHidden;
    private readonly AvPath? _iconShown;
    private bool _syncing;

    public PasswordBox()
    {
        InitializeComponent();

        _input = this.FindControl<TextBox>("Input");
        _eye = this.FindControl<ToggleButton>("Eye");
        _iconHidden = this.FindControl<AvPath>("IconHidden");
        _iconShown = this.FindControl<AvPath>("IconShown");

        if (_input is not null)
        {
            _input.TextChanged += (_, _) =>
            {
                if (!_syncing) Text = _input.Text ?? string.Empty;
            };
            _input.KeyDown += OnInputKeyDown;
        }

        if (_eye is not null)
            _eye.Click += (_, _) => SetRevealed(_eye.IsChecked == true);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public bool IsInvalid
    {
        get => GetValue(IsInvalidProperty);
        set => SetValue(IsInvalidProperty, value);
    }

    public ICommand? SubmitCommand
    {
        get => GetValue(SubmitCommandProperty);
        set => SetValue(SubmitCommandProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_input is null) return;

        if (change.Property == TextProperty)
        {
            var value = change.GetNewValue<string?>() ?? string.Empty;
            if (_input.Text != value)
            {
                _syncing = true;
                _input.Text = value;
                _syncing = false;
            }
        }
        else if (change.Property == WatermarkProperty)
        {
            _input.Watermark = change.GetNewValue<string?>();
        }
        else if (change.Property == IsInvalidProperty)
        {
            _input.Classes.Set("invalid", change.GetNewValue<bool>());
        }
    }

    private void SetRevealed(bool revealed)
    {
        if (_input is null) return;
        _input.RevealPassword = revealed;
        if (_iconShown is not null) _iconShown.IsVisible = revealed;
        if (_iconHidden is not null) _iconHidden.IsVisible = !revealed;
        _input.Focus();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var command = SubmitCommand;
        if (command is not null && command.CanExecute(null))
        {
            command.Execute(null);
            e.Handled = true;
        }
    }
}
