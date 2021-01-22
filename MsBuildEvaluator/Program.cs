using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Build.Evaluation;

namespace MsBuildEvaluator
{
    class Program
    {
        static void Main(string filePath, string properties = null, string toolsVersion = null)
        {
            // Load MsBuildProject
            var project = new Project(filePath, GetGlobalProperties(), toolsVersion ?? "Current");

            // Generate an xml with evaluated values
            var xmlProject = DumpProjectToXml(project);
            
            // Make sure the generated project can be parsed by MsBuild
            var evaluatedProject = new Project(XmlReader.Create(new StringReader(xmlProject)));

            // Write to a temp file and open in text editor
            var file = Path.GetTempFileName();
            File.WriteAllText(file, xmlProject);
            Process.Start(file);
            Debugger.Launch();
            File.Delete(file);

            // Compute global properties from command line argument
            Dictionary<string, string> GetGlobalProperties()
            {
                var globalProperties = new Dictionary<string, string>();
                if (properties != null)
                {
                    foreach (var property in properties.Split(';'))
                    {
                        var parts = property.Split('=');
                        if (parts.Length == 2)
                        {
                            globalProperties.Add(parts[0], parts[1]);
                        }
                    }
                }

                return globalProperties;
            }
        }

        private static string DumpProjectToXml(Project project)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<Project>");

            // Don't dump reserved properties as MsBuild won't accept that we modify them
            
            //sb.AppendLine("  <PropertyGroup> <!-- Reserved properties -->");
            //foreach (var property in project.Properties.Where(x => x.IsReservedProperty))
            //{
            //    sb.AppendLine($"    <{property.Name}>{property.EvaluatedValue}</{property.Name}>");
            //}
            //sb.AppendLine("  </PropertyGroup>");
            
            sb.AppendLine("  <PropertyGroup> <!-- Env properties -->");
            foreach (var property in project.Properties.Where(x => x.IsEnvironmentProperty && !x.IsReservedProperty))
            {
                sb.AppendLine($"    <{property.Name}>{property.EvaluatedValue}</{property.Name}>");
            }

            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("  <PropertyGroup> <!-- Imported properties -->");
            foreach (var property in project.Properties.Where(x =>
                x.IsImported && !x.IsReservedProperty && !x.IsEnvironmentProperty))
            {
                sb.AppendLine($"    <{property.Name}>{property.EvaluatedValue}</{property.Name}>");
            }

            sb.AppendLine("  </PropertyGroup>");

            sb.AppendLine("  <PropertyGroup> <!-- Project properties -->");
            foreach (var property in project.Properties.Where(x =>
                !x.IsImported && !x.IsReservedProperty && !x.IsEnvironmentProperty))
            {
                sb.AppendLine($"    <{property.Name}>{property.EvaluatedValue}</{property.Name}>");
            }

            sb.AppendLine("  </PropertyGroup>");

            sb.AppendLine("  <ItemGroup> <!-- Imported items --> ");
            foreach (var item in project.Items.Where(i => i.IsImported))
            {
                sb.AppendLine($"    <{item.ItemType} Include=\"{item.EvaluatedInclude}\"/>");
            }

            sb.AppendLine("  </ItemGroup>");

            sb.AppendLine("  <ItemGroup> <!-- Project items --> ");
            foreach (var item in project.Items.Where(i => !i.IsImported))
            {
                if (item.MetadataCount > 0)
                {
                    sb.AppendLine($"    <{item.ItemType} Include=\"{item.EvaluatedInclude}\">");
                    foreach (var metadata in item.Metadata)
                    {
                        sb.AppendLine($"      <{metadata.Name}>{metadata.EvaluatedValue}</{metadata.Name}>");
                    }

                    sb.AppendLine($"    </{item.ItemType}>");
                }
                else
                {
                    sb.AppendLine($"    <{item.ItemType} Include=\"{item.EvaluatedInclude}\" />");
                }
            }

            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("</Project>");
            return sb.ToString();
        }
    }
}
