using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixForce.Services
{
    public interface IMessageService
    {
        void ShowMessage(string message, string title = "提示");
    }
}
