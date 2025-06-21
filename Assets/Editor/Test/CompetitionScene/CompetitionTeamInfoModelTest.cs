using NUnit.Framework;
using System.Collections.Generic;

public class CompetitionTeamInfoModelTest
{
    private const string TeamName = "TestTeam";
    private CompetitionTeamInfoModel _teamModel;

    [SetUp]
    public void Setup()
    {
        _teamModel = new CompetitionTeamInfoModel(TeamName);
    }

    [Test]
    public void Constructor_ShouldInitializeProperties()
    {
        // Assert
        Assert.AreEqual(TeamName, _teamModel.name);
        Assert.AreEqual(0, _teamModel.currentRound);
        Assert.IsNotNull(_teamModel.gameList);
        Assert.AreEqual(0, _teamModel.gameList.Count);
    }

    [Test]
    public void Constructor_Record()
    {
        CompetitionBase.TeamInfoRecord record = new CompetitionBase.TeamInfoRecord();
        record.name = "test";
        record.currentRound = 1;
        record.gameList = new CompetitionGameInfoModel[] {
            new CompetitionGameInfoModel("test", "enemy1"),
            new CompetitionGameInfoModel("test", "enemy2")
        };
        record.gameList[0].FinishGame(5, 3, true);
        record.prize = CompetitionBase.Prize.Gold;

        var model = new CompetitionTeamInfoModel(record);

        // Assert
        Assert.AreEqual(record.name, model.name);
        Assert.AreEqual(record.currentRound, model.currentRound);
        Assert.AreEqual(record.gameList.Length, model.gameList.Count);
        Assert.AreEqual(record.prize, model.prize);
    }

    [Test]
    public void GetRecord()
    {
        _teamModel.InitGameList(new List<string> { "Enemy1", "Enemy2", "Enemy3" });
        _teamModel.FinishGame(5, 3, true);

        var record = _teamModel.GetRecord();

        Assert.AreEqual(_teamModel.name, record.name);
        Assert.AreEqual(_teamModel.currentRound, record.currentRound);
        Assert.AreEqual(_teamModel.gameList.Count, record.gameList.Length);
        Assert.AreEqual(_teamModel.prize, record.prize);
    }

    [Test]
    public void InitGameList_WithEnemies_ShouldCreateCorrectGameModels()
    {
        // Arrange
        var enemies = new List<string> { "Enemy1", "Enemy2", "Enemy3" };

        // Act
        _teamModel.InitGameList(enemies);

        // Assert
        Assert.AreEqual(3, _teamModel.gameList.Count);
        for (int i = 0; i < enemies.Count; i++) {
            Assert.AreEqual(TeamName, _teamModel.gameList[i].selfName);
            Assert.AreEqual(enemies[i], _teamModel.gameList[i].enemyName);
            Assert.IsFalse(_teamModel.gameList[i].hasFinished);
        }
    }

