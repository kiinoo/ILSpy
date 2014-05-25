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
   // Menu: menu into which the item is added
   // MenuIcon: optional, icon to use for the menu item. Must be embedded as "Resource" (WPF-style resource) in the same assembly as the command type.
   // Header: text on the menu item
   // MenuCategory: optional, used for grouping related menu items together. A separator is added between different groups.
   // MenuOrder: controls the order in which the items appear (items are sorted by this value)
   [ExportMainMenuCommandAttribute(Menu = "_View", MenuIcon = ToolSet.ToolConstants.ImageFolderOpen, Header = "_Open Location", MenuCategory = "View", MenuOrder = 1.5)]
   // ToolTip: the tool tip
   // ToolbarIcon: The icon. Must be embedded as "Resource" (WPF-style resource) in the same assembly as the command type.
   // ToolbarCategory: optional, used for grouping related toolbar items together. A separator is added between different groups.
   // ToolbarOrder: controls the order in which the items appear (items are sorted by this value)
   [ExportToolbarCommandAttribute(ToolTip = "Open folder of selected file assembly", ToolbarIcon = ToolSet.ToolConstants.ImageFolderOpen, ToolbarCategory = "View", ToolbarOrder = 1.5)]
   //[ExportContextMenuEntryAttribute]
   public class OpenFolderCommand : SimpleCommand
   {
      public override void Execute(object parameter)
      {
         var selection = MainWindow.Instance.SelectedNodes;
         if (selection != null && selection.Count() == 0)
            return;
         SharpTreeNode treeNode = selection.First();
         var assemblyTreeNode = treeNode as AssemblyTreeNode;

         while (assemblyTreeNode == null)
         {
            treeNode = treeNode.Parent;
            assemblyTreeNode = treeNode as AssemblyTreeNode;
         }
         if (assemblyTreeNode != null)
         {
            string folder = "";
            var program = "explorer.exe";
            var args = "/select,\"" + assemblyTreeNode.LoadedAssembly.FileName + "\"";
            try
            {
               Process.Start(program, args);
            }
            catch (System.Exception ex)
            {
               System.Windows.MessageBox.Show(program + " " + args, "Failed to open folder!");
            }
         }
      }
   }

   [ExportContextMenuEntryAttribute(Header = "Open Location", Icon = ToolSet.ToolConstants.ImageFolderOpen, Category = "View")]
   public class OpenFolderContextMenu : SimpleContextMenuEntry
   {
      public OpenFolderContextMenu()
      {
         this.Command = new OpenFolderCommand();
      }
      public override bool IsVisible(TextViewContext context)
      {
         return context.SelectedTreeNodes != null /*&& context.SelectedTreeNodes.All(n => n is AssemblyTreeNode)*/;
      }
   }
}
