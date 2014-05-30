using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy;
using ICSharpCode.TreeView;

namespace ToolSet
{
   public static class ToolConstants
   {
      public const string ImageFolderOpen = "Images\\Folder_6221.png";
      public const string ImageEye = "Images\\eye_16xLG.png";
      public const string ImageSettingsOpen = "Images\\settings_16.png";
      public static AssemblyTreeNode GetAssemblyTreeNode()
      {
         var selection = MainWindow.Instance.SelectedNodes;
         if (selection != null && selection.Count() == 0)
            return null;
         SharpTreeNode treeNode = selection.First();
         var assemblyTreeNode = treeNode as AssemblyTreeNode;

         while (assemblyTreeNode == null)
         {
            treeNode = treeNode.Parent;
            assemblyTreeNode = treeNode as AssemblyTreeNode;
         }
         return assemblyTreeNode;
      }
   }
}
