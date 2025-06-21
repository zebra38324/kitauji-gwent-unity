using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class CompetitionContextModelTest
{
    private const string testName = "TestTeam";
    private const CompetitionBase.Level testStartLevel = CompetitionBase.Level.Kansai;
    private CompetitionContextModel context;

    [SetUp]
    public void Setup()
    {
        context = new CompetitionContextModel(testName, testStartLevel);
    }

    [Test]
    public void Constructor()
    {
        // Assert
        Assert.AreEqual(testName, context.playerName);
        Assert.AreEqual(testStartLevel, context.currnetLevel);
        Assert.AreEqual(CompetitionContextModel.TEAM_NUM, context.teamDict.Count);
        foreach (var team in context.teamDict.Values) {
            Assert.AreEqual(CompetitionContextModel.TEAM_NUM - 1, team.gameList.Count);
            Assert.AreEqual(CompetitionContextModel.TEAM_NUM - 1, team.gameList.Distinct().Count());
            Assert.AreEqual(null, team.gameList.Find(x => x.selfName != team.name));
            Assert.AreEqual(null, team.gameList.Find(x => x.enemyName == team.name));
        }
        for (int round = 0; round < CompetitionContextModel.TEAM_NUM - 1; round++) {
            foreach (var team in context.teamDict.Values) {
                Assert.AreEqual(team.name, context.teamDict[team.gameList[round].enemyName].gameList[round].enemyName);
            }
        }
    }

    [Test]
    public void Constructor_Record()
    {
        // Arrange  
        var record = new CompetitionBase.ContextRecord();
        record.playerName = testName;
        record.currnetLevel = testStartLevel;
        var teamRecords = new List<CompetitionBase.TeamInfoRecord>();
        foreach (var team in context.teamDict.Values) {
            teamRecords.Add(team.GetRecord());
        }
        record.teamRecords = teamRecords.ToArray();
        var contextFromRecord = new CompetitionContextModel(record);

        // Assert  
        Assert.AreEqual(testName, contextFromRecord.playerName);
        Assert.AreEqual(testStartLevel, contextFromRecord.currnetLevel);
        Assert.AreEqual(CompetitionContextModel.TEAM_NUM, contextFromRecord.teamDict.Count);
        foreach (var team in contextFromRecord.teamDict.Values) {
            Assert.AreEqual(CompetitionContextModel.TEAM_NUM - 1, team.gameList.Count);
            Assert.AreEqual(CompetitionContextModel.TEAM_NUM - 1, team.gameList.Distinct().Count());
            Assert.AreEqual(null, team.gameList.Find(x => x.selfName != team.name));
            Assert.AreEqual(null, team.gameList.Find(x => x.enemyName == team.name));
        }
        for (int round = 0; round < CompetitionContextModel.TEAM_NUM - 1; round++) {
            foreach (var team in contextFromRecord.teamDict.Values) {
                Assert.AreEqual(team.name, contextFromRecord.teamDict[team.gameList[round].enemyName].gameList[round].enemyName);
            }
        }
    }

    [Test]
    public void GetRecord()
    {
        for (int round = 0; round < CompetitionContextModel.TEAM_NUM - 1; round++) {
            foreach (var team in context.teamDict.Values) {
                if (team.gameList[round].hasFinished) {
                    continue;
                }
                context.FinishGame(team.name, 5, team.gameList[round].enemyName, 3, true);
            }
        }
        context.FinishCurrentLevel();
        var record = context.GetRecord();
        Assert.AreEqual(testName, record.playerName);
        Assert.AreEqual(testStartLevel, record.currnetLevel);
        Assert.AreEqual(CompetitionContextModel.TEAM_NUM, record.teamRecords.Length);
        foreach (var team in context.teamDict.Values) {
            var teamRecord = record.teamRecords.FirstOrDefault(x => x.name == team.name);
            Assert.IsNotNull(teamRecord);
            Assert.AreEqual(team.currentRound, teamRecord.currentRound);
            Assert.AreEqual(team.gameList.Count, teamRecord.gameList.Length);
            Assert.AreEqual(team.prize, teamRecord.prize);
        }
    }

    [Test]
    public void FinishGame()
    {
        foreach (var team in context.teamDict.Values) {
            if (team.gameList[0].hasFinished) {
                continue;
            }
            context.FinishGame(team.name, 5, context.teamDict[team.name].gameList[0].enemyName, 3, true);
        }
        foreach (var team in context.teamDict.Values) {
            var enemyTeam = context.teamDict[team.gameList[0].enemyName];
            Assert.AreEqual(true, team.gameList[0].hasFinished);
            Assert.AreEqual(1, team.GetWinCount() + team.GetLoseCount());
            Assert.AreEqual(1, team.currentRound);
            Assert.IsTrue(team.gameList[0].selfWin ^ enemyTeam.gameList[0].selfWin);
        }
    }

    [Test]
    public void GetRankTeamList()
    {
        for (int i = 0; i < CompetitionContextModel.TEAM_NUM - 1; i++) {
            foreach (var team in context.teamDict.Values) {
                if (team.gameList[i].hasFinished) {
                    continue;
                }
                var ran = new System.Random();
                bool aWin = ran.Next(0, 2) == 0;
                int aScore = aWin ? ran.Next(2, 4) : 2;
                int bScore = aWin ? 2 : ran.Next(2, 4);
                context.FinishGame(team.name, aScore, context.teamDict[team.name].gameList[i].enemyName, bScore, aWin);
            }
        }
        var rankList = context.GetRankTeamList();
        Assert.AreEqual(CompetitionContextModel.TEAM_NUM, rankList.Count);
        for (int i = 0; i < rankList.Count - 1; i++) {
            Assert.GreaterOrEqual(rankList[i].GetWinCount(), rankList[i + 1].GetWinCount());
            if (rankList[i].GetWinCount() == rankList[i + 1].GetWinCount()) {
                Assert.GreaterOrEqual(rankList[i].GetMinorScoreDiff(), rankList[i + 1].GetMinorScoreDiff());
            }
        }
    }

    // 测试各种特殊情况的排序
    [Test]
    public void GetRankTeamList_Special()
    {
        var teamList = new List<CompetitionTeamInfoModel> {
            new CompetitionTeamInfoModel("A"),
            new CompetitionTeamInfoModel("B"),
            new CompetitionTeamInfoModel("C"),
            new CompetitionTeamInfoModel("TestTeam"),
        };
        // A全胜，B 3:1 C，C(W) 2:2 T，T 2:1 B
        // A 7-1
        // T 4-6 4-3
        // B 4-6 4-3
        // C 4-6 3-5
        teamList[0].InitGameList(new List<string> { "B", "C", "TestTeam" });
        teamList[1].InitGameList(new List<string> { "A", "TestTeam", "C" });
        teamList[2].InitGameList(new List<string> { "TestTeam", "A", "B" });
        teamList[3].InitGameList(new List<string> { "C", "B", "A" });
        teamList[0].FinishGame(3, 0, true); // A B
        teamList[0].FinishGame(1, 1, true); // A C
        teamList[0].FinishGame(3, 0, true); // A T
        teamList[1].FinishGame(0, 3, false); // B A
        teamList[1].FinishGame(1, 2, false); // B T
        teamList[1].FinishGame(3, 1, true); // B C
        teamList[2].FinishGame(2, 2, true); // C T
        teamList[2].FinishGame(1, 1, false); // C A
        teamList[2].FinishGame(1, 3, false); // C B
        teamList[3].FinishGame(2, 2, false); // T C
        teamList[3].FinishGame(2, 1, true); // T B
        teamList[3].FinishGame(0, 3, false); // T A

        MethodInfo privateMethod = typeof(CompetitionContextModel)
            .GetMethod("SortRankTeamList", BindingFlags.NonPublic | BindingFlags.Instance);
        var sortedList = (List<CompetitionTeamInfoModel>)privateMethod.Invoke(context, new object[] { teamList });
        Assert.AreEqual(4, sortedList.Count);
        Assert.AreEqual("A", sortedList[0].name);
        Assert.AreEqual("TestTeam", sortedList[1].name);
        Assert.AreEqual("B", sortedList[2].name);
        Assert.AreEqual("C", sortedList[3].name);
    }

    [Test]
    public void GetGameInfoList()
    {
        foreach (var team in context.teamDict.Values) {
            if (team.gameList[0].hasFinished) {
                continue;
            }
            context.FinishGame(team.name, 5, context.teamDict[team.name].gameList[0].enemyName, 3, true);
        }
        var round0 = context.GetGameInfoList(0);
        var round1 = context.GetGameInfoList(1);
        Assert.AreEqual(5, round0.Count);
        Assert.AreEqual(5, round0.Count);
    }

    [Test]
    public void IsCurrentLevelFinish()
    {
        Assert.IsFalse(context.IsCurrentLevelFinish());
        for (int round = 0; round < CompetitionContextModel.TEAM_NUM - 1; round++) {
            foreach (var team in context.teamDict.Values) {
                if (team.gameList[round].hasFinished) {
                    continue;
                }
                context.FinishGame(team.name, 5, team.gameList[round].enemyName, 3, true);
            }
        }
        Assert.IsTrue(context.IsCurrentLevelFinish());
    }

    [Test]
    public void FinishCurrentLevel()
    {
        for (int round = 0; round < CompetitionContextModel.TEAM_NUM - 1; round++) {
            foreach (var team in context.teamDict.Values) {
                if (team.gameList[round].hasFinished) {
                    continue;
                }
                context.FinishGame(team.name, 5, team.gameList[round].enemyName, 3, true);
            }
        }
        context.FinishCurrentLevel();
        var rankList = context.GetRankTeamList();
        for (int i = 0; i < rankList.Count; i++) {
            if (i <= 1) {
                rankList[i].prize = CompetitionBase.Prize.GoldPromote; // 前两名晋级
            } else if (i <= 3) {
                rankList[i].prize = CompetitionBase.Prize.Gold; // 金奖
            } else if (i <= 6) {
                rankList[i].prize = CompetitionBase.Prize.Silver; // 银奖
            } else {
                rankList[i].prize = CompetitionBase.Prize.Bronze; // 铜奖
            }
        }
    }

    [Test]
    public void StartNextLevel()
    {
        for (int round = 0; round < CompetitionContextModel.TEAM_NUM - 1; round++) {
            foreach (var team in context.teamDict.Values) {
                if (team.gameList[round].hasFinished) {
                    continue;
                }
                context.FinishGame(team.name, 5, team.gameList[round].enemyName, 3, true);
            }
        }
        context.FinishCurrentLevel();
        context.StartNextLevel();
        Assert.AreEqual(CompetitionBase.Level.National, context.currnetLevel);
        Assert.AreEqual(CompetitionContextModel.TEAM_NUM, context.teamDict.Count);
        foreach (var team in context.teamDict.Values) {
            Assert.AreEqual(0, team.currentRound);
            Assert.AreEqual(CompetitionContextModel.TEAM_NUM - 1, team.gameList.Count);
            Assert.AreEqual(CompetitionContextModel.TEAM_NUM - 1, team.gameList.Distinct().Count());
            Assert.AreEqual(null, team.gameList.Find(x => x.selfName != team.name));
            Assert.AreEqual(null, team.gameList.Find(x => x.enemyName == team.name));
        }
    }
}