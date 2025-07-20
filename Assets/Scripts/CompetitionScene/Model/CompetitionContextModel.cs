using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CompetitionContextModel
{
    private string TAG = "CompetitionContextModel";

    public static readonly int TEAM_NUM = 10;

    public string playerName { get; private set; } // 玩家名称

    public CompetitionBase.Level currnetLevel { get; private set; } = CompetitionBase.Level.KyotoPrefecture;

    public Dictionary<string, CompetitionTeamInfoModel> teamDict { get; private set; }

    // 府赛对应L1，关西赛对应L2，全国赛对应L3
    // 由于有个ai队伍一起晋级，所以关西赛与全国赛的队伍默认数量是8个
    private static string[][] AI_TEAM_NAME_LIST = new string[][]
    {
        new string[] {
            "龙圣学园高中", // 金奖，进全国赛
            "立华高中", // 金奖，进关西赛
            "洛秋高中", // 金奖，进关西赛
            "南阳高中", // 金奖
            "山城高中", // 银奖
            "鸟羽高中", // 银奖
            "文教高中", // 银奖
            "堀川高中", // 铜奖
            "西舞鹤高中", // 铜奖
        }, // 京都府赛 
        new string[] {
            "大阪东照高中", // 金奖，进全国赛
            "明静工科高中", // 金奖，进全国赛
            "秀塔大学附属高中", // 金奖，进全国赛
            "兵库县立三木山高中", // 金奖
            "花咲女子高中", // 银奖
            "光川高中", // 银奖
            "京都市立西院中学", // 银奖
            "志化合高中", // 铜奖
        }, // 关西赛
        new string[] {
            "坂江高中", // 关东一霸
            "西条女子高中", // 连续三年打进全国赛
            "清良女子高中",
            "玉名女子高中", // 金奖
            "片敷高中", // 银奖
            "成合高中", // 银奖
            "浜松圣星高中", // 银奖
            "福岛县立磐城高中", // 铜奖
        } // 全国赛
    };

    private List<string> currentAITeamNameList;

    public CompetitionContextModel(string playerName)
    {
        this.playerName = playerName;
        currnetLevel = CompetitionBase.Level.KyotoPrefecture;
        // 此处只有京都府赛会走到
        currentAITeamNameList = new List<string>(AI_TEAM_NAME_LIST[(int)currnetLevel]);
        Init();
    }

    public CompetitionContextModel(CompetitionBase.ContextRecord record)
    {
        this.playerName = record.playerName;
        this.currnetLevel = record.currnetLevel;
        teamDict = new Dictionary<string, CompetitionTeamInfoModel>();
        currentAITeamNameList = new List<string>();
        foreach (var teamRecord in record.teamRecords) {
            teamDict[teamRecord.name] = new CompetitionTeamInfoModel(teamRecord);
            if (teamRecord.name != playerName) {
                currentAITeamNameList.Add(teamRecord.name);
            }
        }
    }

    public CompetitionBase.ContextRecord GetRecord()
    {
        var record = new CompetitionBase.ContextRecord();
        record.playerName = playerName;
        record.currnetLevel = currnetLevel;
        record.teamRecords = new CompetitionBase.TeamInfoRecord[teamDict.Count];
        int index = 0;
        foreach (var team in teamDict.Values) {
            record.teamRecords[index++] = team.GetRecord();
        }
        return record;
    }

    // 按排名获取队伍列表
    public List<CompetitionTeamInfoModel> GetRankTeamList()
    {
        var result = new List<CompetitionTeamInfoModel>(teamDict.Values);
        return SortRankTeamList(result);
    }

    public List<CompetitionGameInfoModel> GetGameInfoList(int round)
    {
        var result = new List<CompetitionGameInfoModel>();
        foreach (var team in teamDict.Values) {
            if (result.Find(x => x.selfName == team.name || x.enemyName == team.name) != null) {
                continue; // 避免重复添加同一队伍的比赛信息
            }
            result.Add(team.gameList[round]);
        }
        return result;
    }

    public void FinishGame(string xName, int xScore, string yName, int yScore, bool xWin)
    {
        if (teamDict[xName].gameList[teamDict[xName].currentRound].enemyName != yName ||
            teamDict[yName].gameList[teamDict[yName].currentRound].enemyName != xName) {
            KLog.E(TAG, $"FinishGame: {xName} vs {yName}, expect {xName} vs {teamDict[xName].gameList[teamDict[xName].currentRound].enemyName} and " +
                $"{yName} vs {teamDict[yName].gameList[teamDict[yName].currentRound].enemyName}");
            return;
        }
        teamDict[xName].FinishGame(xScore, yScore, xWin);
        teamDict[yName].FinishGame(yScore, xScore, !xWin);
    }

    // 当前级别比赛是否已结束
    public bool IsCurrentLevelFinish()
    {
        foreach (var team in teamDict.Values) {
            if (team.currentRound < team.gameList.Count) {
                return false; // 只要有一个队伍还未完成所有比赛，就表示比赛未结束
            }
        }
        return true;
    }

    // 结算当前级别比赛，不包括升级逻辑
    public void FinishCurrentLevel()
    {
        if (!IsCurrentLevelFinish()) {
            KLog.E(TAG, "FinishCurrentLevel: not finish");
            return;
        }
        KLog.I(TAG, $"FinishCurrentLevel: currnetLevel = {currnetLevel}");
        var teamList = GetRankTeamList();
        for (int i = 0; i < teamList.Count; i++) {
            teamList[i].prize = CompetitionBase.GetPrize(i, currnetLevel);
        }
    }

    // 开始下一阶段比赛
    public void StartNextLevel()
    {
        if (!IsCurrentLevelFinish()) {
            KLog.E(TAG, "StartNextLevel: current not finish");
            return;
        }
        if (currnetLevel == CompetitionBase.Level.National) {
            KLog.E(TAG, "StartNextLevel: already National");
            return;
        }
        string promoteAITeam = ""; // 另一个晋级的ai队伍
        foreach (var team in teamDict.Values) {
            if (team.name == playerName && team.prize != CompetitionBase.Prize.GoldPromote) {
                KLog.W(TAG, "StartNextLevel: player not promote");
                return;
            }
            if (team.name != playerName && team.prize == CompetitionBase.Prize.GoldPromote) {
                promoteAITeam = team.name;
            }
        }
        currnetLevel += 1;
        KLog.I(TAG, $"StartNextLevel: new level = {currnetLevel}");
        currentAITeamNameList = new List<string>(AI_TEAM_NAME_LIST[(int)currnetLevel]) {
            promoteAITeam
        };
        Init();
    }

    public void Restart(bool fromKyotoPrefecture)
    {
        KLog.I(TAG, $"Restart: fromKyotoPrefecture = {fromKyotoPrefecture}");
        if (fromKyotoPrefecture) {
            currnetLevel = CompetitionBase.Level.KyotoPrefecture;
            currentAITeamNameList = new List<string>(AI_TEAM_NAME_LIST[(int)currnetLevel]);
        }
        Init();
    }

    private void Init()
    {
        KLog.I(TAG, $"Init: currnetLevel = {currnetLevel}");
        teamDict = new Dictionary<string, CompetitionTeamInfoModel>();
        teamDict[playerName] = new CompetitionTeamInfoModel(playerName);
        foreach (var teamName in currentAITeamNameList) {
            teamDict[teamName] = new CompetitionTeamInfoModel(teamName);
        }
        InitGameSchedule();
    }

    // 初始化赛程
    private void InitGameSchedule()
    {
        List<string> allTeams = new List<string>(currentAITeamNameList) {
            playerName
        };
        var shuffledTeams = allTeams.OrderBy(x => Guid.NewGuid()).ToList();
        List<List<string>> gameSchedule = new List<List<string>>();
        for (int i = 0; i < TEAM_NUM; i++) {
            gameSchedule.Add(new List<string>());
        }
        for (int round = 0; round < TEAM_NUM - 1; round++) {
            for (int match = 0; match < TEAM_NUM / 2; match++) {
                int aIndex = match == 0 ? 0 : (match + round - 1) % (TEAM_NUM - 1) + 1;
                int bIndex = (TEAM_NUM - 1 - match + round - 1) % (TEAM_NUM - 1) + 1;
                gameSchedule[aIndex].Add(shuffledTeams[bIndex]);
                gameSchedule[bIndex].Add(shuffledTeams[aIndex]);
            }
        }
        for (int i = 0; i < gameSchedule.Count; i++) {
            string teamName = shuffledTeams[i];
            teamDict[teamName].InitGameList(gameSchedule[i]);
        }
    }

    private List<CompetitionTeamInfoModel> SortRankTeamList(List<CompetitionTeamInfoModel> teamList)
    {
        var final = new List<CompetitionTeamInfoModel>();
        teamList.Sort((x, y) => {
            int xWinCount = x.GetWinCount();
            int yWinCount = y.GetWinCount();
            int xMinorScoreDiff = x.GetMinorScoreDiff();
            int yMinorScoreDiff = y.GetMinorScoreDiff();
            if (xWinCount != yWinCount) {
                return yWinCount.CompareTo(xWinCount); // 按胜场数降序
            } else {
                return yMinorScoreDiff.CompareTo(xMinorScoreDiff); // 按小分差降序
            }
        });

        // 调整同分的队伍
        for (int i = 0; i < teamList.Count; i++) {
            var tieTeams = FindTieTeams(teamList, i);
            if (tieTeams.Count > 1) {
                tieTeams = BreakTieTeams(tieTeams);
                final.AddRange(tieTeams);
                i += tieTeams.Count - 1; // 跳过已处理的队伍
            } else {
                final.Add(tieTeams[0]);
            }
        }
        return final;
    }

    // 寻找同胜场、同小分的队伍
    private List<CompetitionTeamInfoModel> FindTieTeams(List<CompetitionTeamInfoModel> teamList, int startIndex)
    {
        var tieTeams = new List<CompetitionTeamInfoModel>();
        var referTeam = teamList[startIndex];
        for (int i = startIndex; i < teamList.Count; i++) {
            var team = teamList[i];
            if (team.GetWinCount() == referTeam.GetWinCount() &&
                team.GetMinorScoreDiff() == referTeam.GetMinorScoreDiff()) {
                tieTeams.Add(team);
            } else {
                break;
            }
        }
        return tieTeams;
    }

    // 处理平分的队伍
    private List<CompetitionTeamInfoModel> BreakTieTeams(List<CompetitionTeamInfoModel> tieTeams)
    {
        Dictionary<string, int> winCountDic = new Dictionary<string, int>();
        Dictionary<string, int> minorScoreDiffDic = new Dictionary<string, int>();
        foreach (var team in tieTeams) {
            winCountDic[team.name] = 0;
            minorScoreDiffDic[team.name] = 0;
            foreach (var enemyTeam in tieTeams) {
                if (team.name == enemyTeam.name) {
                    continue;
                }
                var game = team.gameList.Find(x => x.enemyName == enemyTeam.name);
                winCountDic[team.name] += game.selfWin ? 1 : 0;
                minorScoreDiffDic[team.name] += game.selfScore - game.enemyScore;
            }
        }

        tieTeams.Sort((x, y) => {
            // 优先按在组内的胜场数降序
            int winCompare = winCountDic[y.name].CompareTo(winCountDic[x.name]);
            if (winCompare != 0) {
                return winCompare;
            }

            // 其次按对组内对手的净胜分降序
            int diffCompare = minorScoreDiffDic[y.name].CompareTo(minorScoreDiffDic[x.name]);
            if (diffCompare != 0) {
                return diffCompare;
            }

            // 玩家排名靠前
            if (x.name == playerName) {
                return -1;
            } else if (y.name == playerName) {
                return 1;
            }

            // 最后按名称排序
            return x.name.CompareTo(y.name);
        });
        return tieTeams;
    }
}
