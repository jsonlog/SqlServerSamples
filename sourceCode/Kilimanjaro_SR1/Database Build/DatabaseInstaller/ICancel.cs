using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlServer.DatabaseInstaller
{
    public interface ICancel
    {
        bool IsCancelRequested { get; }
    }
}
