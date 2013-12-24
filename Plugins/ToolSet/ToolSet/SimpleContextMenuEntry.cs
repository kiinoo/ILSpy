// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using Mono.Cecil;
using Microsoft.Win32;
using System.Windows.Input;

namespace ToolSet
{

   public class SimpleContextMenuEntry : IContextMenuEntry
   {
      ICommand m_command = null;

      public ICommand Command
      {
         get { return m_command; }
         set { m_command = value; }
      }

      public virtual void Execute(TextViewContext context)
      {
         m_command.Execute(context);
      }

      public virtual bool IsEnabled(TextViewContext context)
      {
         return m_command.CanExecute(context);
      }

      public virtual bool IsVisible(TextViewContext context)
      {
         throw new NotImplementedException();
      }
   }


}
