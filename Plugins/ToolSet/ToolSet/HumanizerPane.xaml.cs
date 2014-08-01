using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.ILSpy;
using System.ComponentModel.Composition;
using ICSharpCode.Decompiler;
using Mono.Cecil;
using System.Threading;
using System.ComponentModel.Composition.Hosting;
using System.Collections.ObjectModel;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ToolSet
{
   /// <summary>
   /// Interaction logic for HumanizerPane.xaml
   /// </summary>
   public partial class HumanizerPane : UserControl, IPane
   {
      //HumanizerDisassembler _humDisassembler;
      public HumanizerPane()
      {
         InitializeComponent();
         MainWindow.Instance.TreeView.SelectionChanged += TreeView_SelectionChanged;
         ToolSetSettings.Instance.PropertyChanged += ToolSetSettings_PropertyChanged;
         CancellationToken token = new CancellationTokenSource().Token;
         TreeView_SelectionChanged(null, null);
      }

      void ToolSetSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "Language")
         {
            TreeView_SelectionChanged(sender, null);
         }
      }

      void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         this.textEditor.Clear();
         AvalonEditTextOutput textOutput = new AvalonEditTextOutput();
         //_humDisassembler = new HumanizerDisassembler(textOutput, token);
         foreach (var item in MainWindow.Instance.SelectedNodes)
         {
            var typeTreeNode = item as TypeTreeNode;
            if (typeTreeNode != null)
            {
               ToolSetSettings.Instance.Language.DecompileType(typeTreeNode.TypeDefinition, textOutput, new DecompilationOptions());
               //_humDisassembler.DisassembleType(typeTreeNode.TypeDefinition);
            }
         }
         this.textEditor.SyntaxHighlighting = ToolSetSettings.Instance.Language.SyntaxHighlighting;
         this.textEditor.AppendText(textOutput.GetDocument().Text);
         textEditor.TextArea.DefaultInputHandler.NestedInputHandlers.Add(new ICSharpCode.AvalonEdit.Search.SearchInputHandler(textEditor.TextArea));
      }

      static HumanizerPane instance;

      public static HumanizerPane Instance
      {
         get
         {
            if (instance == null)
            {
               instance = new HumanizerPane();
            }
            return instance;
         }
      }

      //public ToolSetSettings Settings
      //{
      //   get { return ToolSetSettings.Instance; }
      //}

      public void Show()
      {
         if (!IsVisible)
         {
            MainWindow.Instance.ShowInTopPane("Generate Humanize method", this);
         }
         //Dispatcher.BeginInvoke(
         //   DispatcherPriority.Background,
         //   new Func<bool>(searchBox.Focus));
      }

      public void Closed()
      {
      }
   }
}
