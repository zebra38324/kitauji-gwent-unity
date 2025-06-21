using System.Collections;
using System.Collections.Generic;

// 每个队伍的信息
public class CompetitionTeamInfoModel
{
    private string TAG = "CompetitionTeamInfoModel";

    public string name { get; private set; } // 队伍名称

    public int currentRound { get; private set; } = 0; // 当前轮次，从0开始，作为ui显示时注意+1，大于等于gameList.Count时表示比赛结束

    public List<CompetitionGameInfoModel> gameList { get; private set; } = new List<CompetitionGameInfoModel>(); // 比赛列表

    public CompetitionBase.Prize prize;

    public CompetitionTeamInfoModel(string name)
    {
        this.name = name;
    }

    public CompetitionTeamInfoModel(CompetitionBase.TeamInfoRecord record)
    {
        this.name = record.name;
        this.currentRound = record.currentRound;
        this.gameList = new List<CompetitionGameInfoModel>(record.gameList);
        this.prize = record.prize;
    }

    public CompetitionBase.TeamInfoRecord GetRecord()
    {
        var record = new CompetitionBase.TeamInfoRecord();
        record.name = name;
        record.currentRound = currentRound;
        record.gameList = gameList.ToArray();
        record.prize = prize;
        return record;
    }

    public void InitGameList(List<string> enemyNameList)
    {
        foreach (var enemyName in enemyNameList) {
            gameList.Add(new CompetitionGameInfoModel(name, enemyName));
        }
    }

    public void FinishGame(int selfScore, int enemyScore, bool selfWin)
    {
        if (currentRound >= gameList.Count) {
            KLog.E(TAG, $"FinishGame: currentRound out of range = {currentRound}, team count = {gameList.Count}");
            return;
        }
        gameList[currentRound].FinishGame(selfScore, enemyScore, selfWin);
        currentRound++;
    }

    public int GetWinCount()
    {
        int winCount = 0;
        foreach (var game in gameList) {
            if (game.hasFinished && game.selfWin) {
                winCount++;
            }
        }
        return winCount;
    }

    public int GetLoseCount()
    {
        int loseCount = 0;
        foreach (var game in gameList) {
            if (game.hasFinished && !game.selfWin) {
                loseCount++;
            }
        }
        return loseCount;
    }

    // 获取小分差
    public int GetMinorScoreDiff()
    {
        int diff = 0;
        foreach (var game in gameList) {
            if (game.hasFinished) {
                diff += game.selfScore - game.enemyScore;
            }
        }
        return diff;
    }
}
