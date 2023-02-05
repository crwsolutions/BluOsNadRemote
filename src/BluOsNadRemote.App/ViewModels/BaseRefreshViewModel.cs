using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluOsNadRemote.App.ViewModels
{
    public partial class BaseRefreshViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool _isBusy = false;
    }
}