    [Test]
    public void FinishGame_ShouldUpdateCurrentGameAndIncrementRound()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "Enemy1", "Enemy2" });
        int initialRound = _teamModel.currentRound;

        // Act
        _teamModel.FinishGame(5, 3, true);

        // Assert
        Assert.AreEqual(initialRound + 1, _teamModel.currentRound);
        var finishedGame = _teamModel.gameList[0];
        Assert.IsTrue(finishedGame.hasFinished);
        Assert.AreEqual(5, finishedGame.selfScore);
        Assert.AreEqual(3, finishedGame.enemyScore);
        Assert.IsTrue(finishedGame.selfWin);
    }

    [Test]
    public void GetWinCount_ShouldReturnCorrectNumber()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "Enemy1", "Enemy2", "Enemy3" });

        // Win first game
        _teamModel.FinishGame(5, 3, true);

        // Lose second game
        _teamModel.FinishGame(2, 4, false);

        // Win third game
        _teamModel.FinishGame(6, 5, true);

        // Act
        int winCount = _teamModel.GetWinCount();

        // Assert
        Assert.AreEqual(2, winCount);
    }

    [Test]
    public void GetLoseCount_ShouldReturnCorrectNumber()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "Enemy1", "Enemy2", "Enemy3" });

        // Win first game
        _teamModel.FinishGame(5, 3, true);

        // Lose second game
        _teamModel.FinishGame(2, 4, false);

        // Lose third game
        _teamModel.FinishGame(3, 5, false);

        // Act
        int loseCount = _teamModel.GetLoseCount();

        // Assert
        Assert.AreEqual(2, loseCount);
    }

    [Test]
    public void GetMinorScoreDiff_ShouldCalculateCorrectDifference()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "Enemy1", "Enemy2", "Enemy3" });

        // +2 difference
        _teamModel.FinishGame(5, 3, true);

        // -2 difference
        _teamModel.FinishGame(2, 4, false);

        // +1 difference
        _teamModel.FinishGame(6, 5, true);

        // Act
        int scoreDiff = _teamModel.GetMinorScoreDiff();

        // Assert
        Assert.AreEqual(1, scoreDiff); // (5-3)+(2-4)+(6-5) = 2 - 2 + 1 = 1
    }

    [Test]
    public void GetMinorScoreDiff_WithUnfinishedGames_ShouldOnlyCalculateFinishedOnes()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "Enemy1", "Enemy2", "Enemy3" });

        // Win first game (+3)
        _teamModel.FinishGame(5, 2, true);

        // Don't finish second game

        // Lose third game (-2)
        _teamModel.FinishGame(1, 3, false);

        // Act
        int scoreDiff = _teamModel.GetMinorScoreDiff();

        // Assert
        Assert.AreEqual(1, scoreDiff); // (5-2) + (1-3) = 3 - 2 = 1
    }

    [Test]
    public void FinishGame_ShouldNotAffectPreviousOrLaterGames()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "A", "B", "C" });

        // Act
        _teamModel.FinishGame(10, 0, true); // First game

        // Assert
        var gameA = _teamModel.gameList[0];
        var gameB = _teamModel.gameList[1];
        var gameC = _teamModel.gameList[2];

        Assert.IsTrue(gameA.hasFinished);
        Assert.AreEqual(10, gameA.selfScore);
        Assert.AreEqual(0, gameA.enemyScore);

        Assert.IsFalse(gameB.hasFinished);
        Assert.AreEqual(0, gameB.selfScore);
        Assert.AreEqual(0, gameB.enemyScore);

        Assert.IsFalse(gameC.hasFinished);
        Assert.AreEqual(0, gameC.selfScore);
        Assert.AreEqual(0, gameC.enemyScore);
    }

    [Test]
    public void GetWinCount_WithNoGamesFinished_ShouldReturnZero()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "A", "B" });

        // Act
        int winCount = _teamModel.GetWinCount();

        // Assert
        Assert.AreEqual(0, winCount);
    }

    [Test]
    public void GetLoseCount_WithNoGamesFinished_ShouldReturnZero()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "A", "B" });

        // Act
        int loseCount = _teamModel.GetLoseCount();

        // Assert
        Assert.AreEqual(0, loseCount);
    }

    [Test]
    public void GetMinorScoreDiff_WithNoGamesFinished_ShouldReturnZero()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "A", "B" });

        // Act
        int diff = _teamModel.GetMinorScoreDiff();

        // Assert
        Assert.AreEqual(0, diff);
    }

    [Test]
    public void FinishGame_WithTieResult_ShouldUpdateCorrectly()
    {
        // Arrange
        _teamModel.InitGameList(new List<string> { "A" });

        // Act
        _teamModel.FinishGame(3, 3, false); // Tie game, selfWin = false

        // Assert
        var game = _teamModel.gameList[0];
        Assert.IsTrue(game.hasFinished);
        Assert.AreEqual(3, game.selfScore);
        Assert.AreEqual(3, game.enemyScore);
        Assert.IsFalse(game.selfWin);
    }
}