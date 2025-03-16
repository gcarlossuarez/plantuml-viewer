using System;
using System.IO;
using PlantUmlViewer;
using PlantUmlViewer.NetFx;


class Program
{
    static void Main(string[] args)
    {

        try
        {
            // Path of the file containing the code with the comments
            string filePath = @"D:\Proyectos Visual Studio\VS2022\ViewPlantUmlComments\TestTxt\Test1.txt"; // Cambia esto por la ruta de tu archivo
            string plantUmlJarFullPath =
                @"D:\Proyectos Visual Studio\VS2022\ViewPlantUmlComments\Jars\plantuml-1.2025.0.jar";
            string outputDirectory = @"D:\Proyectos Visual Studio\VS2022\ViewPlantUmlComments\Out";
            FormatPlantUmlOutput formatPlantUmlOutput = FormatPlantUmlOutput.Png;

            (bool result, string errorDescription) = PlantUmlTextParser.ProcessPlantUmlText(filePath, 17,
                plantUmlJarFullPath: plantUmlJarFullPath,
                formatPlantUmlOutput: formatPlantUmlOutput,
                outPutdirectory: outputDirectory,
                encodingPlantUmlTextFile: null,
                plantUmlLimitSize: 8192);
            if (!result)
            {
                Console.WriteLine($"PlantUML NOT processed successfully!. Error:{errorDescription}");
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    
}