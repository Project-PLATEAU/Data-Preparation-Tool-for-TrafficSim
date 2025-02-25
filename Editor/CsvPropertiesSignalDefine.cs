using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrafficSimulationTool.Runtime;

namespace TrafficSimulationTool.Editor
{
    /// <summary>
    /// 信号定義のプロパティを保持するクラス
    /// </summary>
    [System.Serializable]
    public class CsvPropertiesSignalDefine
    {
        [CsvHeader("日付")]
        public string Date;

        [CsvHeader("情報源コード")]
        public string SourceCode;

        [CsvHeader("交差点番号")]
        public string IntersectionNumber;

        [CsvHeader("流入リンク数")]
        public int IncomingLinkCount;

        [CsvHeader("流出リンク数")]
        public int OutgoingLinkCount;

        // 流入リンク1〜8定義
        [CsvHeader("流入リンク＃１定義_２次メッシュコード")]
        public int IncomingLink1MeshCode;

        [CsvHeader("流入リンク＃１定義_リンク区分")]
        public int IncomingLink1Type;

        [CsvHeader("流入リンク＃１定義_リンク番号")]
        public string IncomingLink1Number;

        [CsvHeader("流入リンク＃２定義_２次メッシュコード")]
        public int IncomingLink2MeshCode;

        [CsvHeader("流入リンク＃２定義_リンク区分")]
        public int IncomingLink2Type;

        [CsvHeader("流入リンク＃２定義_リンク番号")]
        public string IncomingLink2Number;

        [CsvHeader("流入リンク＃３定義_２次メッシュコード")]
        public int IncomingLink3MeshCode;

        [CsvHeader("流入リンク＃３定義_リンク区分")]
        public int IncomingLink3Type;

        [CsvHeader("流入リンク＃３定義_リンク番号")]
        public string IncomingLink3Number;

        [CsvHeader("流入リンク＃４定義_２次メッシュコード")]
        public int IncomingLink4MeshCode;

        [CsvHeader("流入リンク＃４定義_リンク区分")]
        public int IncomingLink4Type;

        [CsvHeader("流入リンク＃４定義_リンク番号")]
        public string IncomingLink4Number;

        [CsvHeader("流入リンク＃５定義_２次メッシュコード")]
        public int IncomingLink5MeshCode;

        [CsvHeader("流入リンク＃５定義_リンク区分")]
        public int IncomingLink5Type;

        [CsvHeader("流入リンク＃５定義_リンク番号")]
        public string IncomingLink5Number;

        [CsvHeader("流入リンク＃６定義_２次メッシュコード")]
        public int IncomingLink6MeshCode;

        [CsvHeader("流入リンク＃６定義_リンク区分")]
        public int IncomingLink6Type;

        [CsvHeader("流入リンク＃６定義_リンク番号")]
        public string IncomingLink6Number;

        [CsvHeader("流入リンク＃７定義_２次メッシュコード")]
        public int IncomingLink7MeshCode;

        [CsvHeader("流入リンク＃７定義_リンク区分")]
        public int IncomingLink7Type;

        [CsvHeader("流入リンク＃７定義_リンク番号")]
        public string IncomingLink7Number;

        [CsvHeader("流入リンク＃８定義_２次メッシュコード")]
        public int IncomingLink8MeshCode;

        [CsvHeader("流入リンク＃８定義_リンク区分")]
        public int IncomingLink8Type;

        [CsvHeader("流入リンク＃８定義_リンク番号")]
        public string IncomingLink8Number;

        // 流出リンク1〜8定義
        [CsvHeader("流出リンク＃１定義_２次メッシュコード")]
        public int OutgoingLink1MeshCode;

        [CsvHeader("流出リンク＃１定義_リンク区分")]
        public int OutgoingLink1Type;

        [CsvHeader("流出リンク＃１定義_リンク番号")]
        public string OutgoingLink1Number;

        [CsvHeader("流出リンク＃２定義_２次メッシュコード")]
        public int OutgoingLink2MeshCode;

        [CsvHeader("流出リンク＃２定義_リンク区分")]
        public int OutgoingLink2Type;

        [CsvHeader("流出リンク＃２定義_リンク番号")]
        public string OutgoingLink2Number;

        [CsvHeader("流出リンク＃３定義_２次メッシュコード")]
        public int OutgoingLink3MeshCode;

        [CsvHeader("流出リンク＃３定義_リンク区分")]
        public int OutgoingLink3Type;

