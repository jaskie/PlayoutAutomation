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
        public OkCancelViewModel(IOkCancelViewModel contentViewModel)
        {
            OkCancelVM = contentViewModel;
            CommandCancel = new UiCommand(Cancel, OkCancelVM.CanCancel);
            CommandOk = new UiCommand (Ok, OkCancelVM.CanOk);                     
        }

        private void Ok(object obj)
        {
            if (!OkCancelVM.Ok(obj))
                return;

            DialogResult = true;
            UiServices.WindowManager.CloseWindow(this);
        }

        private void Cancel(object obj)
        {
            OkCancelVM.Cancel(obj);
            DialogResult = false;
            UiServices.WindowManager.CloseWindow(this);
        }

        protected override void OnDispose()
        {
            //
        }

        public ICommand CommandCancel { get; protected set; }
        public ICommand CommandOk { get; protected set; }        
    }
}
