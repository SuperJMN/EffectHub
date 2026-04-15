using Avalonia.Controls;
using Avalonia.Media;
using EffectHub.Controls.EffectPreview;
using EffectHub.Core.Models;

namespace EffectHub.Sections.Editor;

public partial class EditorView : UserControl
{
    public EditorView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.UniformValueChanged += OnUniformValueChanged;
            }
        };
    }

    private void OnUniformValueChanged(UniformPropertyItem item)
    {
        if (PreviewSurface?.Effect is not DynamicShaderEffect effect) return;
        if (DataContext is not EditorViewModel vm) return;

        var items = vm.UniformPropertyItems.OfType<UniformPropertyItem>().ToList();

        int floatSlot = 0, colorSlot = 0, boolSlot = 0, intSlot = 0;

        foreach (var uniform in items)
        {
            switch (uniform.UniformType)
            {
                case UniformType.Float:
                    var floatVal = Convert.ToSingle(uniform.Value ?? 0.5);
                    switch (floatSlot++)
                    {
                        case 0: effect.Float0 = floatVal; break;
                        case 1: effect.Float1 = floatVal; break;
                        case 2: effect.Float2 = floatVal; break;
                        case 3: effect.Float3 = floatVal; break;
                        case 4: effect.Float4 = floatVal; break;
                        case 5: effect.Float5 = floatVal; break;
                        case 6: effect.Float6 = floatVal; break;
                        case 7: effect.Float7 = floatVal; break;
                    }
                    break;

                case UniformType.Color:
                    var colorVal = uniform.Value is Color c ? c : Colors.White;
                    switch (colorSlot++)
                    {
                        case 0: effect.Color0 = colorVal; break;
                        case 1: effect.Color1 = colorVal; break;
                    }
                    break;

                case UniformType.Bool:
                    var boolVal = uniform.Value is true;
                    switch (boolSlot++)
                    {
                        case 0: effect.Bool0 = boolVal; break;
                        case 1: effect.Bool1 = boolVal; break;
                    }
                    break;

                case UniformType.Int:
                    var intVal = Convert.ToInt32(uniform.Value ?? 0);
                    switch (intSlot++)
                    {
                        case 0: effect.Int0 = intVal; break;
                        case 1: effect.Int1 = intVal; break;
                    }
                    break;

                case UniformType.Float2:
                case UniformType.Float3:
                case UniformType.Float4:
                    // Multi-component floats consume float slots
                    var compFloat = Convert.ToSingle(uniform.Value ?? 0.0);
                    var components = uniform.UniformType switch
                    {
                        UniformType.Float2 => 2,
                        UniformType.Float3 => 3,
                        UniformType.Float4 => 4,
                        _ => 1
                    };
                    for (var i = 0; i < components && floatSlot < DynamicShaderEffect.MaxFloatUniforms; i++)
                    {
                        SetFloatSlot(effect, floatSlot++, compFloat);
                    }
                    break;
            }
        }
    }

    private static void SetFloatSlot(DynamicShaderEffect effect, int slot, float value)
    {
        switch (slot)
        {
            case 0: effect.Float0 = value; break;
            case 1: effect.Float1 = value; break;
            case 2: effect.Float2 = value; break;
            case 3: effect.Float3 = value; break;
            case 4: effect.Float4 = value; break;
            case 5: effect.Float5 = value; break;
            case 6: effect.Float6 = value; break;
            case 7: effect.Float7 = value; break;
        }
    }
}
