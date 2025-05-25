using System.Collections;
using System.Collections.Generic;

// WholeAreaModel变化时存储的操作信息
public record ActionEvent
{
    public enum Type
    {
        BattleMsg = 0, // 用于传递至BattleModel的消息
        ActionText, // 用于ui显示的玩家操作
        Toast, // 操作tip
        Sfx, // 音效
    }

    public Type type { get; init; }

    public object[] args { get; init; }

    public ActionEvent(Type type_, params object[] args_)
    {
        type = type_;
        args = args_;
    }
}
