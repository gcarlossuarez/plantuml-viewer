using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantUmlViewer.NetFx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


        /// <summary>
        /// Enumeration that defines the output format of PlantUML.
        /// </summary>
        public enum FormatPlantUmlOutput
        {
            None = 0,
            Png = 1,
            Svg = 2,
        }

        public class PlantUmlTextParser
        {
            public static (bool, string) ProcessPlantUmlText(string filePath, int clickedLine, string plantUmlJarFullPath,
                FormatPlantUmlOutput formatPlantUmlOutput, string outPutdirectory,
                Encoding encodingPlantUmlTextFile = null, int plantUmlLimitSize = 8192,
                int dpi = 300)
            {
                (bool readingFileSuccesfful, List<string> plantUmlText) = FindPlantUmlText(filePath, clickedLine);

                if (!readingFileSuccesfful)
                {
                    return (false, string.Join("n", plantUmlText));
                }

                Directory.CreateDirectory(outPutdirectory);
                string outputTextFile = Path.Combine(outPutdirectory, "plantuml.txt");
                File.WriteAllText(outputTextFile, string.Join(Environment.NewLine, plantUmlText));

                string errorDescription = string.Empty;
            if (!VisualizePlantUmlFile(outputTextFile, plantUmlJarFullPath, formatPlantUmlOutput,
                        encodingPlantUmlTextFile, plantUmlLimitSize, dpi, ref errorDescription))
                {
                    return (false, errorDescription);
                }

                return (true, string.Join("n", plantUmlText));
            }

            /// <summary>
            /// Method that reads the file and extracts the PlantUML text between "@startuml" and "@enduml"
            /// </summary>
            /// <param name="filePath"></param>
            /// <param name="clickedLine"></param>
            /// <returns></returns>
            private static (bool, List<string>) FindPlantUmlText(string filePath, int clickedLine)
            {
                try
                {
                    List<string> plantUmlText = new List<string>();

                    // Read all lines in the file
                    string[] lines = File.ReadAllLines(filePath);

                    // Search up from the clicked line to find "@startuml"
                    int startUmlLine = -1;
                    for (int i = clickedLine; i >= 0; i--)
                    {
                        if (lines[i].Trim().ToLower() == "@startuml")
                        {
                            startUmlLine = i;
                            break;
                        }
                    }

                    // If "@startuml" is not found, display a message and exit
                    if (startUmlLine == -1)
                    {
                        return (false, new List<string>(){ "'@startuml' not found." });
                    }

                    // Search down from the clicked line to find "@enduml"
                    int endUmlLine = -1;
                    for (int i = clickedLine; i < lines.Length; i++)
                    {
                        if (lines[i].Trim().ToLower() == "@enduml")
                        {
                            endUmlLine = i;
                            break;
                        }
                    }

                    // If "@enduml" is not found, display a message and exit
                    if (endUmlLine == -1)
                    {
                        return (false, new List<string>(){ "'@enduml' not found." });
                    }

                    // Display the content between "@startuml" and "@enduml"
                    Console.WriteLine("Content found between @startuml and @enduml.");
                    for (int i = startUmlLine; i <= endUmlLine; i++)
                    {
                        plantUmlText.Add(lines[i]);
                    }

                    return (true, plantUmlText);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                return (false, new List<string>() {e.Message});
            }
            }

        /// <summary>
        /// Method that generates the PlantUML diagram from the text file.
        /// </summary>
        /// <param name="outputTextFile"></param>
        /// <param name="plantUmlJarFullPath"></param>
        /// <param name="formatPlantUmlOutput"></param>
        /// <param name="encodingPlantUmlTextFile"></param>
        /// <param name="plantUmlLimitSize"></param>
        /// <param name="dpi"></param>
        /// <param name="errorDescription"></param>
        /// <returns></returns>
        private static bool VisualizePlantUmlFile(string outputTextFile, string plantUmlJarFullPath,
                FormatPlantUmlOutput formatPlantUmlOutput, Encoding encodingPlantUmlTextFile, int plantUmlLimitSize,
                int dpi, ref string errorDescription)
            {
                const string logFileName = "log_file.txt";

                errorDescription = String.Empty;
                string outputDirectory = Path.GetDirectoryName(outputTextFile) ?? ".\\";

                string logFilePath = Path.Combine(outputDirectory, logFileName);

                // Output path for the image or SVG
                string outputPlantUmlVisualFilePath =
                    formatPlantUmlOutput == FormatPlantUmlOutput.Png
                        ? Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(outputTextFile) + ".png")
                        : Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(outputTextFile) + ".svg");

                // Command to run PlantUML
                // -o Creates a directory exclusively for the output files (png or svg)
                //string javaArguments = $"-jar \"{plantUmlJarFullPath}\" \"{outputTextFile}\" -o \"{outputPlantUmlVisualFilePath}\"";
                string javaArguments = $"-jar \"{plantUmlJarFullPath}\" \"{outputTextFile}\"";

                // Increase maximum size and DPI
                javaArguments += $" -DPLANTUML_LIMIT_SIZE={plantUmlLimitSize} -dpi {dpi}";

                // Optionally, split the diagram into multiple images in the output directory.
                //javaArguments += " -split";

                // Optionally, generate SVG instead of PNG
                if (formatPlantUmlOutput == FormatPlantUmlOutput.Svg)
                {
                    javaArguments += " -tsvg";
                }

                if (encodingPlantUmlTextFile == null) encodingPlantUmlTextFile = Encoding.UTF8;
                javaArguments += $" -charset {encodingPlantUmlTextFile.WebName}";

                // Configurar el proceso
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = javaArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                bool success = false;

                File.AppendAllText(logFilePath, $"Java Path: {processInfo.FileName}\n");
                File.AppendAllText(logFilePath, $"PlantUML JAR: {plantUmlJarFullPath}\n");
                File.AppendAllText(logFilePath, $"Arguments: {javaArguments}\n");

                using (Process process = new Process())
                {
                    process.StartInfo = processInfo;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    File.AppendAllText(logFilePath, $"Exit Code: {process.ExitCode}\n");
                    File.AppendAllText(logFilePath, $"Standard Output: {output}\n");
                    File.AppendAllText(logFilePath, $"Standard Error: {error}\n");

                    if (process.ExitCode == 0)
                    {
                        success = true;
                    }
                    else
                    {
                        errorDescription = "Error generating diagram.";
                        return false;
                    }
                }


                // Open the file text with PlantUml specification
                (success, errorDescription) = OpenFileWithDefaultProgram(outputPlantUmlVisualFilePath);

                return success;
            }

            /// <summary>
            /// Method that opens a file with the default program
            /// </summary>
            /// <param name="fullFilePath"></param>
            private static (bool, string) OpenFileWithDefaultProgram(string fullFilePath)
            {
                string errorDescription = String.Empty;
                ProcessStartInfo processInfo;
                if (System.IO.File.Exists(fullFilePath))
                {
                    // Open file with default program
                    processInfo = new ProcessStartInfo
                    {
                        FileName = fullFilePath,
                        UseShellExecute = true  // Use the system shell to open the file
                    };

                    try
                    {
                        using (Process process = new Process())
                        {
                            process.StartInfo = processInfo;
                            process.Start();
                            process.WaitForExit();

                            if (process.ExitCode == 0)
                            {
                                return (true, errorDescription);
                            }

                            errorDescription = "Error opening PlantUml file.";
                            return (false, errorDescription);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorDescription = "Error loading file :'" + fullFilePath + "'" + ex.Message;
                        return (false, errorDescription);
                    }
                }

                errorDescription = $"File '{fullFilePath}' not exists.";
                return (false, errorDescription);
            }

        }
    

}
