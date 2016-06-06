using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml.Linq;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Extensions.XElementExtensions;
using Sitecore.Install.Framework;
using Sitecore.IO;
using Constants = Sitecore.Install.Constants;

namespace Cognifide.PowerShell.Package.Install
{
    public class PackagePostStep : IPostStep
    {
        public void Run(ITaskOutput output, NameValueCollection metaData)
        {
            var text = metaData["Comment"] ?? string.Empty;
            if (String.IsNullOrEmpty(text)) { return; }

            var xelement = ToXElement(text);
            if (xelement == null) { return; }

            var items = xelement.Element(Constants.ItemsPrefix);
            if (items != null)
            {
                DeleteItems(items);
            }

            var files = xelement.Element(Constants.FilesPrefix);
            if (files == null) { return; }

            DeleteFiles(files);
        }

        private static void DeleteFiles(XContainer files)
        {
            foreach (var element in files.Elements())
            {
                var path = FileUtil.MapPath(element.GetAttributeValue("filename"));
                if (!File.Exists(path)) { continue; }

                try
                {
                    File.Delete(path);
                }
                catch (Exception)
                {
                    Log.Error($"The post step encountered a problem deleting the file {path}. Please remove manually.", typeof(PackagePostStep));
                }
            }
        }

        private static void DeleteItems(XContainer items)
        {
            foreach (var element in items.Elements())
            {
                var database = Factory.GetDatabase(element.GetAttributeValue("database"));

                var obj = database?.GetItem(element.GetAttributeValue("id"));
                obj?.Delete();
            }
        }

        private static XElement ToXElement(string text)
        {
            Assert.ArgumentNotNull(text, "text");
            XDocument xdocument;
            try
            {
                xdocument = XDocument.Parse(text);
            }
            catch
            {
                return null;
            }

            return xdocument.Root;
        }
    }
}