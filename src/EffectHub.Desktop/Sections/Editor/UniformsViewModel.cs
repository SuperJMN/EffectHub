using Avalonia.Media;
using EffectHub.Core.Models;
using ReactiveUI;
using Zafiro.Avalonia.Controls.PropertyGrid.ViewModels;

namespace EffectHub.Sections.Editor;

/// <summary>
/// Bridges detected shader uniforms to IPropertyItem instances that the
/// Zafiro PropertyGrid can display and edit directly.
/// </summary>
public class UniformsViewModel : ReactiveObject
{
    private readonly List<UniformPropertyItem> items = [];

    public UniformsViewModel(IReadOnlyList<UniformDefinition> uniforms, Action<UniformPropertyItem> onValueChanged)
    {
        foreach (var uniform in uniforms)
        {
            var item = new UniformPropertyItem(uniform, onValueChanged);
            items.Add(item);
        }
    }

    public IReadOnlyList<UniformPropertyItem> Items => items;

    public object? GetValue(string name) => items.FirstOrDefault(i => i.Name == name)?.Value;
}

/// <summary>
/// An IPropertyItem backed by a UniformDefinition. The PropertyGrid's
/// PropertyEditorSelector picks the correct editor (Slider, ColorPicker, CheckBox, etc.)
/// based on PropertyType.
/// </summary>
public class UniformPropertyItem : ReactiveObject, IPropertyItem, IDisposable
{
    private readonly Action<UniformPropertyItem> onChanged;
    private object? value;

    public UniformPropertyItem(UniformDefinition definition, Action<UniformPropertyItem> onChanged)
    {
        this.onChanged = onChanged;
        Name = definition.Name;
        UniformType = definition.Type;
        PropertyType = MapToClrType(definition.Type);
        value = GetDefaultValue(definition);
    }

    public string Name { get; }
    public Type PropertyType { get; }
    public UniformType UniformType { get; }

    public object? Value
    {
        get => value;
        set
        {
            this.RaiseAndSetIfChanged(ref this.value, value);
            onChanged(this);
        }
    }

    public void Dispose() { }

    private static Type MapToClrType(UniformType type) => type switch
    {
        UniformType.Float => typeof(double),
        UniformType.Float2 => typeof(double),
        UniformType.Float3 => typeof(double),
        UniformType.Float4 => typeof(double),
        UniformType.Color => typeof(Color),
        UniformType.Bool => typeof(bool),
        UniformType.Int => typeof(int),
        _ => typeof(double)
    };

    private static object? GetDefaultValue(UniformDefinition def) => def.Type switch
    {
        UniformType.Float => def.DefaultValue,
        UniformType.Color => Colors.White,
        UniformType.Bool => false,
        UniformType.Int => (int)def.DefaultValue,
        _ => def.DefaultValue
    };
}

