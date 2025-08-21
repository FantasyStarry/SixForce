using System.Windows;

namespace SixForce.Services
{
    public class MessageService : IMessageService
    {
        public void ShowMessage(string message, string title = "提示")
        {
            MessageBox.Show(message, title);
        }
    }
}
