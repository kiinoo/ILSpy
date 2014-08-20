using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ToolSet
{
   public class ParameterCan
   {
      private TypeDefinition _type;
      private object _value;

      public ParameterCan()
      {
      }
      public ParameterCan(TypeDefinition type, object value)
      {
         _type = type;
         _value = value;
      }

      public TypeDefinition Type
      {
         get { return _type; }
         set { _type = value; }
      }
      public object Value
      {
         get { return _value; }
         set { _value = value; }
      }
   }

}
