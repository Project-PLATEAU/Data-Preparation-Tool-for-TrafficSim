using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TrafficSimulationTool.Editor
{
    public class ModuleProcess
    {
        private static readonly string RootPath = "Packages/com.synesthesias.plateau-trafficsimulationtool/Modules/";

        public static readonly string FN008009Path = RootPath + "fn008009/";

        private static string FN008009ModulePath = FN008009Path + "fn008_TravelRouteDeterminationFn.exe";

        public static readonly string FN010011Path = RootPath + "fn010011/";

        public static readonly string FN010011InputPath = FN010011Path + "fn010_fn011_input/";

        private static string FN010011ModulePath = FN010011Path + "Development_Traffic_Volume.exe";

        public class ArgumentSettings
        {
            public string InputFn008Dir { get; set; } // -08indir
            public string OutputNodeGeojson { get; } = RoadNetworkExporter.ExportFileNameNode; // -innwnd
            public string OutputLinkGeojson { get; } = RoadNetworkExporter.ExportFileNameLink; // -innwlk
            public string OutputZoneGeojson { get; } = RoadNetworkExporter.ExportFileNameZone; // -innwzn
            public string TrafficVolumeCsv { get; } = EstimationViewController.TrafficVolumeFileName; // -intrfvl
            public string SettingIni { get; } = "setting.ini";// -ini
            public string OutputDir { get; set; } // -08outdir
            public string RouteCsv { get; } = "IF207_route.csv"; // -08outfn
            public string EstimateODTrafficVolumeExe { get; } = Path.GetFullPath(FN008009Path + "FN009_EstimateODTrafficVolume.exe"); // -fn09
            public string InputFn009Dir { get; set; } // -09indir
            public string ObsLinkCsv { get; } = "IF105_lk.csv"; // -inobslk
            public string ObsCrsCsv { get; } = "IF106_crs.csv"; // -inobscrs
            public string OutputFn009Dir { get; set; } // -09outdir
            public string EstimateGnRCsv { get; } = "IF103_estgnr.csv"; // -09outfn
            public string StartTime { get; set; } // -sttm
            public string EndTime { get; set; } // -edtm
            public int TimeResolution { get; set; } // -rstm
            public string VehicleType { get; } = "small"; // -vtype
            public int DebugFlag { get; } = 1; // -dbg

            // 引数の文字列を生成するメソッド
            public string GetArguments()
            {
                StringBuilder argsBuilder = new StringBuilder();

                argsBuilder.Append($"-08indir {InputFn008Dir} ");
                argsBuilder.Append($"-innwnd {OutputNodeGeojson} ");
                argsBuilder.Append($"-innwlk {OutputLinkGeojson} ");
                argsBuilder.Append($"-innwzn {OutputZoneGeojson} ");
                argsBuilder.Append($"-intrfvl {TrafficVolumeCsv} ");
                argsBuilder.Append($"-ini {SettingIni} ");
                argsBuilder.Append($"-08outdir {OutputDir} ");
                argsBuilder.Append($"-08outfn {RouteCsv} ");
                argsBuilder.Append($"-fn09 {EstimateODTrafficVolumeExe} ");
                argsBuilder.Append($"-09indir {InputFn009Dir} ");
                argsBuilder.Append($"-inobslk {ObsLinkCsv} ");
                argsBuilder.Append($"-inobscrs {ObsCrsCsv} ");
                argsBuilder.Append($"-09outdir {OutputFn009Dir} ");
                argsBuilder.Append($"-09outfn {EstimateGnRCsv} ");
                argsBuilder.Append($"-sttm {StartTime} ");
                argsBuilder.Append($"-edtm {EndTime} ");
                argsBuilder.Append($"-rstm {TimeResolution} ");
                argsBuilder.Append($"-vtype {VehicleType} ");
                argsBuilder.Append($"-dbg {DebugFlag}");

                return argsBuilder.ToString();
            }
        }

        public static async Task<int> RunModuleFN008009(ArgumentSettings args)
        {
            // プロセスを開始
            Process process = new Process
            {
                // ProcessStartInfo を設定
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(FN008009ModulePath),
                    // 必要に応じて引数を渡す場合
                    Arguments = args.GetArguments(),
                    // ウィンドウを隠したい場合は true にする
                    CreateNoWindow = false,
                    // ウィンドウを表示する場合は true にする
                    UseShellExecute = true,
                    // 作業ディレクトリを設定
                    WorkingDirectory = Path.GetFullPath(FN008009Path),
                }
            };

            UnityEngine.Debug.Log("Process start." + process.StartInfo.FileName);
            UnityEngine.Debug.Log("Process start." + process.StartInfo.Arguments);

            // プロセス開始
            process.Start();

            // プロセス終了まで待機（非同期）
            await Task.Run(() => process.WaitForExit());

            UnityEngine.Debug.Log("Process finished." + process.ExitCode);

            // プロセスの終了コードを返す
            return process.ExitCode;
        }

        public static async Task<int> RunModuleFN010011()
        {
            // プロセスを開始
            Process process = new Process
            {
                // ProcessStartInfo を設定
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(FN010011ModulePath),
                    // 必要に応じて引数を渡す場合
                    Arguments = "",
                    // ウィンドウを隠したい場合は true にする
                    CreateNoWindow = false,
                    // ウィンドウを表示する場合は true にする
                    UseShellExecute = true,
                    // 作業ディレクトリを設定
                    WorkingDirectory = Path.GetFullPath(FN010011Path),
                }
            };

            UnityEngine.Debug.Log("Process start." + process.StartInfo.FileName);
            UnityEngine.Debug.Log("Process start." + process.StartInfo.Arguments);

            // プロセス開始
            process.Start();

            // プロセス終了まで待機（非同期）
            await Task.Run(() => process.WaitForExit());

            UnityEngine.Debug.Log("Process finished." + process.ExitCode);

            // プロセスの終了コードを返す
            return process.ExitCode;
        }
    }
}