using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Linq;
using System.Threading;
using System.Xml;
using System.IO;
using CompositeLanguageClass = ICSharpCode.ILSpy.Language;

namespace ToolSet
{
   /// <summary>
   /// Base settings class
   /// </summary>
   public class SettingsBase
   {
      readonly XElement root;

      SettingsBase()
      {
         this.root = new XElement(RootName);
      }

      SettingsBase(XElement root)
      {
         this.root = root;
      }

      public XElement this[XName section]
      {
         get
         {
            return root.Element(section) ?? new XElement(section);
         }
      }

      /// <summary>
      /// Loads the settings file from disk.
      /// </summary>
      /// <returns>
      /// An instance used to access the loaded settings.
      /// </returns>
      public static SettingsBase Load()
      {
         using (new MutexProtector(ConfigFileMutex))
         {
            try
            {
               XDocument doc = LoadWithoutCheckingCharacters(GetConfigFile());
               return new SettingsBase(doc.Root);
            }
            catch (IOException)
            {
               return new SettingsBase();
            }
            catch (XmlException)
            {
               return new SettingsBase();
            }
         }
      }

      static XDocument LoadWithoutCheckingCharacters(string fileName)
      {
         return XDocument.Load(fileName, LoadOptions.None);
      }

      /// <summary>
      /// Saves a setting section.
      /// </summary>
      public static void SaveSettings(XElement section)
      {
         Update(
            delegate(XElement root)
            {
               XElement existingElement = root.Element(section.Name);
               if (existingElement != null)
                  existingElement.ReplaceWith(section);
               else
                  root.Add(section);
            });
      }

      /// <summary>
      /// Updates the saved settings.
      /// We always reload the file on updates to ensure we aren't overwriting unrelated changes performed
      /// by another ILSpy instance.
      /// </summary>
      public static void Update(Action<XElement> action)
      {
         using (new MutexProtector(ConfigFileMutex))
         {
            string config = GetConfigFile();
            XDocument doc;
            try
            {
               doc = LoadWithoutCheckingCharacters(config);
            }
            catch (IOException)
            {
               // ensure the directory exists
               Directory.CreateDirectory(Path.GetDirectoryName(config));
               doc = new XDocument(new XElement(RootName));
            }
            catch (XmlException)
            {
               doc = new XDocument(new XElement(RootName));
            }
            doc.Root.SetAttributeValue("version", RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision);
            action(doc.Root);
            doc.Save(config, SaveOptions.None);
         }
      }

      public static string GetConfigFile()
      {
         return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FilePathUnderAppData);
      }
      static string ConfigFileMutex
      {
         get { return "D94C35D8-F89F-4773-AFA6-C4FC837DF2DF"; }
      }
      static string RootName
      {
         get { return "Settings";}
      }
      /// <summary>
      /// Helper class for serializing access to the config file when multiple ILSpy instances are running.
      /// </summary>
      sealed class MutexProtector : IDisposable
      {
         readonly Mutex mutex;

         public MutexProtector(string name)
         {
            bool createdNew;
            this.mutex = new Mutex(true, name, out createdNew);
            if (!createdNew)
            {
               try
               {
                  mutex.WaitOne();
               }
               catch (AbandonedMutexException)
               {
               }
            }
         }

         public void Dispose()
         {
            mutex.ReleaseMutex();
            mutex.Dispose();
         }
      }

      /// <summary>
      /// e.g. "ICSharpCode\\ILSpy.xml"
      /// </summary>
      static string FilePathUnderAppData
      {
         get
         {
            var typeName = typeof(SettingsBase).Name;
            return System.Reflection.Assembly.GetExecutingAssembly().GetName() + "\\" + typeName + ".xml";
         }
      }
   }

   public class ToolSetSettings : INotifyPropertyChanged
   {
      static ToolSetSettings instance;

      public static ToolSetSettings Instance
      {
         get
         {
            if (instance == null)
            {
               var sectionName = typeof(ToolSetSettings).Name;
               var settings = SettingsBase.Load();
               var subSettings = settings[sectionName];
               if (subSettings == null)
                  subSettings = new XElement(sectionName);
               instance = new ToolSetSettings(subSettings);
            }
            return instance;
         }
      }

      CompositeLanguageClass language;

      public CompositeLanguageClass Language
      {
         get { return language; }
         set
         {
            if (language != value)
            {
               language = value;
               OnPropertyChanged("Language");
            }
         }
      }
      public ToolSetSettings(XElement element)
      {
         this.Language = HumanizeLanguages.GetLanguage((string)element.Element("Language"));
      }

      public XElement SaveAsXml()
      {
         return new XElement(
            "ToolSetSettings",
            new XElement("Language", this.Language.Name)
         );
      }

      public event PropertyChangedEventHandler PropertyChanged;

      protected virtual void OnPropertyChanged(string propertyName)
      {
         if (PropertyChanged != null)
         {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
         }
      }

      public ToolSetSettings Clone()
      {
         ToolSetSettings f = (ToolSetSettings)MemberwiseClone();
         f.PropertyChanged = null;
         return f;
      }

      
   }
}
