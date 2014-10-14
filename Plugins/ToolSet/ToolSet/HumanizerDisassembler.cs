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
      //ICSharpCode.ILSpy.AssemblyList _list;
      bool isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
      //MethodBodyDisassembler methodBodyDisassembler;
      MemberReference currentMember;

      string[] includeMethodPrefixes = new[] { "Get", "Is", "Can", "Are", "As" };
      string[] excludeMethodPrefixes = new[] { "GetType", "GetHashCode", "GetEnumerator" };
      string[] excludePropPrefixes = new[] { "IsValidObject" };

      public ITextOutput Output { get { return _output; } set { _output = value; } }

      public HumanizerDisassembler(ITextOutput output, CancellationToken cancellationToken)
      {
         //_list = list;
         _output = output;
         _cancellationToken = cancellationToken;
      }

      static Dictionary<string, string> BuiltInHumanizers = new Dictionary<string, string>();
      static HumanizerDisassembler()
      {
         BuiltInHumanizers.Add(
"Autodesk.Revit.DB.Element",
@"public static string Humanize(this Element obj, int indent = 0, bool ignoreIndentation = false)
{
   if (obj == null)
      return ""(null)"";
   StringBuilder sb = new StringBuilder(ignoreIndentation ? """" : "" Type = "" + obj.GetType().FullName);
   var specialFlag = ignoreIndentation ? ""*"" : """";
   var prefix = Environment.NewLine + new string(' ', indent * 2) + ""["" + specialFlag;
   sb.AppendFormat(prefix + ""Id] {0} "", SafeInvoke(obj, oo => oo.Id), indent + 1);
   sb.AppendFormat(prefix + ""Name] {0} "", SafeInvoke(obj, oo => oo.Name), indent + 1);
   sb.AppendFormat(prefix + ""Category] {0} "", SafeInvoke(obj, oo => (BuiltInCategory)oo.Category.Id.IntegerValue), indent + 1);
   sb.AppendFormat(prefix + ""GetType().FullName] {0} "", SafeInvoke(obj, oo => oo.GetType().FullName), indent + 1);
   return sb.ToString();
}"
);

      }

      public virtual void DisassembleType(TypeDefinition type, bool outputSaveInvoke = true)
      {
         if (BuiltInHumanizers.ContainsKey(type.FullName))
         {
            _output.WriteLine(BuiltInHumanizers[type.FullName]);
            return;
         }
         if (type.IsEnum)
         {
            _output.WriteLine("// Enumeration is not supported");
            return;
         }

         string template = @"public static string Humanize(this TypeFullName obj, int indent = 0, bool ignoreIndentation = false)
{
   if (obj == null)
      return ""(null)"";
   StringBuilder sb = new StringBuilder(ignoreIndentation ? """" : "" Type = "" + obj.GetType().FullName);
   var specialFlag = ignoreIndentation ? ""*"" : """";
   var prefix = Environment.NewLine + new string(' ', indent * 2) + ""["" + specialFlag;";

         _output.WriteLine(template.Replace("TypeFullName", type.FullName));

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

         var derivedTypes = ICSharpCode.ILSpy.TreeNodes.Analyzer.Helpers.FindDerivedTypes(type, new ModuleDefinition[] { type.Module }, new System.Threading.CancellationToken());
         BeforeReturn(type, derivedTypes);
         _output.WriteLine("   return sb.ToString();");
         _output.WriteLine("}");
         AfterReturn(type, derivedTypes);
         if (outputSaveInvoke)
            _output.WriteLine(@"
///////////////////// -- Help methods -- //////////////////////
public static TReturn SafeInvoke<T, TReturn>(this T host, Func<T, TReturn> func)
{
   if (func == null) return default(TReturn);
   try
   {
      return func(host);
   }
   catch (System.Exception ex)
   {
      return default(TReturn);
   }
}
public static string Humanize<T, TReturn>(this T host, Func<T, TReturn> func)
{
   TReturn val = default(TReturn);
   if (func == null) return ""(Func Invalid)"";
   try
   {
      val = func(host);
      return Humanize(val);
   }
   catch (System.Exception ex)
   {
      return ex.Message;
   }
}
public static string Humanize(this object obj, int indent = 0, bool ignoreIndentation = false)
{
   if (obj == null)
      return ""(null)"";
   StringBuilder sb = new StringBuilder(ignoreIndentation ? """" : "" Type = "" + obj.GetType().FullName);
   return sb + obj.ToString();
}
static Regex regex = new Regex(@""\.?00+([,\)])"");
public static string Truncate(this string text)
{
   if (text == null) return null;
   return regex.Replace(text, m =>
   {
      return m.Groups[1].Value;
   });
}
public static string Truncate(this object text)
{
   if (text == null) return null;
   return Truncate(text.ToString());
}
");
      }

      protected virtual void AfterReturn(TypeDefinition type, IEnumerable<TypeDefinition> derivedTypes)
      {
      }

      protected virtual void BeforeReturn(TypeDefinition type, IEnumerable<TypeDefinition> derivedTypes)
      {
      }

      public virtual void DisassembleEvent(EventDefinition ev)
      {

      }

      public virtual void DisassembleField(FieldDefinition field)
      {
         if (field.IsStatic) return;
         if (field.IsPublic)
            _output.WriteLine("   sb.AppendFormat(\"[" + field.Name + "] {0} \", obj." + field.Name + ");");
      }

      public virtual void DisassembleMethod(MethodDefinition method)
      {
         if (!FilterMethod(method)) return;
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
               if (method.ReturnType.FullName == "System.Void") continue;
               var methodCallStrings = new List<string>() { method.Name + "()" };
               var paLength = method.Parameters.Count;
               List<ParameterCan[]> result = new List<ParameterCan[]>();
               result.Add(new ParameterCan[paLength]);
               if (paLength > 0)
               {
                  methodCallStrings.Clear();
                  var methodCallString = method.Name + "(";
                  //List<object[]> resultParaLists = new List<object[]>();
                  for (int ii = 0; ii < method.Parameters.Count; ii++)
                  {
                     var parameter = method.Parameters[ii];
                     List<ParameterCan> parameterCandidates = new List<ParameterCan>();
                     List<ParameterCan[]> tmpResult = new List<ParameterCan[]>();
                     var parameterType = parameter.ParameterType.Resolve();
                     if (parameterType.IsEnum)
                     {
                        if (parameterType.HasFields)
                        {
                           int tmpIi = 0;
                           foreach (FieldDefinition field in parameterType.Fields)
                           {
                              if (!field.IsPublic) continue;
                              if (field.Name == "value__") continue;
                              parameterCandidates.Add(new ParameterCan(parameterType, parameterType.Name + "." + field.Name));
                              tmpIi++;
                              if (tmpIi > 20)
                                 break;
                           }
                        }
                     }
                     else
                     {
                        switch (parameter.ParameterType.FullName)
                        {
                           //sb.AppendFormat("[GetEndPoint0] {0} ", prefix + Humanize(obj.GetEndPoint(0)));
                           //sb.AppendFormat("[GetEndPoint1] {0} ", prefix + Humanize(obj.GetEndPoint(1)));
                           case "System.Int16":
                           case "System.Int32":
                           case "System.Int64":
                           case "System.Double":
                              parameterCandidates.Add(new ParameterCan(parameterType, 0));
                              parameterCandidates.Add(new ParameterCan(parameterType, 1));
                              break;
                           case "System.String":
                              parameterCandidates.Add(new ParameterCan(parameterType, "SampleText"));
                              break;
                           default:
                              parameterCandidates.Add(new ParameterCan(parameterType, null));
                              break;
                        }
                     }

                     foreach (var para in parameterCandidates)
                     {
                        foreach (var item in result)
                        {
                           ParameterCan[] clone = new ParameterCan[paLength];
                           item.CopyTo(clone, 0);
                           clone[ii] = para;
                           tmpResult.Add(clone);
                        }
                     }
                     result = tmpResult;
                  }
               }

               var helper = TypeHumanizeHelper.CreateOrGetHelper(method.ReturnType);
               foreach (var one in result)
               {
                  //sb.AppendFormat(prefix + "[GetEndPoint(0)] {0} ", Humanize(SafeInvoke(obj, oo => oo.GetEndPoint(0)), indent + 1));
                  var methodCallString = method.Name + "(" + GetMethodCallString(one) + ")";
                  var start = "   sb.AppendFormat(prefix + \"";
                  _output.WriteLine(start + methodCallString + "] {0} \", " + helper.GetInvokeMethod(methodCallString) + ", indent + 1);");
                  //if (isBasicType(method.ReturnType))
                  //   //_output.WriteLine("   sb.AppendFormat(\"[" + methodCallString + "] {0} \", obj." + methodCallString + ");");
                  //   _output.WriteLine(start + methodCallString + "] {0} \", " + helper.GetInvokeMethod(methodCallString) + ", indent + 1);");
                  //else
                  //   //_output.WriteLine("   sb.AppendFormat(\"[" + methodCallString + "] {0} \", prefix + Humanize(obj." + methodCallString + "));");
                  //   _output.WriteLine(start + methodCallString + "] {0} \", " + helper.GetInvokeMethod(methodCallString) + ", indent + 1));");
                  ////_output.WriteLine("   sb.AppendFormat(\"[" + methodCallString + "] {0} \", prefix + Humanize(obj, oo => oo." + methodCallString + "));");
               }
               break;
            }
         }
      }

      public virtual void DisassembleProperty(PropertyDefinition prop)
      {
         foreach (var exclude in excludePropPrefixes)
         {
            if (prop.Name.StartsWith(exclude))
               return;
         }
         if (FilterMethod(prop.GetMethod))
         {
            var line = "";
            //sb.AppendFormat(prefix + "[GetEndPoint(0)] {0} ", Humanize(SafeInvoke(obj, oo => oo.GetEndPoint(0)), indent + 1));
            var start = "   sb.AppendFormat(prefix + \"";
            var helper = TypeHumanizeHelper.CreateOrGetHelper(prop.PropertyType);
            line = start + prop.Name + "] {0} \", " + helper.GetInvokeMethod(prop.Name) + ", indent + 1);";
            //if (isBasicType(prop.PropertyType))
            //   //_output.WriteLine("   sb.AppendFormat(\"[" + prop.Name + "] {0} \", obj." + prop.Name + ");");
            //   line = start + prop.Name + "] {0} \", SafeInvoke(obj, oo => oo." + prop.Name + "), indent + 1);";
            //else
            //   line = start + prop.Name + "] {0} \", Humanize(SafeInvoke(obj, oo => oo." + prop.Name + "), indent + 1));";
            ////line = "   sb.AppendFormat(\"[" + prop.Name + "] {0} \", prefix + Humanize(obj, oo => oo." + prop.Name + "));";
            if (prop.GetMethod.HasParameters)
            {
               line.Insert(3, "//");
            }
            _output.WriteLine(line);
         }

      }

      private string GetMethodCallString(ParameterCan[] paraArray)
      {
         string result = "";
         if (paraArray == null || paraArray.Length == 0)
            return result;
         object para = null;
         for (int ii = 0; ii < paraArray.Length; ii++)
         {
            var singleText = "";
            var paraOne = paraArray[ii].Value;
            if (paraOne == null)
               singleText = "null";
            else if (paraArray[ii].Type.IsEnum)
               singleText = paraOne.ToString();
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

      //public virtual bool isBasicType(TypeReference typeReference)
      //{
      //   var type = typeReference.Resolve();
      //   switch (type.FullName)
      //   {
      //      case "System.String":
      //      case "Autodesk.Revit.DB.XYZ":
      //         return true;
      //      default:
      //         break;
      //   }
      //   return type.IsValueType;
      //}

      public virtual bool FilterMethod(MethodDefinition method)
      {
         bool result = method != null && !method.IsStatic && method.IsPublic;
         if (!result) return result;
         foreach (ParameterDefinition item in method.Parameters)
         {
            if (item.IsOut)
            {
               return false;
            }
         }
         return true;
      }
   }

   public class HumanizeReturnTypeDisassembler : HumanizerDisassembler
   {
      Dictionary<string, TypeReference> referencedTypes = new Dictionary<string, TypeReference>();
      int level = 1;
      public HumanizeReturnTypeDisassembler(ITextOutput output, CancellationToken cancellationToken)
         : base(output, cancellationToken)
      {
      }
      public override void DisassembleType(TypeDefinition type, bool outputSafeInvoke = true)
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
               DisassembleType(referencedTypes[item].Resolve(), false);
         }
      }
      public override void DisassembleProperty(PropertyDefinition prop)
      {
         base.DisassembleProperty(prop);
         var thh = TypeHumanizeHelper.CreateOrGetHelper(prop.PropertyType);
         if (level < 1) return;
         if (!referencedTypes.ContainsKey(prop.PropertyType.FullName))
         {
            if (prop.PropertyType.Resolve() == prop.DeclaringType)
               return;

            if (thh.IsBasicType())
               return;
            referencedTypes.Add(prop.PropertyType.FullName, prop.PropertyType);
         }
      }
   }
   public class HumanizeReturnTypeDerivedTypesDisassembler : HumanizerDisassembler
   {
      public HumanizeReturnTypeDerivedTypesDisassembler(ITextOutput output, CancellationToken cancellationToken)
         : base(output, cancellationToken)
      {
      }

      protected override void AfterReturn(TypeDefinition type, IEnumerable<TypeDefinition> derivedTypes)
      {
         base.AfterReturn(type, derivedTypes);
         foreach (var derivedType in derivedTypes)
         {
            DisassembleType(derivedType, false);
         }
      }
      protected override void BeforeReturn(TypeDefinition type, IEnumerable<TypeDefinition> derivedTypes)
      {
         base.BeforeReturn(type, derivedTypes);
         //Autodesk.Revit.DB.Line line = obj as Line;
         //if (line != null)
         //{
         //   sb.Append(Humanize(line));
         //   return sb.ToString();
         //}
         string template = @"   DerivedTypeFullName derivedTypeVariableName = obj as DerivedTypeFullName;
   if (derivedTypeVariableName != null)
   {
     sb.Append(Humanize(derivedTypeVariableName, indent, true));
     return sb.ToString();
   }";
         foreach (var derivedType in derivedTypes)
         {
            var code = template
               .Replace("DerivedTypeFullName", derivedType.FullName)
               .Replace("derivedTypeVariableName", char.ToLower(derivedType.Name[0]) + derivedType.Name.Substring(1));
            this.Output.WriteLine(code);
         }
      }
   }

   public class TypeHumanizeHelperLib : Dictionary<string, TypeHumanizeHelper>
   {
   }

   public class TypeHumanizeHelper
   {
      protected static Dictionary<string, TypeHumanizeHelper> Lib = new Dictionary<string, TypeHumanizeHelper>();
      protected static TypeHumanizeHelper DefaultHelper = new TypeHumanizeHelper();
      static TypeHumanizeHelper()
      {
         AddHelper(new StringTypeHumanizeHelper());
         AddHelper(new XYZTypeHumanizeHelper());
      }
      static void AddHelper(TypeHumanizeHelper helper)
      {
         if (!Lib.ContainsKey(helper.FullTypeName))
            Lib.Add(helper.FullTypeName, helper);
      }

      private string _fullTypeName;
      private TypeReference _typeRef;
      public string FullTypeName
      {
         get { return _fullTypeName; }
         protected set { _fullTypeName = value; }
      }
      public TypeReference TypeRef
      {
         get { return _typeRef; }
         set
         {
            _typeRef = value;
            _fullTypeName = _typeRef.FullName;
         }
      }

      public virtual bool IsBasicType()
      {
         if (_typeRef != null)
         {
            return _typeRef.IsValueType;
         }
         throw new Exception("TypeHelper not found!");
      }

      public static TypeHumanizeHelper GetHelper(string typeFullName)
      {
         if (Lib.ContainsKey(typeFullName))
         {
            return Lib[typeFullName];
         }
         return null;
      }

      public static TypeHumanizeHelper CreateOrGetHelper(TypeReference typeRef)
      {
         var txt = typeRef.FullName;
         var helper = GetHelper(txt);
         if (helper == null)
         {
            TypeHumanizeHelper th = typeRef.IsValueType ?
               new BasicTypeHumanizeHelper()
               : new TypeHumanizeHelper();
            th.TypeRef = typeRef;
            Lib.Add(typeRef.FullName, th);
            return th;
         }
         else return helper;
      }

      public virtual string GetInvokeMethod(string methodCallString)
      {
         return "Humanize(SafeInvoke(obj, oo => oo." + methodCallString + "))";
      }
   }

   public class BasicTypeHumanizeHelper : TypeHumanizeHelper
   {
      public override bool IsBasicType()
      {
         return true;
      }
      public override string GetInvokeMethod(string methodCallString)
      {
         return "SafeInvoke(obj, oo => oo." + methodCallString + ")";
      }
   }

   public class StringTypeHumanizeHelper : TypeHumanizeHelper
   {
      public StringTypeHumanizeHelper()
      {
         FullTypeName = "System.String";
      }
      public override bool IsBasicType()
      {
         return true;
      }
      public override string GetInvokeMethod(string methodCallString)
      {
         return "SafeInvoke(obj, oo => oo." + methodCallString + ")";
      }
   }

   public class XYZTypeHumanizeHelper : TypeHumanizeHelper
   {
      public XYZTypeHumanizeHelper()
      {
         FullTypeName = "Autodesk.Revit.DB.XYZ";
      }
      public override bool IsBasicType()
      {
         return true;
      }
      public override string GetInvokeMethod(string methodCallString)
      {
         return "SafeInvoke(obj, oo => oo." + methodCallString + ").Truncate()";
      }
   }
}
