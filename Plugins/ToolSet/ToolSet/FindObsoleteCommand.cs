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
   [ExportMainMenuCommandAttribute(Menu = "_Tools", Header = "Find Obsolete", MenuCategory = "View")]
   public class FindObsoleteCommand : SimpleCommand
   {
      StreamWriter _writer = null;

      public override void Execute(object parameter)
      {
         AssemblyTreeNode assemblyTreeNode = ToolConstants.GetAssemblyTreeNode();
         var types = assemblyTreeNode.LoadedAssembly.ModuleDefinition.Types;
         var fileName = "";
         var ofd = new Microsoft.Win32.SaveFileDialog();
         ofd.Title = "Save to";
         var result = ofd.ShowDialog();
         if (result == true)
         {
            fileName = ofd.FileName;
            _writer = new StreamWriter(fileName);

            //foreach (dynamic treeNode in assemblyTreeNode.Children)
            //{
            //   bool isPublicApi = false;
            //   try
            //   {
            //      isPublicApi = treeNode.IsPublicApi;
            //   }
            //   catch (Exception ex)
            //   {
            //      WriteLine(ex.ToString());
            //   }
            //   if (isPublicApi)
            //   {
            //      try
            //      {
            //         Mono.Cecil.MemberReference member = treeNode.Member;
            //         WriteLine(member.GetType().Name + "," + member.FullName);
            //      }
            //      catch (Exception ex)
            //      {
            //         WriteLine(ex.ToString());
            //      }
            //   }
            //}
            foreach (var type in types)
            {
               ScanType(type);
            }
            _writer.Close();
         }

         if (assemblyTreeNode != null)
         {
            string folder = "";
            var program = "explorer.exe";
            var args = "/select,\"" + fileName + "\"";
            try
            {
               Process.Start(program, args);
            }
            catch (System.Exception ex)
            {
               System.Windows.MessageBox.Show(program + " " + args, "Failed to open!");
            }
         }
      }


      //private void ScanType(IMemberTreeNode treeNode)
      //{
      //   PropertyTreeNode node = treeNode as PropertyTreeNode;
      //   if (node != null)
      //   {
      //      node.IsPublicAPI
      //   }

      //}

      public void WriteLine(string text)
      {
         if (_writer != null)
            _writer.WriteLine(text);
      }
      private void ScanType(Mono.Cecil.TypeDefinition type)
      {
         if (!MainWindow.Instance.SessionSettings.FilterSettings.ShowInternalApi)
         {
            if (!type.IsPublic)
               return;
         }
         //if (type.Name == "Document")
         //{
         //   Trace.WriteLine(type.FullName);
         //}
         string msg = "";
         if (IsObsolete(type, ref msg))
         {
            WriteLine("Type," + type.FullName + ",\"" + csvlize(msg) + "\"");
         }
         else
         {
            foreach (var member in type.Properties)
            {
               if (IsObsolete(member, ref msg))
               {
                  WriteLine("Property," + type.FullName + ",\"" + member + "\",\"" + csvlize(msg) + "\"");
               }
            }
            foreach (var member in type.Methods)
            {
               if (IsObsolete(member, ref msg))
               {
                  WriteLine("Method," + type.FullName + ",\"" + member + "\",\"" + csvlize(msg) + "\"");
               }
            }
            foreach (var member in type.Fields)
            {
               if (IsObsolete(member, ref msg))
               {
                  WriteLine("Field," + type.FullName + ",\"" + member + "\",\"" + csvlize(msg) + "\"");
               }
            }

            foreach (var member in type.Events)
            {
               if (IsObsolete(member, ref msg))
               {
                  WriteLine("Event," + type.FullName + ",\"" + member + "\",\"" + csvlize(msg) + "\"");
               }
            }
            foreach (var nestedType in type.NestedTypes)
            {
               ScanType(nestedType);
            }
         }
      }

      private string csvlize(string msg)
      {
         return msg.Replace("\"", "\"\"");
      }

      private bool IsObsolete(Mono.Cecil.IMemberDefinition member, ref string obsoleteDocument)
      {
         foreach (var item in member.CustomAttributes)
         {
            if (item.AttributeType.FullName == "System.ObsoleteAttribute")
            {
               try
               {
                  if (item.HasConstructorArguments)
                     obsoleteDocument = item.ConstructorArguments[0].Value.ToString();
                  else
                     obsoleteDocument = "(empty)";
               }
               catch (System.Exception ex)
               {
                  obsoleteDocument = ex.ToString();
               }
               return true;
            }
         }
         return false;
      }
   }
}
