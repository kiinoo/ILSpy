using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using ICSharpCode.Decompiler;
using System.Threading;

namespace ToolSet
{
   public class HumanizerDisassembler
   {
      ITextOutput _output;
      CancellationToken _cancellationToken;
      bool isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
      //MethodBodyDisassembler methodBodyDisassembler;
      MemberReference currentMember;

      string[] includeMethodPrefixes = new[] { "Get", "Is", "Can", "Are", "As" };
      string[] excludeMethodPrefixes = new[] { "GetType", "GetHashCode", "GetEnumerator" };
      string[] excludePropPrefixes = new[] { "IsValidObject" };

      public ITextOutput Output { get { return _output; } set { _output = value; } }

      public HumanizerDisassembler(ITextOutput output, CancellationToken cancellationToken)
      {
         _output = output;
         _cancellationToken = cancellationToken;
      }

      public virtual void DisassembleType(TypeDefinition type)
      {
         if (type.IsEnum)
         {
            _output.WriteLine("// Enumeration is not supported");
            return;
         }
         _output.WriteLine("public string Humanize(" + type.FullName + " obj)");
         _output.WriteLine("{");
         _output.WriteLine("   if (obj == null)");
         _output.WriteLine("      return \"(null)\";");
         _output.WriteLine("   var prefix = Environment.NewLine + \"\\t\";");
         _output.WriteLine("   StringBuilder sb = new StringBuilder();");

         //if (type.HasNestedTypes)
         //{
         //   _output.WriteLine("// Nested Types");
         //   foreach (var nestedType in type.NestedTypes)
         //   {
         //      _cancellationToken.ThrowIfCancellationRequested();
         //      DisassembleType(nestedType);
         //      _output.WriteLine();
         //   }
         //   _output.WriteLine();
         //}
         if (type.HasFields)
         {
            //_output.WriteLine("// Fields");
            foreach (var field in type.Fields)
            {
               _cancellationToken.ThrowIfCancellationRequested();
               DisassembleField(field);
            }
         }
         if (type.HasMethods)
         {
            //_output.WriteLine("// Methods");
            foreach (var m in type.Methods)
            {
               _cancellationToken.ThrowIfCancellationRequested();
               DisassembleMethod(m);
            }
         }
         //if (type.HasEvents)
         //{
         //   _output.WriteLine("// Events");
         //   foreach (var ev in type.Events)
         //   {
         //      _cancellationToken.ThrowIfCancellationRequested();
         //      DisassembleEvent(ev);
         //      _output.WriteLine();
         //   }
         //   _output.WriteLine();
         //}
         if (type.HasProperties)
         {
            //_output.WriteLine("// Properties");
            foreach (var prop in type.Properties)
            {
               _cancellationToken.ThrowIfCancellationRequested();
               DisassembleProperty(prop);
            }
         }
         _output.WriteLine("   return sb.ToString();");
         _output.WriteLine("}");
      }

      public virtual void DisassembleEvent(EventDefinition ev)
      {

      }

      public virtual void DisassembleField(FieldDefinition field)
      {
         if (field.IsPublic)
            _output.WriteLine("   sb.AppendFormat(\"[" + field.Name + "] {0} \", obj." + field.Name + ");");
      }

