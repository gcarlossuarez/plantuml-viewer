using System;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using static System.Windows.Forms.LinkLabel;

namespace PlantUmlCommentExtension
{

    public enum FormatPlantUmlOutput
    {
        None = 0,
        Png = 1,
        Svg = 2,
    }


    internal sealed class ProcessPlantUmlCommand
    {
        public static readonly Guid CommandSet = new Guid("{37381321-2386-4d05-8aba-a9088e66fed9}");
        public const int CommandId = 0x0100;

        private readonly AsyncPackage package;
        private readonly OleMenuCommandService commandService;

        private ProcessPlantUmlCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            commandService.AddCommand(new MenuCommand(async (s, e) => await ExecuteAsync(package),
                new CommandID(CommandSet, CommandId)));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var cmdId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(async (s, e) => await ExecuteAsync(package), cmdId);
            commandService?.AddCommand(menuItem);
        }

        private static async Task ExecuteAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Get the active editor view
            var dte = await package.GetServiceAsync(typeof(EnvDTE.DTE)) as DTE;
            var activeDocument = dte?.ActiveDocument;

            if (activeDocument == null)
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    "Something goes wrong. The file you are currently in could not be determined.",
                    "Problems!!!",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // Get the line where the click was made
            TextSelection selection = (TextSelection)activeDocument.Selection;
            int clickedLine = selection.ActivePoint.Line;

            // Get text from the editor (without saving the file)
            IVsTextManager txtMgr = await package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if (txtMgr == null)
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    "Something goes wrong. The content in the file you are currently in could not be determined.",
                    "Problems!!!",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            IVsTextView textView;
            txtMgr.GetActiveView(1, null, out textView);

            if (textView == null)
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    "Something goes wrong. The text view of  content in the file you are currently in could not be determined.",
                    "Problems!!!",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            textView.GetBuffer(out IVsTextLines lines);
            lines.GetLineCount(out int lineCount);

            List<string> allLines = new List<string>();
            for (int i = 0; i < lineCount; i++)
            {
                lines.GetLengthOfLine(i, out int lineLength);
                lines.GetLineText(i, 0, i, lineLength, out string lineText);

                allLines.Add(lineText);
            }

            // Recover user-configured options.
            var options = ((PlantUmlCommentExtensionPackage)package).Options;

            if (options.FormatOutputDiagram == FormatPlantUmlOutput.None)
            {
                VsShellUtilities.ShowMessageBox(
                package,
                "Please first configure the format of the output diagram in the options (Tools --> Options --> PlantUML Options --> General).",
                "Incomplete configuration",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // Save all lines in an aux file
            string fullPathInputFile = Path.Combine(options.OutputDirectory, "inputPlantUml.txt");
            File.WriteAllLines(fullPathInputFile, allLines);

            // Validating that the user has configured the PlantUML.jar file path and the output
            // directory.
            if (string.IsNullOrWhiteSpace(options.PlantUmlJarFilePath) ||
                string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    "Please first configure the path to the PlantUML.jar file and the output directory in the options (Tools \u2192 Options \u2192 PlantUML Options \u2192 General).",
                    "Incomplete configuration",
                    OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // Call the PlantUml Parser function
            (bool result, string errorDescription) = PlantUmlViewer.NetFx.PlantUmlTextParser.ProcessPlantUmlText(fullPathInputFile, clickedLine,
                plantUmlJarFullPath: options.PlantUmlJarFilePath,
                formatPlantUmlOutput:
                ConvertFormatPlantUmlToPlantUmlViewerNetFxFormatPlantUml(options.FormatOutputDiagram),
                outPutdirectory: options.OutputDirectory,
                encodingPlantUmlTextFile: !string.IsNullOrWhiteSpace(options.EncodingPlantUmlTextFile)
                    ? Encoding.GetEncoding(options.EncodingPlantUmlTextFile)
                    : null,
                plantUmlLimitSize: options.PlantUmlLimitSize);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!result)
            {
                VsShellUtilities.ShowMessageBox(
                    package,
                    $"PlantUML NOT processed successfully!. Error:{errorDescription}",
                    "PlantUML Extension",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private static async Task ExecuteAsync(AsyncPackage package, OleMenuCmdEventArgs e)
        {
            await ExecuteAsync(package);
        }

        private static PlantUmlViewer.NetFx.FormatPlantUmlOutput ConvertFormatPlantUmlToPlantUmlViewerNetFxFormatPlantUml(FormatPlantUmlOutput formatPlantUmlOutput)
        {
            switch (formatPlantUmlOutput)
            {
                case FormatPlantUmlOutput.Png:
                    return PlantUmlViewer.NetFx.FormatPlantUmlOutput.Png;
                case FormatPlantUmlOutput.Svg:
                    return PlantUmlViewer.NetFx.FormatPlantUmlOutput.Svg;
                case FormatPlantUmlOutput.None:
                    return PlantUmlViewer.NetFx.FormatPlantUmlOutput.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(formatPlantUmlOutput), formatPlantUmlOutput, null);
            }
        }
    }

}