        [CsvHeader("流出リンク＃３定義_リンク番号")]
        public string OutgoingLink3Number;

        [CsvHeader("流出リンク＃４定義_２次メッシュコード")]
        public int OutgoingLink4MeshCode;

        [CsvHeader("流出リンク＃４定義_リンク区分")]
        public int OutgoingLink4Type;

        [CsvHeader("流出リンク＃４定義_リンク番号")]
        public string OutgoingLink4Number;

        [CsvHeader("流出リンク＃５定義_２次メッシュコード")]
        public int OutgoingLink5MeshCode;

        [CsvHeader("流出リンク＃５定義_リンク区分")]
        public int OutgoingLink5Type;

        [CsvHeader("流出リンク＃５定義_リンク番号")]
        public string OutgoingLink5Number;

        [CsvHeader("流出リンク＃６定義_２次メッシュコード")]
        public int OutgoingLink6MeshCode;

        [CsvHeader("流出リンク＃６定義_リンク区分")]
        public int OutgoingLink6Type;

        [CsvHeader("流出リンク＃６定義_リンク番号")]
        public string OutgoingLink6Number;

        [CsvHeader("流出リンク＃７定義_２次メッシュコード")]
        public int OutgoingLink7MeshCode;

        [CsvHeader("流出リンク＃７定義_リンク区分")]
        public int OutgoingLink7Type;

        [CsvHeader("流出リンク＃７定義_リンク番号")]
        public string OutgoingLink7Number;

        [CsvHeader("流出リンク＃８定義_２次メッシュコード")]
        public int OutgoingLink8MeshCode;

        [CsvHeader("流出リンク＃８定義_リンク区分")]
        public int OutgoingLink8Type;

        [CsvHeader("流出リンク＃８定義_リンク番号")]
        public string OutgoingLink8Number;

        // スプリットの通行権 1〜6
        [CsvHeader("スプリット＃１通行権_流入リンク＃１")]
        public int Split1IncomingLink1;

        [CsvHeader("スプリット＃１通行権_流入リンク＃２")]
        public int Split1IncomingLink2;

        [CsvHeader("スプリット＃１通行権_流入リンク＃３")]
        public int Split1IncomingLink3;

        [CsvHeader("スプリット＃１通行権_流入リンク＃４")]
        public int Split1IncomingLink4;

        [CsvHeader("スプリット＃１通行権_流入リンク＃５")]
        public int Split1IncomingLink5;

        [CsvHeader("スプリット＃１通行権_流入リンク＃６")]
        public int Split1IncomingLink6;

        [CsvHeader("スプリット＃１通行権_流入リンク＃７")]
        public int Split1IncomingLink7;

        [CsvHeader("スプリット＃１通行権_流入リンク＃８")]
        public int Split1IncomingLink8;

        [CsvHeader("スプリット＃１通行権_流出リンク＃１")]
        public int Split1OutgoingLink1;

        [CsvHeader("スプリット＃１通行権_流出リンク＃２")]
        public int Split1OutgoingLink2;

        [CsvHeader("スプリット＃１通行権_流出リンク＃３")]
        public int Split1OutgoingLink3;

        [CsvHeader("スプリット＃１通行権_流出リンク＃４")]
        public int Split1OutgoingLink4;

        [CsvHeader("スプリット＃１通行権_流出リンク＃５")]
        public int Split1OutgoingLink5;

        [CsvHeader("スプリット＃１通行権_流出リンク＃６")]
        public int Split1OutgoingLink6;

        [CsvHeader("スプリット＃１通行権_流出リンク＃７")]
        public int Split1OutgoingLink7;

        [CsvHeader("スプリット＃１通行権_流出リンク＃８")]
        public int Split1OutgoingLink8;

        // 2
        [CsvHeader("スプリット＃２通行権_流入リンク＃１")]
        public int Split2IncomingLink1;

        [CsvHeader("スプリット＃２通行権_流入リンク＃２")]
        public int Split2IncomingLink2;

        [CsvHeader("スプリット＃２通行権_流入リンク＃３")]
        public int Split2IncomingLink3;

        [CsvHeader("スプリット＃２通行権_流入リンク＃４")]
        public int Split2IncomingLink4;

        [CsvHeader("スプリット＃２通行権_流入リンク＃５")]
        public int Split2IncomingLink5;

        [CsvHeader("スプリット＃２通行権_流入リンク＃６")]
        public int Split2IncomingLink6;

