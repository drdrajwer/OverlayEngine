using System.Collections.ObjectModel;
using System.Reactive.Linq;
using OverlayEngine.Core.Models;
using OverlayEngine.Core.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OverlayEngine.UI.ViewModels.Widgets;

public sealed class NotesWidgetViewModel : ViewModelBase
{
    [Reactive] public bool   IsExpanded  { get; set; } = false;
    [Reactive] public string NewNoteText { get; set; } = string.Empty;
    [Reactive] public int    NoteCount   { get; private set; }

    public ObservableCollection<NoteItem> Notes { get; }

    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AddNoteCommand       { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleExpandCommand  { get; }
    public ReactiveCommand<NoteItem,             System.Reactive.Unit> RemoveNoteCommand    { get; }
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ShowSettingsCommand  { get; }

    private readonly ISettingsService _settings;

    public NotesWidgetViewModel(ISettingsService settings)
    {
        _settings = settings;
        Notes = new ObservableCollection<NoteItem>(settings.Current.Notes);

        NoteCount = Notes.Count;
        Notes.CollectionChanged += async (_, _) =>
        {
            NoteCount = Notes.Count;
            await PersistAsync();
        };

        var canAdd = this.WhenAnyValue(x => x.NewNoteText,
            text => !string.IsNullOrWhiteSpace(text));

        AddNoteCommand      = ReactiveCommand.CreateFromTask(AddNoteAsync, canAdd);
        ToggleExpandCommand = ReactiveCommand.Create(() => { IsExpanded = !IsExpanded; });
        RemoveNoteCommand   = ReactiveCommand.CreateFromTask<NoteItem>(RemoveNoteAsync);
        ShowSettingsCommand = ReactiveCommand.Create(() => { });
    }

    private async Task AddNoteAsync()
    {
        Notes.Add(new NoteItem { Text = NewNoteText });
        NewNoteText = string.Empty;
        await PersistAsync();
    }

    private async Task RemoveNoteAsync(NoteItem note)
    {
        Notes.Remove(note);
        await PersistAsync();
    }

    private async Task PersistAsync()
    {
        _settings.Current.Notes = [.. Notes];
        await _settings.SaveAsync();
    }
}
