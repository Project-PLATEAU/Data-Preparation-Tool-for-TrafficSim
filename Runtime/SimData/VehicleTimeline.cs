using PLATEAU.Geometries;
using PLATEAU.Native;
using System;
using UnityEngine;
using TrafficSimulationTool.Runtime.Util;

namespace TrafficSimulationTool.Runtime.SimData
{
    [System.Serializable]
    public class VehicleTimeline
    {
        [CsvHeader("時刻")]
        //[CsvHeader("Time")]
        public string TimeStamp;

        [CsvHeader("車両ID")]
        //[CsvHeader("VID")]
        public string VehicleID;

        //[CsvHeader("車両タイプ")]
        [CsvHeader("車種")]
        public string VehicleType;

        [CsvHeader("車両位置緯度")]
        //[CsvHeader("Pos.y")]
        public float Latitude;

        [CsvHeader("車両位置経度")]
        //[CsvHeader("Pos.x")]
        public float Longitude;

        [CsvHeader("走行リンクID")]
        //[CsvHeader("LinkID")]
        public string LinkID;

        [CsvHeader("リンク始端からの走行位置")]
        //[CsvHeader("走行位置")]
        //[CsvHeader("PosOnLink")]
        public float Offset;

        [CsvHeader("走行車線番号")]
        //[CsvHeader("Lane")]
        public float Lane;

        //[CsvHeader("トラック")]
        [CsvHeader("トラック番号")]
        public float Track;

        [CsvHeader("速度")]
        //[CsvHeader("Spd_kph")]
        public float Speed;

        [CsvHeader("出発発生点ID")]
        //[CsvHeader("出発点")]
        //[CsvHeader("Origin")]
        public string Departure;

        [CsvHeader("到着集中点ID")]
        //[CsvHeader("到着点")]
        //[CsvHeader("Destination")]
        public string Destination;

        public uint Index {  get; set; }

        private Nullable<double> _timeSeconds;
        public double TimeSeconds { 
            get{
                if(_timeSeconds  == null)
                    _timeSeconds = TimelineUtil.GetTimeSeconds(TimeStamp);
                return _timeSeconds ?? 0; 
            }
        }

        public UnityEngine.Vector3 Position { get; private set; }

        public void Initialize(GeoReference geoReference)
        {
            var coordinate = new GeoCoordinate(Latitude, Longitude, 0);
            var point = geoReference.Project(coordinate);
            Position = new UnityEngine.Vector3((float)point.X, (float)point.Y, (float)point.Z);
        }
        
    }
}