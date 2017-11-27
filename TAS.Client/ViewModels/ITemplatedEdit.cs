using System;
using System.Windows.Input;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public interface ITemplatedEdit: ITemplated
    {
        object SelectedField { get; set; }
        bool IsDisplayCgMethod { get; }
        bool IsKeyReadOnly { get; }
        ICommand CommandEditField { get; }
        ICommand CommandAddField { get; }
        ICommand CommandDeleteField { get; }
        Array Methods { get; }
    }
}
