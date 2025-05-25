using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;

public class GameStateTest
{
    [Test]
    public void InitialState_Is_WaitBackupInfo_And_ActionNone()
    {
        var state = new GameState(true);
        Assert.AreEqual(GameState.State.WAIT_BACKUP_INFO, state.curState);
        Assert.AreEqual(GameState.ActionState.None, state.actionState);
    }

    [Test]
    public void TransState_ValidTransition_ChangesState_And_UpdatesTimestamp()
    {
        var state = new GameState(true);
        var beforeTs = state.stateChangeTs;
        var result = state.TransState(GameState.State.WAIT_INIT_HAND_CARD);
        Assert.AreEqual(GameState.State.WAIT_INIT_HAND_CARD, result.curState);
        Assert.GreaterOrEqual(result.stateChangeTs, beforeTs);
    }

    [Test]
    public void TransState_SameState_ReturnsOriginal()
    {
        var state = new GameState(true);
        var result = state.TransState(GameState.State.WAIT_BACKUP_INFO);
        Assert.AreSame(state, result);
    }

    [Test]
    public void TransState_WithActionState_NotNone_DoesNotChangeState()
    {
        var state = new GameState(true).TransState(GameState.State.WAIT_INIT_HAND_CARD)
            .TransState(GameState.State.WAIT_START)
            .TransState(GameState.State.WAIT_SELF_ACTION);
        var result = state.TransActionState(GameState.ActionState.ATTACKING, new CardModel(new CardInfo()));
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, result.curState);
    }

    [Test]
    public void Pass_SetsSelfPassAndTransitions()
    {
        var state = new GameState(true).TransState(GameState.State.WAIT_INIT_HAND_CARD)
            .TransState(GameState.State.WAIT_START)
            .TransState(GameState.State.WAIT_SELF_ACTION);
        var result = state.Pass(true);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, result.curState);
        var newGameState = result.TransState(GameState.State.WAIT_SELF_ACTION);
        Assert.AreEqual(GameState.State.WAIT_ENEMY_ACTION, newGameState.curState);
        Assert.AreNotSame(result, newGameState);
    }

    [Test]
    public void Pass_SetsEnemyPassAndTransitions()
    {
        var state = new GameState(true).TransState(GameState.State.WAIT_INIT_HAND_CARD)
            .TransState(GameState.State.WAIT_START)
            .TransState(GameState.State.WAIT_ENEMY_ACTION);
        var result = state.Pass(false);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, result.curState);
        var newGameState = result.TransState(GameState.State.WAIT_ENEMY_ACTION);
        Assert.AreEqual(GameState.State.WAIT_SELF_ACTION, newGameState.curState);
        Assert.AreNotSame(result, newGameState);
    }

    [Test]
    public void Pass_BothPasses_LeadsTo_SetFinish()
    {
        var state = new GameState(true).TransState(GameState.State.WAIT_INIT_HAND_CARD)
            .TransState(GameState.State.WAIT_START)
            .TransState(GameState.State.WAIT_SELF_ACTION);
        state = state.Pass(true);
        state = state.Pass(false);
        Assert.AreEqual(GameState.State.SET_FINFISH, state.curState);
    }

    [Test]
    public void TransActionState_ValidTransition_ChangesActionState_And_SetsCard()
    {
        var card = new CardModel(new CardInfo());
        var state = new GameState(true).TransState(GameState.State.WAIT_INIT_HAND_CARD)
            .TransState(GameState.State.WAIT_START)
            .TransState(GameState.State.WAIT_SELF_ACTION);
        var result = state.TransActionState(GameState.ActionState.ATTACKING, card);
        Assert.AreEqual(GameState.ActionState.ATTACKING, result.actionState);
        Assert.AreEqual(card, result.actionCard);
    }

    [Test]
    public void TransActionState_DoesNotChange()
    {
        var state = new GameState(true);
        var result = state.TransActionState(GameState.ActionState.None);
        Assert.AreEqual(GameState.ActionState.None, result.actionState);
    }
}
