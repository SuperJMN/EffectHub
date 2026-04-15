using EffectHub.Sections.Editor;
using EffectHub.Sections.Gallery;
using EffectHub.Sections.MyEffects;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace EffectHub;

public partial class MainViewModel : ReactiveObject
{
    [Reactive] private int selectedIndex;

    public GalleryViewModel Gallery { get; }
    public EditorViewModel Editor { get; }
    public MyEffectsViewModel MyEffects { get; }

    public MainViewModel(GalleryViewModel gallery, EditorViewModel editor, MyEffectsViewModel myEffects)
    {
        Gallery = gallery;
        Editor = editor;
        MyEffects = myEffects;

        Gallery.EditEffectCallback = effect => NavigateToEditor(effect);
    }

    public void NavigateToEditor(Core.Models.Effect? effectToEdit = null)
    {
        if (effectToEdit is not null)
        {
            Editor.LoadEffect(effectToEdit);
        }

        SelectedIndex = 1;
    }
}