        [CsvHeader("スプリット＃２通行権_流入リンク＃７")]
        public int Split2IncomingLink7;

        [CsvHeader("スプリット＃２通行権_流入リンク＃８")]
        public int Split2IncomingLink8;

        // 2
        [CsvHeader("スプリット＃２通行権_流出リンク＃１")]
        public int Split2OutgoingLink1;

        [CsvHeader("スプリット＃２通行権_流出リンク＃２")]
        public int Split2OutgoingLink2;

        [CsvHeader("スプリット＃２通行権_流出リンク＃３")]
        public int Split2OutgoingLink3;

        [CsvHeader("スプリット＃２通行権_流出リンク＃４")]
        public int Split2OutgoingLink4;

        [CsvHeader("スプリット＃２通行権_流出リンク＃５")]
        public int Split2OutgoingLink5;

        [CsvHeader("スプリット＃２通行権_流出リンク＃６")]
        public int Split2OutgoingLink6;

        [CsvHeader("スプリット＃２通行権_流出リンク＃７")]
        public int Split2OutgoingLink7;

        [CsvHeader("スプリット＃２通行権_流出リンク＃８")]
        public int Split2OutgoingLink8;

        // 3
        [CsvHeader("スプリット＃３通行権_流入リンク＃１")]
        public int Split3IncomingLink1;

        [CsvHeader("スプリット＃３通行権_流入リンク＃２")]
        public int Split3IncomingLink2;

        [CsvHeader("スプリット＃３通行権_流入リンク＃３")]
        public int Split3IncomingLink3;

        [CsvHeader("スプリット＃３通行権_流入リンク＃４")]
        public int Split3IncomingLink4;

        [CsvHeader("スプリット＃３通行権_流入リンク＃５")]
        public int Split3IncomingLink5;

        [CsvHeader("スプリット＃３通行権_流入リンク＃６")]
        public int Split3IncomingLink6;

        [CsvHeader("スプリット＃３通行権_流入リンク＃７")]
        public int Split3IncomingLink7;

        [CsvHeader("スプリット＃３通行権_流入リンク＃８")]
        public int Split3IncomingLink8;

        // 3
        [CsvHeader("スプリット＃３通行権_流出リンク＃１")]
        public int Split3OutgoingLink1;

        [CsvHeader("スプリット＃３通行権_流出リンク＃２")]
        public int Split3OutgoingLink2;

        [CsvHeader("スプリット＃３通行権_流出リンク＃３")]
        public int Split3OutgoingLink3;

        [CsvHeader("スプリット＃３通行権_流出リンク＃４")]
        public int Split3OutgoingLink4;

        [CsvHeader("スプリット＃３通行権_流出リンク＃５")]
        public int Split3OutgoingLink5;

        [CsvHeader("スプリット＃３通行権_流出リンク＃６")]
        public int Split3OutgoingLink6;

        [CsvHeader("スプリット＃３通行権_流出リンク＃７")]
        public int Split3OutgoingLink7;

        [CsvHeader("スプリット＃３通行権_流出リンク＃８")]
        public int Split3OutgoingLink8;

        // 4
        [CsvHeader("スプリット＃４通行権_流入リンク＃１")]
        public int Split4IncomingLink1;

        [CsvHeader("スプリット＃４通行権_流入リンク＃２")]
        public int Split4IncomingLink2;

        [CsvHeader("スプリット＃４通行権_流入リンク＃３")]
        public int Split4IncomingLink3;

        [CsvHeader("スプリット＃４通行権_流入リンク＃４")]
        public int Split4IncomingLink4;

        [CsvHeader("スプリット＃４通行権_流入リンク＃５")]
        public int Split4IncomingLink5;

        [CsvHeader("スプリット＃４通行権_流入リンク＃６")]
        public int Split4IncomingLink6;

        [CsvHeader("スプリット＃４通行権_流入リンク＃７")]
        public int Split4IncomingLink7;

        [CsvHeader("スプリット＃４通行権_流入リンク＃８")]
        public int Split4IncomingLink8;

        // 4
        [CsvHeader("スプリット＃４通行権_流出リンク＃１")]
        public int Split4OutgoingLink1;

        [CsvHeader("スプリット＃４通行権_流出リンク＃２")]
        public int Split4OutgoingLink2;

        [CsvHeader("スプリット＃４通行権_流出リンク＃３")]
        public int Split4OutgoingLink3;

