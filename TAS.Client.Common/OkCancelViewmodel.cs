using System;
using System.Windows.Input;
using System.Windows.Navigation;

namespace TAS.Client.Common
{
    public abstract class OkCancelViewModel : DialogViewModel
    {
        private OkCancelViewModel _okCancelViewModel;        
        public bool OkCancelButtonsActivateViaKeyboard { get; set; } = true;
        public OkCancelViewModel OkCancelVM { get => _okCancelViewModel; private set => SetField(ref _okCancelViewModel, value); }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentViewModel">ViewModel that will be main content of a window</param>
        /// <param name="okButtonContent">Ok button content</param>
        /// <param name="cancelButtonContent">Cancel button content</param>
        public OkCancelViewModel(string okButtonContent = "Ok", string cancelButtonContent = "Cancel")
        {
            OkCancelVM = this;
            CommandCancel = new UiCommand(OkCancelVM.Cancel, OkCancelVM.CanCancel);
            CommandOk = new UiCommand((obj) => Ok(obj), OkCancelVM.CanOk);

            OkButtonContent = okButtonContent;            
            CancelButtonContent = cancelButtonContent;            
        }        

        protected virtual bool Ok(object obj)
        {
            DialogConfirm();
            return true;
        }
        protected abstract bool CanOk(object obj);
        protected virtual bool CanCancel(object obj)
        {
            return true;
        }

        protected virtual void Cancel(object obj)
        {            
            DialogClose();
        }            

        public ICommand CommandCancel { get; protected set; }
        public ICommand CommandOk { get; protected set; }

        public string OkButtonContent { get; }
        public string CancelButtonContent { get; }
    }
}
