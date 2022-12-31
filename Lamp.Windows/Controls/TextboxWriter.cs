using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

namespace Lamp
{
    internal class TextBoxWriter : TextWriter
    {

        private TextBox _textbox;
        private Microsoft.UI.Dispatching.DispatcherQueue _uiDispatcherQueue;
        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
        public TextBoxWriter(TextBox textbox, Microsoft.UI.Dispatching.DispatcherQueue uiDispatcherQueue)
        {
            _textbox = textbox;
            _uiDispatcherQueue = uiDispatcherQueue;
        }

        public override void Write(char value)
        {
            _uiDispatcherQueue?.TryEnqueue(() =>
            {
                _textbox.Text += value;
            });
        }

        public override void Write(string value)
        {
            _uiDispatcherQueue?.TryEnqueue(() => { 
                _textbox.Text += value;
            });
        }


    }
}