        [CsvHeader("スプリット＃４通行権_流出リンク＃４")]
        public int Split4OutgoingLink4;

        [CsvHeader("スプリット＃４通行権_流出リンク＃５")]
        public int Split4OutgoingLink5;

        [CsvHeader("スプリット＃４通行権_流出リンク＃６")]
        public int Split4OutgoingLink6;

        [CsvHeader("スプリット＃４通行権_流出リンク＃７")]
        public int Split4OutgoingLink7;

        [CsvHeader("スプリット＃４通行権_流出リンク＃８")]
        public int Split4OutgoingLink8;

        // 5
        [CsvHeader("スプリット＃５通行権_流入リンク＃１")]
        public int Split5IncomingLink1;

        [CsvHeader("スプリット＃５通行権_流入リンク＃２")]
        public int Split5IncomingLink2;

        [CsvHeader("スプリット＃５通行権_流入リンク＃３")]
        public int Split5IncomingLink3;

        [CsvHeader("スプリット＃５通行権_流入リンク＃４")]
        public int Split5IncomingLink4;

        [CsvHeader("スプリット＃５通行権_流入リンク＃５")]
        public int Split5IncomingLink5;

        [CsvHeader("スプリット＃５通行権_流入リンク＃６")]
        public int Split5IncomingLink6;

        [CsvHeader("スプリット＃５通行権_流入リンク＃７")]
        public int Split5IncomingLink7;

        [CsvHeader("スプリット＃５通行権_流入リンク＃８")]
        public int Split5IncomingLink8;

        // 5
        [CsvHeader("スプリット＃５通行権_流出リンク＃１")]
        public int Split5OutgoingLink1;

        [CsvHeader("スプリット＃５通行権_流出リンク＃２")]
        public int Split5OutgoingLink2;

        [CsvHeader("スプリット＃５通行権_流出リンク＃３")]
        public int Split5OutgoingLink3;

        [CsvHeader("スプリット＃５通行権_流出リンク＃４")]
        public int Split5OutgoingLink4;

        [CsvHeader("スプリット＃５通行権_流出リンク＃５")]
        public int Split5OutgoingLink5;

        [CsvHeader("スプリット＃５通行権_流出リンク＃６")]
        public int Split5OutgoingLink6;

        [CsvHeader("スプリット＃５通行権_流出リンク＃７")]
        public int Split5OutgoingLink7;

        [CsvHeader("スプリット＃５通行権_流出リンク＃８")]
        public int Split5OutgoingLink8;

        // 6
        [CsvHeader("スプリット＃６通行権_流入リンク＃１")]
        public int Split6IncomingLink1;

        [CsvHeader("スプリット＃６通行権_流入リンク＃２")]
        public int Split6IncomingLink2;

        [CsvHeader("スプリット＃６通行権_流入リンク＃３")]
        public int Split6IncomingLink3;

        [CsvHeader("スプリット＃６通行権_流入リンク＃４")]
        public int Split6IncomingLink4;

        [CsvHeader("スプリット＃６通行権_流入リンク＃５")]
        public int Split6IncomingLink5;

        [CsvHeader("スプリット＃６通行権_流入リンク＃６")]
        public int Split6IncomingLink6;

        [CsvHeader("スプリット＃６通行権_流入リンク＃７")]
        public int Split6IncomingLink7;

        [CsvHeader("スプリット＃６通行権_流入リンク＃８")]
        public int Split6IncomingLink8;

        // 6
        [CsvHeader("スプリット＃６通行権_流出リンク＃１")]
        public int Split6OutgoingLink1;

        [CsvHeader("スプリット＃６通行権_流出リンク＃２")]
        public int Split6OutgoingLink2;

        [CsvHeader("スプリット＃６通行権_流出リンク＃３")]
        public int Split6OutgoingLink3;

        [CsvHeader("スプリット＃６通行権_流出リンク＃４")]
        public int Split6OutgoingLink4;

        [CsvHeader("スプリット＃６通行権_流出リンク＃５")]
        public int Split6OutgoingLink5;

        [CsvHeader("スプリット＃６通行権_流出リンク＃６")]
        public int Split6OutgoingLink6;

        [CsvHeader("スプリット＃６通行権_流出リンク＃７")]
        public int Split6OutgoingLink7;

        [CsvHeader("スプリット＃６通行権_流出リンク＃８")]
        public int Split6OutgoingLink8;

        [CsvHeader("リンクバージョン")]
        public string LinkVersion;
    }
}