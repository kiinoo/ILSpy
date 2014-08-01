using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using ICSharpCode.ILSpy;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using CompositeLanguageClass = ICSharpCode.ILSpy.Language;
using Mono.Cecil;

namespace ToolSet
{
   [Export(typeof(CompositeLanguageClass))]
   public class HumanizeLanguage : Language
   {
      public override string FileExtension
      {
         get { return ".cs"; }
      }

      public override void DecompileType(Mono.Cecil.TypeDefinition type, ICSharpCode.Decompiler.ITextOutput output, DecompilationOptions options)
      {
         var dis = new HumanizeReturnTypeDerivedTypesDisassembler(output, options.CancellationToken);
         dis.DisassembleType(type);
      }

      public override string Name
      {
         get { return "H"; }
      }
   }

   //[Export(typeof(CompositeLanguageClass))]
   //public class HumanizeRecursiveOnMethodReturnTypeLanguage : HumanizeLanguage
   //{
   //   public override void DecompileType(Mono.Cecil.TypeDefinition type, ICSharpCode.Decompiler.ITextOutput output, DecompilationOptions options)
   //   {
   //      var dis = new HumanizeReturnTypeDisassembler(output, options.CancellationToken);
   //      dis.DisassembleType(type);
   //   }

   //   public override string Name
   //   {
   //      get { return "H+ReturnType"; }
   //   }
   //}

   public static class HumanizerContext
   {
      public static CompositionContainer compositionContainer;
      public static AggregateCatalog catalog;

      static HumanizerContext()
      {
         catalog = new AggregateCatalog();
         catalog.Catalogs.Add(new AssemblyCatalog(typeof(HumanizeLanguage).Assembly));
         compositionContainer = new CompositionContainer(catalog);
         HumanizeLanguages.Initialize(compositionContainer);
      }

      public static void Initialize()
      {
      }
   }

   public class TypeHumanizer
   {
      public TypeDefinition Type { get; set; }
      public TypeHumanizer(TypeDefinition type)
      {
         Type = type;
      }
   }

   public static class TypeHumanizers
   {
      private static ReadOnlyCollection<TypeHumanizer> allHumanizers;
      internal static void Initialize(CompositionContainer composition)
      {
         List<TypeHumanizer> list = new List<TypeHumanizer>();
         list.AddRange(composition.GetExportedValues<TypeHumanizer>());
         allHumanizers = list.AsReadOnly();
      }
   }

   public static class HumanizeLanguages
   {
      private static ReadOnlyCollection<CompositeLanguageClass> allLanguages;

      /// <summary>
      /// A list of all languages.
      /// </summary>
      public static ReadOnlyCollection<CompositeLanguageClass> AllLanguages
      {
         get { return allLanguages; }
      }

      internal static void Initialize(CompositionContainer composition)
      {
         List<CompositeLanguageClass> languages = new List<CompositeLanguageClass>();
         languages.AddRange(composition.GetExportedValues<CompositeLanguageClass>());
         allLanguages = languages.AsReadOnly();
      }

      /// <summary>
      /// Gets a language using its name.
      /// If the language is not found, C# is returned instead.
      /// </summary>
      public static CompositeLanguageClass GetLanguage(string name)
      {
         return AllLanguages.FirstOrDefault(l => l.Name == name) ?? AllLanguages.First();
      }
   }
}
