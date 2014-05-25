using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyForTest
{
   class Program
   {
      static void Main(string[] args)
      {
         Apple a = new Apple();
         //a.Id = 12;
      }
   }

   public class Apple
   {

      public int Id
      {
         get;
         private set;
      }
   }
}
