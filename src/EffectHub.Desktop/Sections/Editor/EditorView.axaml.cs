using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using EffectHub.Controls.CpuFallbackPreview;
using EffectHub.Controls.EffectPreview;
using EffectHub.Core.Models;
using ReactiveUI;
using SkiaSharp;

namespace EffectHub.Sections.Editor;

public partial class EditorView : UserControl
{
    private CpuFallbackPreviewControl? cpuPreviewControl;
    private DispatcherTimer? cpuAnimationTimer;

    public EditorView()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is EditorViewModel vm)
            {
                vm.UniformValueChanged += OnUniformValueChanged;
                vm.XamlContentParsed += OnXamlContentParsed;

                SetupCpuPreview(vm);

                vm.WhenAnyValue(x => x.IsGpuRunning)
                    .Subscribe(running =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (GpuPreviewSurface != null)
                                GpuPreviewSurface.IsVisible = running;
                        });
                    });

                vm.WhenAnyValue(x => x.ActiveCpuRenderer)
                    .Subscribe(renderer =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (cpuPreviewControl != null)
                                cpuPreviewControl.Renderer = renderer;
                        });
                    });

                vm.WhenAnyValue(x => x.IsCpuRunning)
                    .Subscribe(running =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (cpuPreviewControl != null)
                            {
                                cpuPreviewControl.IsRunning = running;
                                UpdateCpuAnimationTimer(running);
                            }
                        });
                    });

                OnXamlContentParsed(vm.XamlContent);
            }
        };
    }

    private void SetupCpuPreview(EditorViewModel vm)
    {
        cpuPreviewControl = new CpuFallbackPreviewControl
        {
            IsRunning = vm.IsCpuRunning,
            Renderer = vm.ActiveCpuRenderer,
        };

        CpuPreviewSurface.Child = cpuPreviewControl;
    }

    private void UpdateCpuAnimationTimer(bool running)
    {
        if (running)
        {
            cpuAnimationTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, (_, _) =>
            {
                cpuPreviewControl?.InvalidateVisual();
            });
            cpuAnimationTimer.Start();
        }
        else
        {
            cpuAnimationTimer?.Stop();
        }
    }

    private void OnXamlContentParsed(string xaml)
    {
        if (DataContext is not EditorViewModel vm)
            return;

        try
        {
            var control = AvaloniaRuntimeXamlLoader.Load(xaml) as Control;
            if (control != null)
            {
                // Set XAML content as child of both preview surfaces
                GpuPreviewSurface.Child = control;

                vm.XamlParseError = null;
            }
        }
        catch (Exception ex)
        {
            vm.XamlParseError = ex.Message;
        }
    }

    private void OnUniformValueChanged(UniformPropertyItem item)
    {
        // Push to GPU effect
        if (GpuPreviewSurface?.Effect is DynamicShaderEffect effect)
        {
            PushUniformsToGpuEffect(effect);
        }

        // Push to CPU preview
        if (cpuPreviewControl != null)
        {
            PushUniformsToCpuPreview();
        }
    }

    private void PushUniformsToGpuEffect(DynamicShaderEffect effect)
    {
        if (DataContext is not EditorViewModel vm) return;

        var items = vm.UniformPropertyItems.OfType<UniformPropertyItem>().ToList();

        int floatSlot = 0, colorSlot = 0, boolSlot = 0, intSlot = 0;

        foreach (var uniform in items)
        {
            switch (uniform.UniformType)
            {
                case UniformType.Float:
                    var floatVal = Convert.ToSingle(uniform.Value ?? 0.5);
                    SetFloatSlot(effect, floatSlot++, floatVal);
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

    private void PushUniformsToCpuPreview()
    {
        if (DataContext is not EditorViewModel vm || cpuPreviewControl is null) return;

        var items = vm.UniformPropertyItems.OfType<UniformPropertyItem>().ToList();

        var floats = new List<float>();
        var colors = new List<SKColor>();
        var bools = new List<bool>();
        var ints = new List<int>();

        foreach (var uniform in items)
        {
            switch (uniform.UniformType)
            {
                case UniformType.Float:
                case UniformType.Float2:
                case UniformType.Float3:
                case UniformType.Float4:
                    floats.Add(Convert.ToSingle(uniform.Value ?? 0.0));
                    break;
                case UniformType.Color:
                    var c = uniform.Value is Color avColor
                        ? new SKColor(avColor.R, avColor.G, avColor.B, avColor.A)
                        : SKColors.White;
                    colors.Add(c);
                    break;
                case UniformType.Bool:
                    bools.Add(uniform.Value is true);
                    break;
                case UniformType.Int:
                    ints.Add(Convert.ToInt32(uniform.Value ?? 0));
                    break;
            }
        }

        cpuPreviewControl.FloatUniforms = floats.ToArray();
        cpuPreviewControl.ColorUniforms = colors.ToArray();
        cpuPreviewControl.BoolUniforms = bools.ToArray();
        cpuPreviewControl.IntUniforms = ints.ToArray();
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
