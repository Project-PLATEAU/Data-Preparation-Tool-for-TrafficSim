using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrafficSimulationTool.Runtime
{
    //TODO: windows.forms を使わない

    public class FileBrowser
    {

        public async Task<BrowseResult> Browse()
        {
            BrowseResult result = null;
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select a CSV file"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                result = new BrowseResult(BrowseResult.ResultType.OK, openFileDialog.FileName);
            }
            else
            {
                result = new BrowseResult(BrowseResult.ResultType.CANCEL);
            }

            return await Task.FromResult<BrowseResult>(result);
        }
    }

    public class BrowseResult
    {
        public enum ResultType
        {
            OK,
            CANCEL
        }

        public string FilePath;
        public ResultType Result;

        public BrowseResult(ResultType result, string path = "")
        {
            Result = result;
            FilePath = path;
        }
    }
}
