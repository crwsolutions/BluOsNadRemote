namespace BluOsNadRemote.App.Controls;

public class ItemsListViewMultilineTemplateSelector : DataTemplateSelector
{
    public DataTemplate Singleline { get; set; }
    public DataTemplate Multiline { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return string.IsNullOrEmpty(((MusicContentEntryViewModel)item).Entry.Text2) ? Singleline : Multiline;
    }
}
