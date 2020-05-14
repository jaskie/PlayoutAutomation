namespace TAS.Client.Common
{
    public interface IWindowManager
    {
        void ShowWindow(ViewModelBase content, WindowInfo windowInfo = null);
        bool? ShowDialog(ViewModelBase content, WindowInfo windowInfo = null);
        void ShowWindow(ViewModelBase content, string title);
        bool? ShowDialog(ViewModelBase content, string title);
        void CloseWindow(ViewModelBase content);
    }
}
