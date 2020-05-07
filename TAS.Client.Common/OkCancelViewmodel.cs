using System;
using System.Windows.Input;

namespace TAS.Client.Common
{
    public class OkCancelViewModel : ViewModelBase
    {
        private IOkCancelViewModel _okCancelViewModel;

        public bool DialogResult { get; private set; }
        public bool OkCancelButtonsActivateViaKeyboard { get; set; } = true;
        public IOkCancelViewModel OkCancelVM { get => _okCancelViewModel; private set => SetField(ref _okCancelViewModel, value); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentViewModel">ViewModel that will be main content of a window</param>
        /// <param name="okButtonContent">Ok button content</param>
        /// <param name="cancelButtonContent">Cancel button content</param>
        public OkCancelViewModel(IOkCancelViewModel contentViewModel, string okButtonContent = "Ok", string cancelButtonContent = "Cancel")
        {
            OkCancelVM = contentViewModel;
            CommandCancel = new UiCommand(Cancel, OkCancelVM.CanCancel);
            CommandOk = new UiCommand (Ok, OkCancelVM.CanOk);


            OkButtonContent = okButtonContent;
            //NotifyPropertyChanged(nameof(OkButtonContent));

            CancelButtonContent = cancelButtonContent;
            //NotifyPropertyChanged(nameof(CancelButtonContent));
        }

        private void Ok(object obj)
        {
            if (!OkCancelVM.Ok(obj))
                return;

            DialogResult = true;
            Closing?.Invoke(this, EventArgs.Empty);
            UiServices.WindowManager.CloseWindow(this);
        }

        private void Cancel(object obj)
        {
            OkCancelVM.Cancel(obj);
            DialogResult = false;
            Closing?.Invoke(this, EventArgs.Empty);
            UiServices.WindowManager.CloseWindow(this);
        }

        protected override void OnDispose()
        {
            //
        }

        public event EventHandler Closing;

        public ICommand CommandCancel { get; protected set; }
        public ICommand CommandOk { get; protected set; }

        public string OkButtonContent { get; }
        public string CancelButtonContent { get; }
    }
}