      public virtual void DisassembleMethod(MethodDefinition method)
      {
         if (!method.IsPublic) return;
         if (method.IsConstructor) return;
         if (method.Name.StartsWith("get_")) return;
         //if (method.Parameters.Count > 0)
         //   return;
         foreach (var exclude in excludeMethodPrefixes)
         {
            if (method.Name.StartsWith(exclude))
               return;
         }
         foreach (var prefix in includeMethodPrefixes)
         {
            if (method.Name.StartsWith(prefix))
            {

               var methodCallStrings = new List<string>() { method.Name + "()" };
               var paLength = method.Parameters.Count;
               List<object[]> result = new List<object[]>();
               result.Add(new object[paLength]);
               if (paLength > 0)
               {
                  methodCallStrings.Clear();
                  var methodCallString = method.Name + "(";
                  //List<object[]> resultParaLists = new List<object[]>();
                  for (int ii = 0; ii < method.Parameters.Count; ii++)
                  {
                     var parameter = method.Parameters[ii];
                     List<object> parameterCandidates = new List<object>();
                     List<object[]> tmpResult = new List<object[]>();

                     switch (parameter.ParameterType.FullName)
                     {
                        //sb.AppendFormat("[GetEndPoint0] {0} ", prefix + Humanize(obj.GetEndPoint(0)));
                        //sb.AppendFormat("[GetEndPoint1] {0} ", prefix + Humanize(obj.GetEndPoint(1)));
                        case "System.Int16":
                        case "System.Int32":
                        case "System.Int64":
                        case "System.Double":
                           parameterCandidates.Add(0);
                           parameterCandidates.Add(1);
                           break;
                        case "System.String":
                           parameterCandidates.Add("SampleText");
                           break;
                        default:
                           parameterCandidates.Add(null);
                           break;
                     }
                     foreach (var para in parameterCandidates)
                     {
                        foreach (var item in result)
                        {
                           object[] clone = new object[paLength];
                           item.CopyTo(clone, 0);
                           clone[ii] = para;
                           tmpResult.Add(clone);
                        }
                     }
                     result = tmpResult;
                  }
               }
               foreach (var one in result)
               {
                  var methodCallString = method.Name + "(" + GetMethodCallString(one) + ")";
                  if (isBasicType(method.ReturnType))
                     _output.WriteLine("   sb.AppendFormat(\"[" + methodCallString + "] {0} \", obj." + methodCallString + ");");
                  else
                     _output.WriteLine("   sb.AppendFormat(\"[" + methodCallString + "] {0} \", prefix + Humanize(obj." + methodCallString + "));");
               }
               break;
            }
         }
      }

      private string GetMethodCallString(object[] paraArray)
      {
         string result = "";
         if (paraArray == null || paraArray.Length == 0)
            return result;
         object para = null;
         for (int ii = 0; ii < paraArray.Length; ii++)
         {
            var singleText = "";
            var paraOne = paraArray[ii];
            if (paraOne == null)
               singleText = "null";
            else if (paraOne.GetType() == typeof(String))
               singleText = "\"" + paraOne + "\"";
            else
               singleText = paraOne.ToString();
            if (ii != 0)
               result += ",";
            result += singleText;
         }
         return result;
      }

      public virtual bool isBasicType(TypeReference typeReference)
      {
         var type = typeReference.Resolve();
         if (type.FullName == "System.String")
            return true;
         return type.IsValueType;
      }

      public virtual void DisassembleProperty(PropertyDefinition prop)
      {
         foreach (var exclude in excludePropPrefixes)
         {
            if (prop.Name.StartsWith(exclude))
               return;
         }
         if (prop.GetMethod != null && prop.GetMethod.IsPublic)
            if (isBasicType(prop.PropertyType))
               _output.WriteLine("   sb.AppendFormat(\"[" + prop.Name + "] {0} \", obj." + prop.Name + ");");
            else
               _output.WriteLine("   sb.AppendFormat(\"[" + prop.Name + "] {0} \", prefix + Humanize(obj." + prop.Name + "));");
      }
   }

   public class HumanizeRecursiveOnMethodReturnTypeDisassembler : HumanizerDisassembler
   {
      Dictionary<string, TypeReference> referencedTypes = new Dictionary<string, TypeReference>();
      int level = 1;
      public HumanizeRecursiveOnMethodReturnTypeDisassembler(ITextOutput output, CancellationToken cancellationToken)
         : base(output, cancellationToken)
      {
      }
      public override void DisassembleType(TypeDefinition type)
      {
         //if (level < 0)
         //   return;
         if (referencedTypes.ContainsKey(type.FullName))
         {
            referencedTypes[type.FullName] = null; //marked as disassembled
         }

         base.DisassembleType(type);

         level--;
         foreach (var item in referencedTypes.Keys.ToArray())
         {
            if (referencedTypes[item] != null)
               DisassembleType(referencedTypes[item].Resolve());
         }
      }
      public override void DisassembleProperty(PropertyDefinition prop)
      {
         base.DisassembleProperty(prop);
         if (level < 1) return;
         if (!referencedTypes.ContainsKey(prop.PropertyType.FullName))
         {
            if (prop.PropertyType.Resolve() == prop.DeclaringType)
               return;
            if (isBasicType(prop.PropertyType))
               return;
            referencedTypes.Add(prop.PropertyType.FullName, prop.PropertyType);
         }
      }
   }
}
