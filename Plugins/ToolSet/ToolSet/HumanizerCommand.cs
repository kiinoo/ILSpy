using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using System.Diagnostics;
using System.IO;
using ICSharpCode.TreeView;

namespace ToolSet
{
   [ExportToolbarCommandAttribute(ToolTip = "Generate Humanize method", ToolbarIcon = ToolConstants.ImageEye, ToolbarCategory = "View", ToolbarOrder = 2)]
   public class HumanizerCommand : SimpleCommand
   {
      StreamWriter _writer = null;

      public override void Execute(object parameter)
      {
         HumanizerContext.Initialize();
         HumanizerPane.Instance.Show();
      }


      public void WriteLine(string text)
      {
         if (_writer != null)
            _writer.WriteLine(text);
      }
   }
   
}
