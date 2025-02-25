using PLATEAU.Geometries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TrafficSimulationTool.Runtime.SimData
{
    /// <summary>
    /// 複数データの切替用
    /// フレームデータ初期化ベース処理
    /// </summary>
    public class DataManager : MonoBehaviour, IDisposable
    {
        private List<DataSets> dataSets = new List<DataSets>();

        public DataSets CurrentDataSets { get; private set; }

        public void AddDataSet(IDataSet dataset)
        {
            if(CurrentDataSets == null || CurrentDataSets.HasData(dataset))
            {
                CurrentDataSets = new DataSets();
                AddDataSets(CurrentDataSets);
            }
            CurrentDataSets.SetData(dataset);
        }

        public void AddDataSets(DataSets data)
        {
            dataSets.Add(data);
        }

        public void RemoveDataSet(DataSets data)
        {
            dataSets.Remove(data);
        }

        public void SetCurrentDataIndex(int index)
        {
            if(dataSets.Count <= index)
            {
                Debug.LogError("GetDataByIndex : index larger than datasets length. ");
                return;
            }

            CurrentDataSets = dataSets[index];
        }

        public List<string> GetDataSetNameList()
        {
            return dataSets.Select(l => l.GetName()).ToList();
        }

        public DataSets GetDataByIndex(int index)
        {
            if (dataSets.Count <= index)
            {
                Debug.LogError("GetDataByIndex : index larger than datasets length. ");
                return null;
            }
                
            return dataSets[index];
        }

        public void Dispose()
        {
            foreach(var data in dataSets)
                data?.Dispose();

            dataSets?.Clear();
        }
    }

    public class DataSets : IDisposable
    {
        public VehicleTimelineDataSet vehicleTimelineDataSet;
        public RoadIndicatorDataSet roadIndicatorDataSet;
        public SequenceDuration duration;

        private CancellationTokenSource initilizationTokenSrc;

        public  bool Initialized {  get; private set; } = false;

        public string GetName()
        {
            return Path.GetFileName(vehicleTimelineDataSet?.name) + " / " + Path.GetFileName(roadIndicatorDataSet?.name);
        }

        public async Task<bool> Initialize(uint fps, GeoReference geoReference)
        {
            if (Initialized) return true;

            if (vehicleTimelineDataSet == null || roadIndicatorDataSet == null)
            {
                Debug.LogError("Timeline Data not set!");
                return false;
            }

            initilizationTokenSrc = new CancellationTokenSource();

            try
            {
                await Task.Run (async () => 
                {
                    duration = new SequenceDuration(fps);
                    duration.SetStartEnd(vehicleTimelineDataSet);
                    duration.SetStartEnd(roadIndicatorDataSet);
                    duration.CalculateTotalFrames();

                    vehicleTimelineDataSet.InitializeReference(geoReference);

                    await vehicleTimelineDataSet.InitializeFrames(duration, initilizationTokenSrc?.Token);
                    await roadIndicatorDataSet.InitializeFrames(duration, initilizationTokenSrc?.Token);

                });
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Initialization Canceled.");
                return false;
            }
            catch (TimeNotMatchException e)
            {
                Debug.LogError(e.Message);
                return false;
            }
            finally
            {
                initilizationTokenSrc?.Dispose();
                initilizationTokenSrc= null;
                Initialized = true;
            }

            return true;
        }

        public void CancelInitilization()
        {
            initilizationTokenSrc?.Cancel();
            initilizationTokenSrc?.Dispose();
        }

        public void SetData(IDataSet data)
        {
            if (data is VehicleTimelineDataSet)
                vehicleTimelineDataSet = (VehicleTimelineDataSet)data;
            else if (data is RoadIndicatorDataSet)
                roadIndicatorDataSet = (RoadIndicatorDataSet)data;
        }

        public bool HasData(IDataSet data)
        {
            if (data is VehicleTimelineDataSet)
                return vehicleTimelineDataSet != null;
            else if (data is RoadIndicatorDataSet)
                return roadIndicatorDataSet != null;
            return false;
        }

        public void Dispose()
        {
            CancelInitilization();
            vehicleTimelineDataSet?.Dispose();
            roadIndicatorDataSet?.Dispose();
        }
    }
    public interface IDataSet : IDisposable
    {
        void Initialize(string name, List<object> dataList);
        Task InitializeFrames(SequenceDuration duration, CancellationToken? token = null);
        DateTime GetStartTime();
        DateTime GetEndTime();
    }

}
